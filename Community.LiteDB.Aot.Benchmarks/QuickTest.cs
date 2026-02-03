using System.Diagnostics;
using Community.LiteDB.Aot.Benchmarks.Models;
using Community.LiteDB.Aot.Benchmarks.Generators;
using Community.LiteDB.Aot.Benchmarks.Benchmarks;
using LiteDB;

namespace Community.LiteDB.Aot.Benchmarks;

/// <summary>
/// Quick sanity test to verify mappers work correctly before running full benchmarks
/// </summary>
class QuickTest
{
    public static void Run()
    {
        Console.WriteLine("=== Quick Sanity Test ===");
        Console.WriteLine();

        TestSimpleEntity();
        TestDddEntity();
        TestComplexEntity();

        Console.WriteLine();
        Console.WriteLine("? All tests passed!");
        Console.WriteLine();
        Console.WriteLine("Ready to run full benchmarks with:");
        Console.WriteLine("  dotnet run -c Release");
    }

    private static void TestSimpleEntity()
    {
        Console.Write("Testing Simple Entity... ");
        
        var mapper = new BsonMapper();
        var entity = TestDataGenerator.GenerateTestEntities(1)[0];
        
        // Test serialization
        var doc = mapper.ToDocument(entity);
        if (doc["Name"].AsString != entity.Name)
            throw new Exception("Serialization failed!");
        
        // Test deserialization
        var restored = mapper.ToObject<TestEntity>(doc);
        if (restored.Name != entity.Name)
            throw new Exception("Deserialization failed!");
        
        Console.WriteLine("?");
    }

    private static void TestDddEntity()
    {
        Console.Write("Testing DDD Entity... ");
        
        var mapper = new BsonMapper();
        var entity = TestDataGenerator.GenerateDddEntities(1)[0];
        
        // Test serialization
        var doc = mapper.ToDocument(entity);
        if (doc["Name"].AsString != entity.Name)
            throw new Exception("Serialization failed!");
        
        // Test deserialization
        var restored = mapper.ToObject<DddValueObject>(doc);
        if (restored.Name != entity.Name || restored.Amount != entity.Amount)
            throw new Exception("Deserialization failed!");
        
        Console.WriteLine("?");
    }

    private static void TestComplexEntity()
    {
        Console.Write("Testing Complex Entity... ");
        
        var mapper = new BsonMapper();
        var entity = TestDataGenerator.GenerateComplexEntities(1)[0];
        
        // Test serialization
        var doc = mapper.ToDocument(entity);
        if (doc["Title"].AsString != entity.Title)
            throw new Exception("Serialization failed!");
        
        // Test deserialization
        var restored = mapper.ToObject<ComplexEntity>(doc);
        if (restored.Title != entity.Title || restored.Address.City != entity.Address.City)
            throw new Exception("Deserialization failed!");
        
        Console.WriteLine("?");
    }
}
