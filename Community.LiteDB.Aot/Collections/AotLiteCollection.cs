using LiteDB;
using LiteDB.Engine;
using Community.LiteDB.Aot.Mapping;

namespace Community.LiteDB.Aot.Collections;

/// <summary>
/// Type-safe collection that uses ILiteEngine directly (AOT-compatible).
/// Wraps LiteDB engine operations with compile-time generated mappers.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class AotLiteCollection<T> where T : class
{
    private readonly ILiteEngine _engine;
    private readonly IEntityMapper<T> _mapper;
    
    internal AotLiteCollection(ILiteEngine engine, IEntityMapper<T> mapper)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }
    
    #region Insert
    
    /// <summary>
    /// Insert a new entity into collection. Returns the ID of the inserted document.
    /// If entity has an AutoId field, it will be populated after insert.
    /// </summary>
    public BsonValue Insert(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var doc = _mapper.Serialize(entity);
        var count = _engine.Insert(_mapper.CollectionName, new[] { doc }, BsonAutoId.Int32);
        
        var id = doc["_id"];
        
        // Set generated ID back to entity if insertion succeeded
        if (count > 0)
        {
            _mapper.SetId(entity, id);
        }
        
        return id;
    }
    
    /// <summary>
    /// Insert multiple entities into collection. Returns number of inserted documents.
    /// </summary>
    public int InsertBulk(IEnumerable<T> entities)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        
        var docs = entities.Select(e => _mapper.Serialize(e));
        return _engine.Insert(_mapper.CollectionName, docs, BsonAutoId.Int32);
    }
    
    #endregion
    
    #region Update
    
    /// <summary>
    /// Update an entity in collection. Returns number of updated documents.
    /// </summary>
    public int Update(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var doc = _mapper.Serialize(entity);
        return _engine.Update(_mapper.CollectionName, new[] { doc });
    }
    
    /// <summary>
    /// Update multiple documents using transform expression and predicate.
    /// Example: UpdateMany("Age = @newAge", "City = @city", new BsonValue(30), new BsonValue("NYC"))
    /// </summary>
    public int UpdateMany(string transform, string predicate, params BsonValue[] args)
    {
        if (string.IsNullOrEmpty(transform)) throw new ArgumentNullException(nameof(transform));
        
        var transformExpr = BsonExpression.Create(transform);
        var predicateExpr = string.IsNullOrEmpty(predicate) ? null : BsonExpression.Create(predicate, args);
        
        return _engine.UpdateMany(_mapper.CollectionName, transformExpr, predicateExpr);
    }
    
    #endregion
    
    #region Delete
    
    /// <summary>
    /// Delete a document by ID. Returns number of deleted documents.
    /// </summary>
    public int Delete(BsonValue id)
    {
        if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));
        
        return _engine.Delete(_mapper.CollectionName, new[] { id });
    }
    
    /// <summary>
    /// Delete multiple documents using predicate expression.
    /// Example: DeleteMany("Age < @minAge", new BsonValue(18))
    /// </summary>
    public int DeleteMany(string predicate, params BsonValue[] args)
    {
        if (string.IsNullOrEmpty(predicate)) throw new ArgumentNullException(nameof(predicate));
        
        var predicateExpr = BsonExpression.Create(predicate, args);
        return _engine.DeleteMany(_mapper.CollectionName, predicateExpr);
    }
    
    #endregion
    
    #region Query
    
    /// <summary>
    /// Find a document by ID. Returns null if not found.
    /// </summary>
    public T? FindById(BsonValue id)
    {
        if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));
        
        var query = new Query 
        { 
            Select = BsonExpression.Create("$"),
            Limit = 1
        };
        query.Where.Add(BsonExpression.Create($"_id = {id}"));
        
        using var reader = _engine.Query(_mapper.CollectionName, query);
        
        if (reader.Read())
        {
            return _mapper.Deserialize(reader.Current.AsDocument);
        }
        
        return null;
    }
    
    /// <summary>
    /// Find documents using string-based predicate (AOT-safe).
    /// Example: Find("Age > @minAge AND City = @city", new BsonValue(18), new BsonValue("NYC"))
    /// </summary>
    public IEnumerable<T> Find(string? predicate = null, params BsonValue[] args)
    {
        var query = new Query 
        { 
            Select = BsonExpression.Create("$"),
            Limit = int.MaxValue
        };
        
        if (!string.IsNullOrEmpty(predicate))
        {
            query.Where.Add(BsonExpression.Create(predicate, args));
        }
        
        using var reader = _engine.Query(_mapper.CollectionName, query);
        
        while (reader.Read())
        {
            yield return _mapper.Deserialize(reader.Current.AsDocument);
        }
    }
    
    /// <summary>
    /// Find all documents in collection.
    /// </summary>
    public IEnumerable<T> FindAll()
    {
        return Find(null);
    }
    
    /// <summary>
    /// Find first document matching predicate. Returns null if not found.
    /// </summary>
    public T? FindOne(string? predicate = null, params BsonValue[] args)
    {
        return Find(predicate, args).FirstOrDefault();
    }
    
    #endregion
    
    #region Aggregation
    
    /// <summary>
    /// Count documents matching predicate.
    /// </summary>
    public int Count(string? predicate = null, params BsonValue[] args)
    {
        var query = new Query 
        { 
            Select = BsonExpression.Create("_id") // Use _id instead of COUNT(*)
        };
        
        if (!string.IsNullOrEmpty(predicate))
        {
            query.Where.Add(BsonExpression.Create(predicate, args));
        }
        
        using var reader = _engine.Query(_mapper.CollectionName, query);
        
        int count = 0;
        while (reader.Read())
        {
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// Check if any document matches predicate.
    /// </summary>
    public bool Exists(string? predicate = null, params BsonValue[] args)
    {
        return Count(predicate, args) > 0;
    }
    
    #endregion
    
    #region Index
    
    /// <summary>
    /// Ensure index exists on collection.
    /// </summary>
    public bool EnsureIndex(string name, string expression, bool unique = false)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrEmpty(expression)) throw new ArgumentNullException(nameof(expression));
        
        var expr = BsonExpression.Create(expression);
        return _engine.EnsureIndex(_mapper.CollectionName, name, expr, unique);
    }
    
    /// <summary>
    /// Drop index from collection.
    /// </summary>
    public bool DropIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        
        return _engine.DropIndex(_mapper.CollectionName, name);
    }
    
    #endregion
}
