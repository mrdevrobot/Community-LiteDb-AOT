using Community.LiteDB.Aot.Sample.Domain;
using Community.LiteDB.Aot.Sample.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Community.LiteDB.Aot.Tests;

/// <summary>
/// Basic sanity tests to verify code generation works
/// </summary>
public class BasicSanityTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _testDbPath;

    public BasicSanityTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_sanity_{Guid.NewGuid()}.db");
        _db = new AppDbContext(_testDbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    [Fact]
    public void SimpleEntity_ShouldInsertAndRetrieve()
    {
        // Arrange
        var customer = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            City = "New York"
        };

        // Act
        var id = _db.Customers.Insert(customer);
        var retrieved = _db.Customers.FindById(id);

        // Assert
        retrieved.Should().NotBe(null);
        retrieved!.Name.Should().Be("John Doe");
        retrieved.Email.Should().Be("john@example.com");
        retrieved.City.Should().Be("New York");
    }

    [Fact]
    public void DataAnnotations_RequiredValidation_ShouldWork()
    {
        // Arrange
        var profile = new UserProfile
        {
            Username = null!, // Required
            Email = "test@example.com",
            Password = "Password123",
            Age = 25
        };

        // Act & Assert
        var act = () => _db.UserProfiles.Insert(profile);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DataAnnotations_RangeValidation_ShouldWork()
    {
        // Arrange
        var profile = new UserProfile
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123",
            Age = 17 // Range: 18-120
        };

        // Act & Assert
        var act = () => _db.UserProfiles.Insert(profile);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NestedObject_ShouldWork()
    {
        // Arrange
        var customer = new CustomerWithAddress
        {
            Name = "Jane Doe",
            Email = "jane@example.com",
            Address = new Address
            {
                Street = "123 Main St",
                City = "Boston",
                ZipCode = "02101"
            }
        };

        // Act
        var id = _db.CustomersWithAddress.Insert(customer);
        var retrieved = _db.CustomersWithAddress.FindById(id);

        // Assert
        retrieved.Should().NotBe(null);
        retrieved!.Address.Should().NotBe(null);
        retrieved.Address.City.Should().Be("Boston");
    }

    [Fact]
    public void KeyAttribute_ShouldWork()
    {
        // Arrange
        var product = new ProductWithKeyAttribute
        {
            Name = "Test Product",
            Price = 99.99m
        };

        // Act
        var id = _db.ProductsWithKeyAttr.Insert(product);
        var retrieved = _db.ProductsWithKeyAttr.FindById(id);

        // Assert
        retrieved.Should().NotBe(null);
        retrieved!.Name.Should().Be("Test Product");
    }
}
