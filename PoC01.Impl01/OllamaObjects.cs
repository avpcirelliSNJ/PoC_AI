using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoC01.Impl01
{
    // Ollama: Risposta per la Chat
    public class OllamaChatResponse { public OllamaMessage Message { get; set; } }
    public class OllamaMessage { public string Role { get; set; } public string Content { get; set; } }

    public class OllamaEmbeddingResponse
    {
        // Il nome della proprietà deve corrispondere esattamente alla chiave JSON
        // o essere mappato tramite l'attributo [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; }
    }
}
