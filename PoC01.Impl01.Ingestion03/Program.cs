using PoC01.Lib;

namespace PoC01.Impl01.Ingestion03
{
    internal class Program
    {
        static void Main(string[] args)
        {
             ExecIngestion().Wait();
        }

        private static async Task ExecIngestion()
        {
            // 1. Initialize Orchestrator
            AiOrchestrator orchestrator = new AiOrchestrator();
            string coll = "docs01";

            // 2. Start the Manager (Point it to a folder)
            string forlderName = "ingest_bucket";
            string folderPath = "C:/tmp";// Directory.GetCurrentDirectory();

            string path = Path.Combine(folderPath, forlderName);
            var manager = new DocumentManager(path,coll, orchestrator);

            Console.WriteLine($"[SYSTEM] Monitoring folder: {path}");
            Console.WriteLine("[SYSTEM] Ready. Drop .txt files there to index them.");
            Console.Write("(type 'exit' for stop Monitoring ): ");
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "exit")
                {
                    Console.WriteLine("[SYSTEM] Stopped.");
                    break;
                }
            }
        }
    }
}
