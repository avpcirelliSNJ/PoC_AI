using PoC01.Lib;
using System.Collections;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PoC01.Impl01
{
    /// <summary>
    /// Local Implementazion (Ollama + Qdrant)
    /// </summary>
    public class AiOrchestrator : IAiOrchestrator
    {


        private readonly HttpClient _http = new HttpClient() { Timeout = TimeSpan.FromMinutes(10) };
        private const string QdrantUrl = "http://localhost:6333";
        private const string OllamaUrl = "http://127.0.0.1:11434";// http://localhost:11434; 

        /// <summary>
        /// Generate Embedding
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var res = await _http.PostAsJsonAsync($"{OllamaUrl}/api/embeddings", new { model = "mxbai-embed-large", prompt = text });
                var data = await res.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
                return data.Embedding;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Errore durante la generazione dell'embedding: {ex.Message}");
                return null;
            }

        }

        /// <summary>
        /// Save in DB (simple Upsert)
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="text"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public async Task<bool> SimpleUpsertDocumentAsync(string collection, string text, string metadata)
        {
            var vector = await GenerateEmbeddingAsync(text);
            var request = new QdrantUpsertRequest
            {
                Points = new List<Point> {
                new Point {
                    Id = Guid.NewGuid(),
                    Vector = vector,
                    Payload = new Dictionary<string, object> { 
                        { "text", text }, 
                        { "source", metadata }
                    }
                }
            }
            };
            var response = await _http.PutAsJsonAsync($"{QdrantUrl}/collections/{collection}/points?wait=true", request);
            if(response.IsSuccessStatusCode) 
                return true;
            else return false;
        }

        /// <summary>
        /// Save txt in DB with file_hash (Upsert) 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="text"></param>
        /// <param name="file_hash"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public async Task<bool> UpsertDocumentWithFileHashAsync(string collection, string text, string file_hash , string metadata)
        {
            var vector = await GenerateEmbeddingAsync(text);
            var request = new QdrantUpsertRequest
            {
                Points = new List<Point> {
                new Point {
                    Id = Guid.NewGuid(),
                    Vector = vector,
                    Payload = new Dictionary<string, object> {
                        { "text", text },
                        { "source", metadata },
                        { "file_hash", file_hash }
                    }
                }
            }
            };
            var response = await _http.PutAsJsonAsync($"{QdrantUrl}/collections/{collection}/points?wait=true", request);
            if (response.IsSuccessStatusCode)
                return true;
            else return false;
        }

        /// <summary>
        /// The RAG Flow 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<string> GetRAGAnswer_Simple_Async(string collection, string query)
        {
            // A. Converti la domanda in vettore
            var queryVector = await GenerateEmbeddingAsync(query);

            // B. Cerca i contesti più simili su Qdrant
            var searchRes = await _http.PostAsJsonAsync($"{QdrantUrl}/collections/{collection}/points/search",
                new QdrantSearchRequest { Vector = queryVector, Limit = 2,  with_payload = true });
            var searchData = await searchRes.Content.ReadFromJsonAsync<QdrantSearchResponse>();

            // C. Costruisci il contesto testuale
            var context = string.Join("\n", searchData.Result.Select(r => (r as ScoredPoint).Payload["text"].ToString()));

            // D.Augmentation
            var prompt = $"Use these informations:\n{context}\n\nQuestion: {query}\nAnswer:";
            var chatRes = await _http.PostAsJsonAsync($"{OllamaUrl}/api/chat", new
            {
                model = "llama3",
                messages = new[] { new { role = "user", content = prompt } },
                stream = false
            });
            
            chatRes.EnsureSuccessStatusCode();

            var chatData = await chatRes.Content.ReadFromJsonAsync<OllamaChatResponse>();
            return chatData.Message.Content;
        }
        
        /// <summary>
        /// Ingest info from a .txt file using Chunking
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<bool> IngestLongFileAsync(string collectionName, string filePath)
        {
            try
            {
                // 1. Read the raw text from the file
                string fullText = await System.IO.File.ReadAllTextAsync(filePath);
                string fileName = Path.GetFileName(filePath);

                string filehash = DocumentManager.GetFileHash(filePath);
                if (await IsFileAlreadyIndexedAsync(collectionName, filehash)) 
                {
                    Console.WriteLine($"[SKIP] File {Path.GetFileName(filePath)} is already in the database.");
                    return false;
                }

                // 2. Break it into chunks (e.g., 800 chars with 150 overlap)
                var chunks = TextChunker.SplitText(fullText, 800, 150);

                Console.WriteLine($"[INFO] Splitting {fileName} into {chunks.Count} chunks...");

                // 3. Reuse your existing UpsertDocumentAsync for each chunk
                for (int i = 0; i < chunks.Count; i++)
                {
                    string metadata = $"{fileName} - Chunk {i + 1}/{chunks.Count}";

                    // ingestion text
                    bool success = await UpsertDocumentWithFileHashAsync(collectionName, chunks[i], filehash, metadata);

                    Console.WriteLine($"[UPLOAD] Progress: {i + 1}/{chunks.Count}");
                }

                Console.WriteLine("[SUCCESS] All chunks ingested and indexed.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message ); 
                return false;
            }
        }
        /// <summary>
        /// check: the file is already indicized?
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="fileHash"></param>
        /// <returns></returns>
        public async Task<bool> IsFileAlreadyIndexedAsync(string collectionName, string fileHash)
        {
            using var httpClient = new HttpClient();

            // 1. Prepare the JSON Filter Body
            // We tell Qdrant: "Scroll through points where file_hash is equal to this value"
            var requestBody = new
            {
                filter = new
                {
                    must = new[]
                    {
                new
                {
                    key = "file_hash",
                    match = new { value = fileHash }
                }
            }
                },
                limit = 1,           // We only need to know if at least one exists
                with_payload = false, // Optimization: don't download the text
                with_vector = false   // Optimization: don't download the numbers
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 2. Send the POST request to the scroll endpoint
            var response = await httpClient.PostAsync(
                $"http://localhost:6333/collections/{collectionName}/points/scroll",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Qdrant Error: {error}");
            }

            // 3. Parse the result
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);

            // Qdrant returns an object: { "result": { "points": [...] }, "status": "ok" }
            var points = doc.RootElement
                            .GetProperty("result")
                            .GetProperty("points");

            // If the array has any elements, the file is already there!
            return points.GetArrayLength() > 0;
        }


        /// <summary>
        /// RAG Flow with "Citations & Sources" 
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<string> GetRAGAnswer_Enhancement01_Async(string coll, string? query)
        {
            // A. Converto question text to VEctor
            var queryVector = await GenerateEmbeddingAsync(query);

            // B. Find the most similar contexts on Qdrant
            var searchRes = await _http.PostAsJsonAsync($"{QdrantUrl}/collections/{coll}/points/search",
                new QdrantSearchRequest { Vector = queryVector, Limit = 2, with_payload = true });
            var searchData = await searchRes.Content.ReadFromJsonAsync<QdrantSearchResponse>();

            // C. build textual context           
            var retrievedChunks = searchData.Result.Select(r =>
            {
                var text = r.Payload["text"].GetString();
                var source = r.Payload.ContainsKey("source") ? r.Payload["source"].GetString() : "Unknown";
                return $"[DOCUMENT: {source}] << {text} >>";
            });

            string context = string.Join("\n\n", retrievedChunks);

            // D. Augmentation
            var systemPrompt = @"You are a professional technical assistant. 
                Answer the user's question using ONLY the provided context. 
                If the answer isn't in the context, say you don't know.
                When you provide an answer, cite the [DOCUMENT: name] used at the end of the sentence.";
            var userPrompt = $@"CONTEXT:
            {context}

            QUESTION:
            {query}";

            var chatRes = await _http.PostAsJsonAsync($"{OllamaUrl}/api/chat", new
            {
                model = "llama3",
                messages = new[] { new { role = "user", content = userPrompt } },
                stream = false
            });
          
            chatRes.EnsureSuccessStatusCode();

            var chatData = await chatRes.Content.ReadFromJsonAsync<OllamaChatResponse>();
            return chatData.Message.Content;
        }
    }
}
