using System.Linq.Expressions;

namespace Community.LiteDB.Aot.ModelBuilder;

/// <summary>
/// Fluent API for configuring entity models (EF Core style)
/// </summary>
public class EntityModelBuilder
{
    private readonly List<IEntityConfiguration> _configurations = new();
    
    /// <summary>
    /// Configure entity of type T
    /// </summary>
    public EntityTypeBuilder<T> Entity<T>() where T : class
    {
        var config = new EntityConfiguration<T>();
        _configurations.Add(config);
        return new EntityTypeBuilder<T>(config);
    }
    
    /// <summary>
    /// Configure entity of type T with action
    /// </summary>
    public void Entity<T>(Action<EntityTypeBuilder<T>> buildAction) where T : class
    {
        if (buildAction == null) throw new ArgumentNullException(nameof(buildAction));
        
        var builder = Entity<T>();
        buildAction(builder);
    }
    
    internal IReadOnlyList<IEntityConfiguration> GetConfigurations() => _configurations.AsReadOnly();
}

/// <summary>
/// Fluent API for configuring a specific entity type
/// </summary>
public class EntityTypeBuilder<T> where T : class
{
    private readonly EntityConfiguration<T> _config;
    
    internal EntityTypeBuilder(EntityConfiguration<T> config)
    {
        _config = config;
    }
    
    /// <summary>
    /// Configure primary key
    /// </summary>
    public KeyBuilder HasKey<TProperty>(Expression<Func<T, TProperty>> keyExpression)
    {
        if (keyExpression == null) throw new ArgumentNullException(nameof(keyExpression));
        
        var propertyName = GetPropertyName(keyExpression);
        _config.IdPropertyName = propertyName;
        
        return new KeyBuilder(_config);
    }
    
    /// <summary>
    /// Configure property
    /// </summary>
    public PropertyBuilder<TProperty> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        
        var propertyName = GetPropertyName(propertyExpression);
        
        if (!_config.Properties.ContainsKey(propertyName))
        {
            _config.Properties[propertyName] = new PropertyConfiguration();
        }
        
        return new PropertyBuilder<TProperty>(_config.Properties[propertyName]);
    }
    
    /// <summary>
    /// Ignore property
    /// </summary>
    public EntityTypeBuilder<T> Ignore<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        
        var propertyName = GetPropertyName(propertyExpression);
        _config.IgnoredProperties.Add(propertyName);
        
        return this;
    }
    
    /// <summary>
    /// Set custom collection name
    /// </summary>
    public EntityTypeBuilder<T> ToCollection(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
        
        _config.CollectionName = collectionName;
        return this;
    }
    
    private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        
        if (expression.Body is UnaryExpression unaryExpression && 
            unaryExpression.Operand is MemberExpression innerMember)
        {
            return innerMember.Member.Name;
        }
        
        throw new ArgumentException("Expression must be a property accessor", nameof(expression));
    }
}

/// <summary>
/// Fluent API for key configuration
/// </summary>
public class KeyBuilder
{
    private readonly IEntityConfigurationBase _config;
    
    internal KeyBuilder(IEntityConfigurationBase config)
    {
        _config = config;
    }
    
    /// <summary>
    /// Configure key with auto-increment
    /// </summary>
    public KeyBuilder AutoIncrement()
    {
        _config.AutoId = true;
        return this;
    }
    
    /// <summary>
    /// Configure value conversion for the key property (for ValueObject IDs)
    /// IMPORTANT: Only string conversion is supported (LiteDB limitation)
    /// </summary>
    /// <typeparam name="TKey">The ValueObject ID type (e.g., OrderId)</typeparam>
    /// <param name="toDb">Lambda to convert from ID to string (e.g., id => id.Value.ToString())</param>
    /// <param name="fromDb">Lambda to convert from string to ID (e.g., str => new OrderId(Guid.Parse(str)))</param>
    /// <example>
    /// <code>
    /// entity.HasKey(x => x.Id)
    ///     .HasConversion(
    ///         toDb: id => id.Value.ToString(),
    ///         fromDb: str => new OrderId(Guid.Parse(str))
    ///     );
    /// </code>
    /// </example>
    public KeyBuilder HasConversion<TKey>(
        Expression<Func<TKey, string>> toDb,
        Expression<Func<string, TKey>> fromDb)
    {
        if (toDb == null) throw new ArgumentNullException(nameof(toDb));
        if (fromDb == null) throw new ArgumentNullException(nameof(fromDb));
        
        // Store the lambda expressions as strings for the source generator to analyze
        // The source generator will parse the syntax tree to extract the body
        _config.IdConversionToDb = toDb.ToString();
        _config.IdConversionFromDb = fromDb.ToString();
        _config.IdConversionTargetType = "string"; // Always string for LiteDB
        
        return this;
    }
}

/// <summary>
/// Fluent API for property configuration
/// </summary>
public class PropertyBuilder<TProperty>
{
    private readonly PropertyConfiguration _config;
    
    internal PropertyBuilder(PropertyConfiguration config)
    {
        _config = config;
    }
    
    /// <summary>
    /// Mark property as required (not null)
    /// </summary>
    public PropertyBuilder<TProperty> IsRequired()
    {
        _config.IsRequired = true;
        return this;
    }
    
    /// <summary>
    /// Set maximum length (for strings)
    /// </summary>
    public PropertyBuilder<TProperty> HasMaxLength(int maxLength)
    {
        _config.MaxLength = maxLength;
        return this;
    }
    
    /// <summary>
    /// Create index on this property
    /// </summary>
    public IndexBuilder HasIndex(string? indexName = null)
    {
        _config.HasIndex = true;
        _config.IndexName = indexName;
        return new IndexBuilder(_config);
    }
}

/// <summary>
/// Fluent API for index configuration
/// </summary>
public class IndexBuilder
{
    private readonly PropertyConfiguration _config;
    
    internal IndexBuilder(PropertyConfiguration config)
    {
        _config = config;
    }
    
    /// <summary>
    /// Mark index as unique
    /// </summary>
    public IndexBuilder IsUnique()
    {
        _config.IsUnique = true;
        return this;
    }
}

#region Configuration Classes (internal)

internal interface IEntityConfiguration
{
    Type EntityType { get; }
}

internal interface IEntityConfigurationBase
{
    bool AutoId { get; set; }
    string? IdConversionToDb { get; set; }
    string? IdConversionFromDb { get; set; }
    string? IdConversionTargetType { get; set; }
}

internal class EntityConfiguration<T> : IEntityConfiguration, IEntityConfigurationBase where T : class
{
    public Type EntityType => typeof(T);
    public string? CollectionName { get; set; }
    public string? IdPropertyName { get; set; }
    public bool AutoId { get; set; }
    public string? IdConversionToDb { get; set; }
    public string? IdConversionFromDb { get; set; }
    public string? IdConversionTargetType { get; set; }
    public Dictionary<string, PropertyConfiguration> Properties { get; } = new();
    public HashSet<string> IgnoredProperties { get; } = new();
}

internal class PropertyConfiguration
{
    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }
    public bool HasIndex { get; set; }
    public string? IndexName { get; set; }
    public bool IsUnique { get; set; }
}

#endregion
