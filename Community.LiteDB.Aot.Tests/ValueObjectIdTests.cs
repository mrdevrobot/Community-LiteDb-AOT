using Community.LiteDB.Aot.Sample.Domain;
using Community.LiteDB.Aot.Sample.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Community.LiteDB.Aot.Tests;

/// <summary>
/// Tests for ValueObject ID serialization/deserialization
/// Verifies that strongly-typed IDs work correctly with conversion
/// </summary>
public class ValueObjectIdTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _testDbPath;

    public ValueObjectIdTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_valueobject_{Guid.NewGuid()}.db");
        _db = new AppDbContext(_testDbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    [Fact]
    public void ValueObjectId_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var order = new OrderWithStronglyTypedId("John Doe", 99.99m)
        {
            Status = "Pending"
        };
        
        var originalId = order.Id;

        // Act - Insert
        var insertedId = _db.OrdersWithStrongId.Insert(order);

        // Assert - ID should be converted to string in DB
        insertedId.Should().NotBe(null);

        // Act - Retrieve
        var retrieved = _db.OrdersWithStrongId.FindById(insertedId);

        // Assert - Should deserialize back to OrderId correctly
        retrieved.Should().NotBe(null);
        retrieved!.Id.Should().NotBe(null);
        retrieved.Id.Value.Should().Be(originalId.Value, "OrderId.Value should match");
        retrieved.CustomerName.Should().Be("John Doe");
        retrieved.TotalAmount.Should().Be(99.99m);
        retrieved.Status.Should().Be("Pending");
    }

    [Fact]
    public void ValueObjectId_ShouldSupportMultipleOrders()
    {
        // Arrange
        var order1 = new OrderWithStronglyTypedId("Alice", 100m);
        var order2 = new OrderWithStronglyTypedId("Bob", 200m);
        var order3 = new OrderWithStronglyTypedId("Charlie", 300m);

        // Act
        var id1 = _db.OrdersWithStrongId.Insert(order1);
        var id2 = _db.OrdersWithStrongId.Insert(order2);
        var id3 = _db.OrdersWithStrongId.Insert(order3);

        // Assert - All IDs should be unique
        var allOrders = _db.OrdersWithStrongId.FindAll().ToList();
        allOrders.Should().HaveCount(3);
        
        var uniqueIds = allOrders.Select(o => o.Id.Value).Distinct().ToList();
        uniqueIds.Should().HaveCount(3, "All OrderIds should be unique");
    }

    [Fact]
    public void ValueObjectId_ShouldSupportUpdate()
    {
        // Arrange
        var order = new OrderWithStronglyTypedId("Original Customer", 50m);
        var id = _db.OrdersWithStrongId.Insert(order);

        // Act - Retrieve and modify
        var retrieved = _db.OrdersWithStrongId.FindById(id);
        retrieved!.CustomerName = "Updated Customer";
        retrieved.TotalAmount = 75m;
        retrieved.Status = "Completed";
        
        var updateResult = _db.OrdersWithStrongId.Update(retrieved);

        // Assert
        updateResult.Should().BeGreaterThan(0);
        
        var updated = _db.OrdersWithStrongId.FindById(id);
        updated.Should().NotBe(null);
        updated!.CustomerName.Should().Be("Updated Customer");
        updated.TotalAmount.Should().Be(75m);
        updated.Status.Should().Be("Completed");
        updated.Id.Value.Should().Be(order.Id.Value, "ID should remain unchanged");
    }

    [Fact]
    public void ValueObjectId_ShouldSupportDelete()
    {
        // Arrange
        var order = new OrderWithStronglyTypedId("Test Customer", 100m);
        var id = _db.OrdersWithStrongId.Insert(order);

        // Act
        var deleteResult = _db.OrdersWithStrongId.Delete(id);

        // Assert
        deleteResult.Should().BeGreaterThan(0);
        
        var retrieved = _db.OrdersWithStrongId.FindById(id);
        retrieved.Should().BeNull("Order should be deleted");
    }

    [Fact]
    public void ValueObjectId_ShouldSupportBasicFiltering()
    {
        // Arrange
        var orders = new[]
        {
            new OrderWithStronglyTypedId("Alice", 100m) { Status = "Pending" },
            new OrderWithStronglyTypedId("Bob", 200m) { Status = "Completed" },
            new OrderWithStronglyTypedId("Charlie", 300m) { Status = "Pending" }
        };

        foreach (var order in orders)
        {
            _db.OrdersWithStrongId.Insert(order);
        }

        // Act - Use FindAll + LINQ to Objects (not database query)
        var allOrders = _db.OrdersWithStrongId.FindAll().ToList();
        var pendingOrders = allOrders.Where(o => o.Status == "Pending").ToList();

        // Assert
        pendingOrders.Should().HaveCount(2);
        pendingOrders.Should().AllSatisfy(o => o.Status.Should().Be("Pending"));
    }

    [Fact]
    public void ValueObjectId_PrivateSetter_ShouldWorkWithExpressionTrees()
    {
        // Arrange - OrderId has private setter, tests Expression Tree compilation
        var order = new OrderWithStronglyTypedId("Test", 50m);
        var originalIdValue = order.Id.Value;

        // Act - Insert and retrieve (this internally uses SetId with Expression Tree)
        var id = _db.OrdersWithStrongId.Insert(order);
        var retrieved = _db.OrdersWithStrongId.FindById(id);

        // Assert - ID should be correctly deserialized via Expression Tree
        retrieved.Should().NotBe(null);
        retrieved!.Id.Should().NotBe(null);
        retrieved.Id.Value.Should().Be(originalIdValue);
    }
}
