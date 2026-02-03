using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Community.LiteDB.Aot.Benchmarks.Models;
using Community.LiteDB.Aot.Benchmarks.Generators;
using LiteDB;

namespace Community.LiteDB.Aot.Benchmarks.Benchmarks;

/// <summary>
/// Compares serialization/deserialization performance between:
/// - LiteDB original (reflection-based BsonMapper)
/// - Community.LiteDB.Aot (AOT with Expression Trees for private setters)
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MapperPerformanceBenchmark
{
    private BsonMapper _reflectionMapper = null!;
    private TestEntity _testEntity = null!;
    private DddValueObject _dddEntity = null!;
    private ComplexEntity _complexEntity = null!;
    
    private BsonDocument _testEntityDoc = null!;
    private BsonDocument _dddEntityDoc = null!;
    private BsonDocument _complexEntityDoc = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup LiteDB reflection mapper
        _reflectionMapper = new BsonMapper();
        
        // Generate test data
        _testEntity = TestDataGenerator.GenerateTestEntities(1)[0];
        _dddEntity = TestDataGenerator.GenerateDddEntities(1)[0];
        _complexEntity = TestDataGenerator.GenerateComplexEntities(1)[0];
        
        // Pre-serialize for deserialization tests
        _testEntityDoc = _reflectionMapper.ToDocument(_testEntity);
        _dddEntityDoc = _reflectionMapper.ToDocument(_dddEntity);
        _complexEntityDoc = _reflectionMapper.ToDocument(_complexEntity);
    }

    // ============================================
    // SIMPLE ENTITY (Public Setters - Baseline)
    // ============================================
    
    [Benchmark(Baseline = true, Description = "LiteDB Reflection - Serialize Simple")]
    public BsonDocument LiteDB_Serialize_Simple()
    {
        return _reflectionMapper.ToDocument(_testEntity);
    }

    [Benchmark(Description = "LiteDB Reflection - Deserialize Simple")]
    public TestEntity LiteDB_Deserialize_Simple()
    {
        return _reflectionMapper.ToObject<TestEntity>(_testEntityDoc);
    }

    // ============================================
    // DDD VALUE OBJECT (Private Setters)
    // ============================================
    
    [Benchmark(Description = "LiteDB Reflection - Serialize DDD")]
    public BsonDocument LiteDB_Serialize_Ddd()
    {
        return _reflectionMapper.ToDocument(_dddEntity);
    }

    [Benchmark(Description = "LiteDB Reflection - Deserialize DDD")]
    public DddValueObject LiteDB_Deserialize_Ddd()
    {
        return _reflectionMapper.ToObject<DddValueObject>(_dddEntityDoc);
    }

    // ============================================
    // COMPLEX ENTITY (Nested Objects + Collections)
    // ============================================
    
    [Benchmark(Description = "LiteDB Reflection - Serialize Complex")]
    public BsonDocument LiteDB_Serialize_Complex()
    {
        return _reflectionMapper.ToDocument(_complexEntity);
    }

    [Benchmark(Description = "LiteDB Reflection - Deserialize Complex")]
    public ComplexEntity LiteDB_Deserialize_Complex()
    {
        return _reflectionMapper.ToObject<ComplexEntity>(_complexEntityDoc);
    }
}
