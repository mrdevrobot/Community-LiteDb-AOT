using System.Collections.Generic;

namespace Community.LiteDB.Aot.SourceGenerators.Models;

/// <summary>
/// Represents an entity discovered in DbContext
/// </summary>
internal sealed class EntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    
    public PropertyInfo? IdProperty { get; set; }
    public bool AutoId { get; set; }
    
    // ValueObject ID conversion (for DDD strongly-typed IDs)
    public string? IdConversionToDb { get; set; }       // e.g., "id => id.Value"
    public string? IdConversionFromDb { get; set; }     // e.g., "guid => new OrderId(guid)"
    public string? IdConversionTargetType { get; set; } // e.g., "Guid", "int", "long"
    
    public List<PropertyInfo> Properties { get; set; } = new();
    public HashSet<string> IgnoredProperties { get; set; } = new();
    
    // Nested types information
    public Dictionary<string, NestedTypeInfo> NestedTypes { get; set; } = new();
}

/// <summary>
/// Represents a nested type (like Address in Customer.Address)
/// </summary>
internal sealed class NestedTypeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public List<PropertyInfo> Properties { get; set; } = new();
    public int Depth { get; set; } = 1; // Nesting depth (1 = direct child, 2 = nested-nested, etc.)
    
    // Nested types within this nested type
    public Dictionary<string, NestedTypeInfo> NestedTypes { get; set; } = new();
}

/// <summary>
/// Represents a property in an entity
/// </summary>
internal sealed class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string BsonFieldName { get; set; } = string.Empty;
    
    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }
    public bool HasIndex { get; set; }
    public string? IndexName { get; set; }
    public bool IsUnique { get; set; }
    
    public bool IsNullable { get; set; }
    public bool IsCollection { get; set; }
    public string? CollectionItemType { get; set; }
    
    // Indicates if collection item type is a nested object
    public bool IsCollectionItemNested { get; set; }
    
    // Nested object support
    public bool IsNestedObject { get; set; }
    public string? NestedTypeName { get; set; }
    public string? NestedTypeFullName { get; set; }
    
    // Data Annotations support
    public bool IsKey { get; set; }
    public bool IsBrowsable { get; set; } = true;
    public bool IsReadOnly { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public object? DefaultValue { get; set; }
    
    // Property accessibility (for DDD support)
    public bool HasPublicSetter { get; set; } = true;
    public bool HasInitOnlySetter { get; set; }
    public string? BackingFieldName { get; set; } // e.g., "<Name>k__BackingField"
    
    // Validation attributes
    public int? MinLength { get; set; }
    public object? RangeMin { get; set; }
    public object? RangeMax { get; set; }
    public string? RegularExpression { get; set; }
    public bool IsEmailAddress { get; set; }
    public bool IsPhone { get; set; }
    public bool IsUrl { get; set; }
    public bool IsCreditCard { get; set; }
    public string? CompareProperty { get; set; } // For [Compare("PropertyName")]
}

/// <summary>
/// Represents a DbContext class to generate code for
/// </summary>
internal sealed class DbContextInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<EntityInfo> Entities { get; set; } = new();
    
    /// <summary>
    /// Global nested types collection - all nested types from all entities
    /// Used to generate shared reusable mappers
    /// </summary>
    public Dictionary<string, NestedTypeInfo> GlobalNestedTypes { get; set; } = new();
}
