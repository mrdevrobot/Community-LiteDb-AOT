using Microsoft.CodeAnalysis;
using System.Linq;

namespace Community.LiteDB.Aot.SourceGenerators.Helpers;

internal static class AttributeHelper
{
    /// <summary>
    /// Checks if a property has a specific attribute
    /// </summary>
    public static bool HasAttribute(IPropertySymbol property, string attributeName)
    {
        return property.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == attributeName ||
                        attr.AttributeClass?.Name == attributeName + "Attribute");
    }
    
    /// <summary>
    /// Gets the first attribute of a specific type
    /// </summary>
    public static AttributeData? GetAttribute(IPropertySymbol property, string attributeName)
    {
        return property.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == attributeName ||
                                   attr.AttributeClass?.Name == attributeName + "Attribute");
    }
    
    /// <summary>
    /// Checks if property has [Required] attribute
    /// </summary>
    public static bool IsRequired(IPropertySymbol property)
    {
        return HasAttribute(property, "Required");
    }
    
    /// <summary>
    /// Checks if property has [Key] attribute (Data Annotations)
    /// </summary>
    public static bool IsKey(IPropertySymbol property)
    {
        return HasAttribute(property, "Key");
    }
    
    /// <summary>
    /// Checks if property has [Browsable(false)] or [Editable(false)]
    /// </summary>
    public static bool ShouldIgnore(IPropertySymbol property)
    {
        // Check [Browsable(false)]
        var browsableAttr = GetAttribute(property, "Browsable");
        if (browsableAttr != null && browsableAttr.ConstructorArguments.Length > 0)
        {
            if (browsableAttr.ConstructorArguments[0].Value is bool browsable && !browsable)
            {
                return true;
            }
        }
        
        // Check [Editable(false)]
        var editableAttr = GetAttribute(property, "Editable");
        if (editableAttr != null && editableAttr.ConstructorArguments.Length > 0)
        {
            if (editableAttr.ConstructorArguments[0].Value is bool editable && !editable)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets [StringLength] max length value
    /// </summary>
    public static int? GetStringLength(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "StringLength");
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            if (attr.ConstructorArguments[0].Value is int maxLength)
            {
                return maxLength;
            }
        }
        
        // Also check [MaxLength]
        var maxLengthAttr = GetAttribute(property, "MaxLength");
        if (maxLengthAttr != null && maxLengthAttr.ConstructorArguments.Length > 0)
        {
            if (maxLengthAttr.ConstructorArguments[0].Value is int max)
            {
                return max;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets [MinLength] value
    /// </summary>
    public static int? GetMinLength(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "MinLength");
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            if (attr.ConstructorArguments[0].Value is int minLength)
            {
                return minLength;
            }
        }
        
        // Also check StringLength.MinimumLength
        var stringLengthAttr = GetAttribute(property, "StringLength");
        if (stringLengthAttr?.NamedArguments != null)
        {
            var minArg = stringLengthAttr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "MinimumLength");
            
            if (minArg.Value.Value is int min)
            {
                return min;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets [Display(Name = "...")] value
    /// </summary>
    public static string? GetDisplayName(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "Display");
        if (attr?.NamedArguments != null)
        {
            var nameArg = attr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Name");
            
            if (nameArg.Value.Value is string name)
            {
                return name;
            }
        }
        
        // Also check [DisplayName]
        var displayNameAttr = GetAttribute(property, "DisplayName");
        if (displayNameAttr != null && displayNameAttr.ConstructorArguments.Length > 0)
        {
            if (displayNameAttr.ConstructorArguments[0].Value is string displayName)
            {
                return displayName;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets [Description] value
    /// </summary>
    public static string? GetDescription(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "Description");
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            if (attr.ConstructorArguments[0].Value is string description)
            {
                return description;
            }
        }
        
        // Also check [Display(Description = "...")]
        var displayAttr = GetAttribute(property, "Display");
        if (displayAttr?.NamedArguments != null)
        {
            var descArg = displayAttr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Description");
            
            if (descArg.Value.Value is string desc)
            {
                return desc;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Checks if property has [ReadOnly] attribute
    /// </summary>
    public static bool IsReadOnly(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "ReadOnly");
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            if (attr.ConstructorArguments[0].Value is bool readOnly)
            {
                return readOnly;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets [Range] min and max values
    /// </summary>
    public static (object? min, object? max) GetRange(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "Range");
        if (attr != null && attr.ConstructorArguments.Length >= 2)
        {
            var min = attr.ConstructorArguments[0].Value;
            var max = attr.ConstructorArguments[1].Value;
            return (min, max);
        }
        
        return (null, null);
    }
    
    /// <summary>
    /// Gets [RegularExpression] pattern
    /// </summary>
    public static string? GetRegularExpression(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "RegularExpression");
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            if (attr.ConstructorArguments[0].Value is string pattern)
            {
                return pattern;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets [DefaultValue] value
    /// </summary>
    public static object? GetDefaultValue(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "DefaultValue");
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            return attr.ConstructorArguments[0].Value;
        }
        
        return null;
    }
    
    /// <summary>
    /// Checks if property has [EmailAddress] attribute
    /// </summary>
    public static bool IsEmailAddress(IPropertySymbol property)
    {
        return HasAttribute(property, "EmailAddress");
    }
    
    /// <summary>
    /// Checks if property has [Phone] attribute
    /// </summary>
    public static bool IsPhone(IPropertySymbol property)
    {
        return HasAttribute(property, "Phone");
    }
    
    /// <summary>
    /// Checks if property has [Url] attribute
    /// </summary>
    public static bool IsUrl(IPropertySymbol property)
    {
        return HasAttribute(property, "Url");
    }
    
    /// <summary>
    /// Checks if property has [CreditCard] attribute
    /// </summary>
    public static bool IsCreditCard(IPropertySymbol property)
    {
        return HasAttribute(property, "CreditCard");
    }
    
    /// <summary>
    /// Gets [Compare("PropertyName")] target property name
    /// </summary>
    public static string? GetCompareProperty(IPropertySymbol property)
    {
        var attr = GetAttribute(property, "Compare");
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            if (attr.ConstructorArguments[0].Value is string propertyName)
            {
                return propertyName;
            }
        }
        
        return null;
    }
}

