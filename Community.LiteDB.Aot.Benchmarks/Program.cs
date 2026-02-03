using BenchmarkDotNet.Running;
using Community.LiteDB.Aot.Benchmarks.Benchmarks;

namespace Community.LiteDB.Aot.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=======================================================");
        Console.WriteLine(" Community.LiteDB.Aot Performance Benchmarks");
        Console.WriteLine("=======================================================");
        Console.WriteLine();
        Console.WriteLine("Comparing:");
        Console.WriteLine("  - LiteDB Original (Reflection-based BsonMapper)");
        Console.WriteLine("  - Community.LiteDB.Aot (Expression Trees + Object Initializers)");
        Console.WriteLine();
        Console.WriteLine("=======================================================");
        Console.WriteLine();

        // Check if user wants quick test
        if (args.Length > 0 && args[0] == "--quick-test")
        {
            QuickTest.Run();
            return;
        }

        // Run all benchmarks
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        
        Console.WriteLine();
        Console.WriteLine("Benchmarks completed! Press any key to exit...");
        Console.ReadKey();
    }
}
