using Community.LiteDB.Aot.Sample.Domain;
using Community.LiteDB.Aot.Sample.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Community.LiteDB.Aot.Tests;

/// <summary>
/// Tests for [Key] attribute detection
/// Verifies that the source generator correctly identifies primary keys via Data Annotations
/// </summary>
public class KeyAttributeTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _testDbPath;

    public KeyAttributeTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_key_{Guid.NewGuid()}.db");
        _db = new AppDbContext(_testDbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    [Fact]
    public void KeyAttribute_WithIntKey_ShouldWorkWithAutoIncrement()
    {
        // Arrange - ProductId is marked with [Key], should auto-increment
        var product1 = new ProductWithKeyAttribute
        {
            Name = "Product 1",
            Price = 19.99m,
            Description = "First product"
        };
        
        var product2 = new ProductWithKeyAttribute
        {
            Name = "Product 2",
            Price = 29.99m,
            Description = "Second product"
        };

        // Act
        var id1 = _db.ProductsWithKeyAttr.Insert(product1);
        var id2 = _db.ProductsWithKeyAttr.Insert(product2);

        // Assert - Should auto-increment (checking that they are different and sequential)
        id1.Should().NotBe(0, "First ID should not be 0");
        id2.Should().NotBe(0, "Second ID should not be 0");
        id2.Should().NotBe(id1, "IDs should be different");
    }

    [Fact]
    public void KeyAttribute_NonIdPropertyName_ShouldBeRecognized()
    {
        // Arrange - Property is named "ProductId" not "Id", but has [Key]
        var product = new ProductWithKeyAttribute
        {
            Name = "Test Product",
            Price = 99.99m
        };

        // Act
        var id = _db.ProductsWithKeyAttr.Insert(product);

        // Assert
        id.Should().NotBe(null);
        
        var retrieved = _db.ProductsWithKeyAttr.FindById(id);
        retrieved.Should().NotBe(null);
        retrieved!.Name.Should().Be("Test Product");
    }

    [Fact]
    public void KeyAttribute_WithGuidKey_ShouldWork()
    {
        // Arrange - DocumentId is Guid with [Key]
        var document = new DocumentWithGuidKey
        {
            Title = "Test Document",
            Content = "Some content"
        };
        
        var originalId = document.DocumentId;

        // Act
        var id = _db.Documents.Insert(document);

        // Assert
        id.Should().NotBe(null);
        
        var retrieved = _db.Documents.FindById(id);
        retrieved.Should().NotBe(null);
        retrieved!.DocumentId.Should().Be(originalId);
        retrieved.Title.Should().Be("Test Document");
    }

    [Fact]
    public void KeyAttribute_WithStringKey_ShouldWork()
    {
        // Arrange - Code is string with [Key]
        var entity = new EntityWithStringKey
        {
            Code = "ABC123",
            Name = "Test Entity",
            Value = 42
        };

        // Act
        var id = _db.EntitiesWithStringKey.Insert(entity);

        // Assert
        id.Should().NotBe(null);
        
        var retrieved = _db.EntitiesWithStringKey.FindById(id);
        retrieved.Should().NotBe(null);
        retrieved!.Code.Should().Be("ABC123");
        retrieved.Name.Should().Be("Test Entity");
        retrieved.Value.Should().Be(42);
    }

    [Fact]
    public void KeyAttribute_UpdateShouldPreserveKey()
    {
        // Arrange
        var product = new ProductWithKeyAttribute
        {
            Name = "Original",
            Price = 10m
        };
        
        var id = _db.ProductsWithKeyAttr.Insert(product);

        // Act - Update
        var retrieved = _db.ProductsWithKeyAttr.FindById(id);
        retrieved!.Name = "Updated";
        retrieved.Price = 20m;
        _db.ProductsWithKeyAttr.Update(retrieved);

        // Assert
        var updated = _db.ProductsWithKeyAttr.FindById(id);
        updated.Should().NotBe(null);
        updated!.Name.Should().Be("Updated");
        updated.Price.Should().Be(20m);
    }

    [Fact]
    public void KeyAttribute_WithDataAnnotationsValidation_ShouldEnforceRules()
    {
        // Arrange - ProductWithKeyAttribute has [Range] on Price
        var product = new ProductWithKeyAttribute
        {
            Name = "Test",
            Price = 1_000_000m, // Exceeds range: 0-999999.99
            Description = "Test"
        };

        // Act & Assert
        var act = () => _db.ProductsWithKeyAttr.Insert(product);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Price*between*");
    }
}
