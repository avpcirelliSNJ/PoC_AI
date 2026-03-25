namespace PoC01.Impl01.Ingestion02
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

            Console.WriteLine("Ingestion 02 (Load text with Chunking)");
            await orch.IngestLongFileAsync(coll, "C:\\tmp\\manual.txt");
            Console.WriteLine("Documents Loaded");

        }
    }
}
