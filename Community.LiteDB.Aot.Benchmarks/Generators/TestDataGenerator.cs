using Community.LiteDB.Aot.Benchmarks.Models;

namespace Community.LiteDB.Aot.Benchmarks.Generators;

public static class TestDataGenerator
{
    private static readonly Random Random = new(42); // Fixed seed for reproducibility
    private static readonly string[] Names = { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry" };
    private static readonly string[] Cities = { "New York", "London", "Tokyo", "Paris", "Sydney", "Toronto", "Berlin", "Dubai" };
    private static readonly string[] Countries = { "USA", "UK", "Japan", "France", "Australia", "Canada", "Germany", "UAE" };
    private static readonly string[] Tags = { "Important", "Urgent", "Review", "Pending", "Approved", "Rejected" };
    private static readonly string[] Colors = { "Red", "Blue", "Green", "Yellow", "Purple", "Orange" };

    public static List<TestEntity> GenerateTestEntities(int count)
    {
        var list = new List<TestEntity>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new TestEntity
            {
                Id = i + 1,
                Name = Names[Random.Next(Names.Length)] + i,
                Description = $"Description for entity {i}",
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Next(365)),
                IsActive = Random.Next(2) == 1,
                Score = Random.NextDouble() * 100
            });
        }
        return list;
    }

    public static List<DddValueObject> GenerateDddEntities(int count)
    {
        var list = new List<DddValueObject>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new DddValueObject(
                Names[Random.Next(Names.Length)] + i,
                (decimal)(Random.NextDouble() * 1000),
                "USD"
            ));
        }
        return list;
    }

    public static List<ComplexEntity> GenerateComplexEntities(int count)
    {
        var list = new List<ComplexEntity>(count);
        for (int i = 0; i < count; i++)
        {
            var tagCount = Random.Next(1, 5);
            var tags = new List<NestedTag>();
            for (int j = 0; j < tagCount; j++)
            {
                tags.Add(new NestedTag
                {
                    Name = Tags[Random.Next(Tags.Length)],
                    Color = Colors[Random.Next(Colors.Length)]
                });
            }

            list.Add(new ComplexEntity
            {
                Id = i + 1,
                Title = $"Complex Entity {i}",
                Address = new NestedAddress
                {
                    Street = $"{Random.Next(1, 1000)} Main Street",
                    City = Cities[Random.Next(Cities.Length)],
                    Country = Countries[Random.Next(Countries.Length)]
                },
                Tags = tags,
                UpdatedAt = DateTime.UtcNow
            });
        }
        return list;
    }
}
