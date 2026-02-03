namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// Address value object - nested in Customer
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Customer with nested Address object
/// </summary>
public class CustomerWithAddress
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Nested object
    public Address? Address { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
