using System.ComponentModel.DataAnnotations;

namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// Example entity demonstrating full Data Annotations support
/// </summary>
public class UserProfile
{
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Url]
    public string? Website { get; set; }

    [Range(18, 120)]
    public int Age { get; set; }

    [RegularExpression(@"^[A-Z]{2}\d{4}$", ErrorMessage = "PostalCode must be 2 letters followed by 4 digits")]
    public string? PostalCode { get; set; }

    [CreditCard]
    public string? CreditCardNumber { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password))]
    public string? ConfirmPassword { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
