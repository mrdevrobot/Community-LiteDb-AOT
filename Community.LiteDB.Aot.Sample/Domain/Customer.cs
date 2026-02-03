namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// Sample domain entity - completely clean, no dependencies
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string City { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
