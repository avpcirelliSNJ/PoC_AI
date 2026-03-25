using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PoC01.Lib
{
    internal class Utils
    {

        
    }

    public static class TextChunker
    {
        public static List<string> SplitText(string text, int chunkSize = 1000, int overlap = 200)
        {
            var chunks = new List<string>();
            int index = 0;

            while (index < text.Length)
            {
                if (index + chunkSize >= text.Length)
                {
                    chunks.Add(text.Substring(index).Trim());
                    break;
                }

                // Look for the last space within the chunk size to avoid splitting words
                int end = index + chunkSize;
                int lastSpace = text.LastIndexOf(' ', end, chunkSize / 2); // Search back a bit

                int effectiveEnd = (lastSpace > index) ? lastSpace : end;
                string chunk = text.Substring(index, effectiveEnd - index).Trim();
                chunks.Add(chunk);

                // Move the index back by the overlap to maintain context
                index = effectiveEnd - overlap;

                // Safety check to ensure we are always moving forward
                if (index < 0) index = effectiveEnd;
            }

            return chunks;
        }
    }

    public class DocumentManager
    {
        private readonly string _watchPath;
        private readonly FileSystemWatcher _watcher;
        private readonly IAiOrchestrator _orchestrator;
        private readonly string _collectionName;

        public DocumentManager(string path, string collectionName,  IAiOrchestrator orchestrator)
        {
            _watchPath = path;
            _orchestrator = orchestrator;
            _collectionName = collectionName;
            if (!Directory.Exists(_watchPath)) Directory.CreateDirectory(_watchPath);

            _watcher = new FileSystemWatcher(_watchPath, "*.txt"); // Start with .txt
            _watcher.Created += OnNewFileDetected;
            _watcher.EnableRaisingEvents = true;
        }

        private async void OnNewFileDetected(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"\n[WATCHER] New file detected: {e.Name}. Starting ingestion...");

            // Give the OS a second to finish writing the file to disk
            await Task.Delay(1000);

            try
            {
                bool success = await _orchestrator.IngestLongFileAsync(_collectionName, e.FullPath);
                if(success) 
                Console.WriteLine($"[WATCHER] Successfully indexed {e.Name}!");
                else
                    Console.WriteLine($"[WATCHER] Error in indexing {e.Name}!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to process {e.Name}: {ex.Message}");
            }
        }

        public static string GetFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}


    
