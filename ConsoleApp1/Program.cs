
using PoC01.Impl01;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            PoF01Exec().Wait();
        }

        private static async Task PoF01Exec()
        {
            var orch = new AiOrchestrator();
            string coll = "docs01";

            Console.WriteLine("Ingestion 01 (Load simple text)");

            await orch.SimpleUpsertDocumentAsync(coll, "The production server restarts with 'sudo systemctl restart app'.", "Operation Manual");
            await orch.SimpleUpsertDocumentAsync(coll, "Security policies require 12-character passwords.", "Security Policy");
            Console.WriteLine("Documents Loaded");

        }
    }
}
