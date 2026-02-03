using System;

namespace Community.LiteDB.Aot.Benchmarks.Models;

/// <summary>
/// Simple entity with public setters (baseline)
/// </summary>
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public double Score { get; set; }
}

/// <summary>
/// DDD Value Object with private setters
/// </summary>
public class DddValueObject
{
    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime Timestamp { get; private set; }

    private DddValueObject() { }

    public DddValueObject(string name, decimal amount, string currency)
    {
        Name = name;
        Amount = amount;
        Currency = currency;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Complex entity with nested objects
/// </summary>
public class ComplexEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public NestedAddress Address { get; set; } = new();
    public List<NestedTag> Tags { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class NestedAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class NestedTag
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
