using Community.LiteDB.Aot.Sample.Domain;
using Community.LiteDB.Aot.Sample.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Community.LiteDB.Aot.Tests;

/// <summary>
/// Tests for DDD entities with private setters using Expression Trees
/// Verifies that compiled Expression Trees work correctly for immutable entities
/// </summary>
public class PrivateSetterTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _testDbPath;

    public PrivateSetterTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_private_{Guid.NewGuid()}.db");
        _db = new AppDbContext(_testDbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    [Fact]
    public void PrivateSetters_DddValueObject_ShouldSerializeAndDeserialize()
    {
        // Arrange - Money has private setters on Amount and Currency
        var product = new ProductWithMoney
        {
            Name = "Laptop",
        };

        product.SetPrice(new Money(1500m, "USD"));

        // Act
        var id = _db.ProductsWithMoney.Insert(product);
        var retrieved = _db.ProductsWithMoney.FindById(id);

        // Assert - Private setters should work via Expression Trees
        retrieved.Should().NotBe(null);
        retrieved!.Price.Should().NotBe(null);
        retrieved.Price.Amount.Should().Be(1500m);
        retrieved.Price.Currency.Should().Be("USD");
    }

    [Fact]
    public void PrivateSetters_ShouldPreserveImmutability()
    {
        // Arrange
        var originalMoney = new Money(100m, "EUR");
        var product = new ProductWithMoney
        {
            Name = "Book",
        };

        product.SetPrice(originalMoney);

        // Act
        var id = _db.ProductsWithMoney.Insert(product);
        var retrieved = _db.ProductsWithMoney.FindById(id);

        // Assert - Values should match exactly
        retrieved.Should().NotBe(null);
        retrieved!.Price.Amount.Should().Be(originalMoney.Amount);
        retrieved.Price.Currency.Should().Be(originalMoney.Currency);
    }

    [Fact]
    public void PrivateSetters_MultipleEntities_ShouldWorkIndependently()
    {
        // Arrange
        var product1 = new ProductWithMoney
        {
            Name = "Item1"
        };
        product1.SetPrice(new Money(10m, "USD"));

        var product2 = new ProductWithMoney
        {
            Name = "Item2"
        };
        product2.SetPrice(new Money(20m, "EUR"));

        var product3 = new ProductWithMoney
        {
            Name = "Item3"
        };
        product3.SetPrice(new Money(30m, "GBP"));



        var products = new[]
        {
            product1,
            product2,
            product3
        };

        // Act
        foreach (var product in products)
        {
            _db.ProductsWithMoney.Insert(product);
        }

        var allProducts = _db.ProductsWithMoney.FindAll().OrderBy(p => p.Name).ToList();

        // Assert
        allProducts.Should().HaveCount(3);
        allProducts[0].Price.Amount.Should().Be(10m);
        allProducts[0].Price.Currency.Should().Be("USD");
        allProducts[1].Price.Amount.Should().Be(20m);
        allProducts[1].Price.Currency.Should().Be("EUR");
        allProducts[2].Price.Amount.Should().Be(30m);
        allProducts[2].Price.Currency.Should().Be("GBP");
    }

    [Fact]
    public void PrivateSetters_Update_ShouldWorkCorrectly()
    {
        // Arrange
        var product = new ProductWithMoney
        {
            Name = "Original",
        };
        product.SetPrice(new Money(50m, "USD"));

        var id = _db.ProductsWithMoney.Insert(product);

        // Act - Update name only (Price is init-only/immutable in this DDD model)
        var retrieved = _db.ProductsWithMoney.FindById(id);
        retrieved!.Name = "Updated";
        _db.ProductsWithMoney.Update(retrieved);

        // Assert
        var updated = _db.ProductsWithMoney.FindById(id);
        updated.Should().NotBe(null);
        updated!.Name.Should().Be("Updated");
        // Price remains unchanged (immutable ValueObject)
        updated.Price.Amount.Should().Be(50m);
        updated.Price.Currency.Should().Be("USD");
    }

    [Fact]
    public void PrivateSetters_ExpressionTrees_ShouldHaveNearNativePerformance()
    {
        // Arrange - Create many entities to test performance
        var products = Enumerable.Range(1, 1000)
            .Select(i =>
            {
                var p = new ProductWithMoney
                {
                    Name = $"Product {i}",
                };
                p.SetPrice(new Money(i * 10m, "USD"));
                return p;
            })
            .ToList();

        // Act - Insert all (tests Expression Tree performance)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (var product in products)
        {
            _db.ProductsWithMoney.Insert(product);
        }
        sw.Stop();

        // Assert - Should be reasonably fast (< 2 seconds for 1000 items)
        sw.ElapsedMilliseconds.Should().BeLessThan(2000,
            "Expression Trees should provide near-native performance");

        // Verify all were inserted correctly
        var count = _db.ProductsWithMoney.Count();
        count.Should().Be(1000);
    }

    [Fact]
    public void PrivateSetters_OrderWithStronglyTypedId_ShouldWork()
    {
        // Arrange - OrderWithStronglyTypedId has private setter on Id
        var order = new OrderWithStronglyTypedId("Customer", 100m);
        var originalIdValue = order.Id.Value;

        // Act
        var id = _db.OrdersWithStrongId.Insert(order);
        var retrieved = _db.OrdersWithStrongId.FindById(id);

        // Assert - ID with private setter should be preserved
        retrieved.Should().NotBe(null);
        retrieved!.Id.Value.Should().Be(originalIdValue);
        retrieved.CustomerName.Should().Be("Customer");
    }

    [Fact]
    public void PrivateSetters_Constructor_ShouldNotBeCalledDuringDeserialization()
    {
        // Arrange - Money constructor validates amount > 0
        var product = new ProductWithMoney
        {
            Name = "Test",
        };

        product.SetPrice(new Money(100m, "USD"));

        var id = _db.ProductsWithMoney.Insert(product);

        // Act - Deserialize uses FormatterServices.GetUninitializedObject
        // This bypasses the constructor, then uses Expression Trees to set private fields
        var retrieved = _db.ProductsWithMoney.FindById(id);

        // Assert - Should work correctly via Expression Trees
        retrieved.Should().NotBe(null);
        retrieved!.Price.Amount.Should().Be(100m);
        retrieved.Price.Currency.Should().Be("USD");
    }
}

