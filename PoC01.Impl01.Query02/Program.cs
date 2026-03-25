namespace PoC01.Impl01.Query02
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ExecQuery().Wait();
        }

        private async static Task ExecQuery()
        {
            Console.WriteLine("Ok, this simple RAG is Up And Running");
            var orch = new AiOrchestrator();
            string coll = "docs01";

            while (true)
            {
                Console.Write("Ask somekind (or 'exit'): ");
                var input = Console.ReadLine();
                if (input == "exit") break;

                var answer = await orch.GetRAGAnswer_Enhancement01_Async(coll, input);
                Console.WriteLine($"\nAI: {answer}\n");
            }
        }
    }
}
