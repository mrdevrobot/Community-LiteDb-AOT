using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// Advanced entity using Data Annotations for validation and metadata
/// </summary>
public class Product
{
    [Key]
    public int ProductId { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Product Name", Description = "The name of the product")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, 999999.99)]
    [Display(Name = "Unit Price")]
    public decimal Price { get; set; }
    
    // [Range(0, int.MaxValue)]  // Temporarily disabled
    [DefaultValue(0)]
    public int Stock { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    [Display(Name = "Is Active")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Created Date")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ReadOnly(true)]
    [Display(Name = "Last Modified")]
    public DateTime? LastModified { get; set; }
    
    [Browsable(false)]
    [Editable(false)]
    public string? InternalNotes { get; set; }
}
