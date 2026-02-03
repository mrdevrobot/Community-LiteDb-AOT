namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// Company entity with a collection of nested Address objects
/// Tests: List<NestedObject> support
/// </summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    
    // Collection of nested objects!
    public List<Address> Offices { get; set; } = new();
    
    public DateTime Founded { get; set; } = DateTime.UtcNow;
}
