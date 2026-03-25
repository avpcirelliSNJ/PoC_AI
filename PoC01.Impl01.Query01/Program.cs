// See https://aka.ms/new-console-template for more information

//

using PoC01.Impl01;
using PoC01.Lib;

Console.WriteLine("Ok, this simple RAG is Up And Running");
var orch = new AiOrchestrator();
string coll = "docs01";

while (true)
{
    Console.Write("Ask somekind (or 'exit'): ");
    var input = Console.ReadLine();
    if (input == "exit") break;

    var answer = await orch.GetRAGAnswer_Simple_Async(coll, input);
    Console.WriteLine($"\nAI: {answer}\n");
}
