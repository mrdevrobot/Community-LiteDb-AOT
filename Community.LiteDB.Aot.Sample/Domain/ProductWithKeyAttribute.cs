using System.ComponentModel.DataAnnotations;

namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// Example entity using [Key] attribute instead of convention or configuration
/// Demonstrates that Data Annotations [Key] is fully supported
/// </summary>
public class ProductWithKeyAttribute
{
    [Key] // ? This identifies the primary key via Data Annotations
    public int ProductId { get; set; } // Note: NOT named "Id"!

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 999999.99)]
    public decimal Price { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// Example using [Key] with Guid
/// </summary>
public class DocumentWithGuidKey
{
    [Key]
    public Guid DocumentId { get; set; } = Guid.NewGuid();

    [Required]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Example using [Key] with string (custom ID)
/// </summary>
public class EntityWithStringKey
{
    [Key]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty; // Custom string key

    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }
}
