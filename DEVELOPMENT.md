# Development Notes

## ? Completed (v0.1 - Prototype)

### Project Structure
- [x] Solution setup (`Community.LiteDB.Aot.sln`)
- [x] Runtime package (`Community.LiteDB.Aot`)
- [x] Sample application (`Community.LiteDB.Aot.Sample`)
- [x] Basic documentation (README.md)

### Core Components
- [x] `IEntityMapper<T>` - Interface for entity serializers
- [x] `AotLiteCollection<T>` - Type-safe collection wrapper
- [x] `LiteDbContext` - Base context class (EF Core style)
- [x] `EntityModelBuilder` - Fluent configuration API
- [x] Manual mapper example (CustomerMapper)

### Features
- [x] Insert/Update/Delete operations
- [x] String-based queries (AOT-safe)
- [x] FindById/FindOne/FindAll
- [x] Count/Exists aggregations
- [x] Transaction support (BeginTrans/Commit/Rollback)
- [x] Index management (EnsureIndex/DropIndex)

### Sample Application
- [x] Clean domain entity (Customer)
- [x] DbContext configuration (AppDbContext)
- [x] Comprehensive usage examples
- [x] Manual mapper implementation

## ?? In Progress

### v0.2 - Refinement
- [ ] Fix COUNT aggregation (use proper COUNT(*) query)
- [ ] Add more query helpers (OrderBy, Skip, Limit)
- [ ] Improve error messages
- [ ] Add XML documentation comments
- [ ] Unit tests setup

## ?? Next Steps (Priority Order)

### Phase 1: Stabilize Core (1-2 weeks)

1. **Fix Known Issues**
   - [ ] COUNT(*) aggregation query
   - [ ] Better AutoId handling in mappers
   - [ ] Proper disposal pattern

2. **Add Missing Operations**
   - [ ] Upsert support
   - [ ] Bulk operations optimization
   - [ ] Query with OrderBy/Skip/Limit fluent API

3. **Testing**
   - [ ] Unit tests for AotLiteCollection
   - [ ] Integration tests with real database
   - [ ] Performance benchmarks vs standard LiteDB

4. **Documentation**
   - [ ] API reference documentation
   - [ ] Migration guide from LiteDB
   - [ ] Best practices guide

### Phase 2: Source Generator (3-4 weeks)

1. **Setup Generator Project**
   - [ ] Create `Community.LiteDB.Aot.SourceGenerators` project
   - [ ] Add Roslyn dependencies
   - [ ] Setup incremental source generator

2. **Analyzer Implementation**
   - [ ] Find all `LiteDbContext` derived classes
   - [ ] Parse `OnModelCreating` method
   - [ ] Extract entity configurations
   - [ ] Build syntax tree for mappers

3. **Code Generation**
   - [ ] Generate `IEntityMapper<T>` implementations
   - [ ] Generate `RegisterMappers()` override
   - [ ] Handle nested types
   - [ ] Handle collections (List<T>, etc.)
   - [ ] Handle nullable types

4. **Validation**
   - [ ] Compile-time validation of configurations
   - [ ] Helpful error messages
   - [ ] Warnings for potential issues

### Phase 3: Enhanced API (2-3 weeks)

1. **Query Builder**
   ```csharp
   db.Customers.Query()
       .Where(c => c.Property("Age").GreaterThan(18))
       .OrderBy(c => c.Property("Name"))
       .Skip(10)
       .Limit(20)
       .ToList();
   ```

2. **Strongly-Typed Extensions**
   ```csharp
   db.Customers
       .Where(x => x.Age, op => op.GreaterThan(18))
       .OrderBy(x => x.Name)
       .ToList();
   ```

3. **Pre-Compiled Queries**
   ```csharp
   modelBuilder.Entity<Customer>()
       .HasQuery("ActiveAdults", q => q.Where("Age > 18 AND Active = true"));
   
   // Usage
   db.Customers.ExecuteQuery("ActiveAdults");
   ```

### Phase 4: Advanced Features (3-4 weeks)

1. **DbRef Support**
   ```csharp
   modelBuilder.Entity<Order>()
       .HasOne(x => x.Customer)
       .WithMany()
       .HasForeignKey(x => x.CustomerId);
   ```

2. **Change Tracking (Optional)**
   ```csharp
   var customer = db.Customers.FindById(1);
   customer.Name = "New Name";
   db.SaveChanges(); // Automatic update
   ```

3. **Async Support**
   ```csharp
   await db.Customers.InsertAsync(customer);
   var result = await db.Customers.FindAsync("Age > 18");
   ```

## ?? Design Decisions

### Why Manual Mappers for v0.1?
- **Quick prototype**: Validate concept before investing in source generator
- **Learning**: Understand mapping requirements
- **Templates**: Manual mappers serve as templates for generator

### Why String-Based Queries?
- **AOT Compatible**: No `Expression.Compile()` needed
- **Powerful**: LiteDB's BsonExpression is very capable
- **Familiar**: Similar to SQL and MongoDB queries
- **Type-Safe Alternative**: Can add fluent API later

### Why Separate Package?
- **Zero Risk**: LiteDB codebase unchanged
- **Progressive**: Users can migrate gradually
- **Trimming**: Unused parts automatically removed
- **Community**: Independent development pace

## ?? Performance Considerations

### Current Implementation
- ? **Direct engine access**: No extra overhead
- ? **Compiled serializers**: Will be fast when generated
- ?? **String parsing**: BsonExpression parsing on each query
- ?? **No caching**: Could cache parsed expressions

### Optimizations Planned
1. **Query Caching**: Cache parsed BsonExpression instances
2. **Bulk Operations**: Optimize InsertBulk/UpdateBulk
3. **Pre-Compiled Queries**: Parse queries at compile-time
4. **Connection Pooling**: Reuse engine instances

## ?? Technical Notes

### Trimming Analysis
When publishing with AOT:
```xml
<PublishTrimmed>true</PublishTrimmed>
<PublishAot>true</PublishAot>
```

Expected trimming results:
- ? `LiteDB.Engine.*` - Kept (used directly)
- ? `LiteDB.Document.*` - Kept (BsonDocument/Value)
- ? `LiteDB.Engine.Query` - Kept (query execution)
- ? `LiteDB.Client.Mapper.*` - Trimmed (~30%)
- ? `LiteDB.Client.Mapper.Linq.*` - Trimmed (~5%)
- ? `LiteDB.Client.Mapper.Reflection.*` - Trimmed (~5%)

**Total size reduction**: ~40% of LiteDB package

### Source Generator Challenges

1. **Roslyn Complexity**: Need to parse C# syntax trees
2. **Nested Types**: Handle complex property graphs
3. **Nullable Context**: Respect nullable reference types
4. **Error Reporting**: Provide clear diagnostics
5. **Performance**: Incremental generation for large projects

### Compatibility Notes

- **Minimum**: .NET 8.0 (for AOT support)
- **LiteDB Version**: >= 5.0.21
- **Database Format**: 100% compatible with standard LiteDB

## ?? Known Issues

1. **COUNT aggregation**: Returns document instead of scalar
   - **Status**: Fixed in latest commit
   - **Solution**: Iterate and count instead of COUNT(*)

2. **AutoId handling**: Manual mapper needs special logic
   - **Status**: Workaround in place
   - **Solution**: Source generator will handle automatically

3. **No async support**: All operations are synchronous
   - **Status**: Design decision (LiteDB engine is sync)
   - **Workaround**: Wrap calls in Task.Run if needed

## ?? Resources

### LiteDB Internals
- [ILiteEngine Interface](https://github.com/litedb-org/LiteDB/blob/master/LiteDB/Engine/ILiteEngine.cs)
- [BsonExpression](https://github.com/litedb-org/LiteDB/blob/master/LiteDB/Document/Expression/BsonExpression.cs)
- [Query Class](https://github.com/litedb-org/LiteDB/blob/master/LiteDB/Client/Structures/Query.cs)

### Source Generators
- [Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [Incremental Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)
- [System.Text.Json Generator](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Text.Json/gen)

### AOT Compatibility
- [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Prepare libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)

## ?? Success Criteria

### v1.0 Release Criteria
- [x] Core runtime package works
- [x] Source generator generates mappers automatically
- [x] Supports all primitive types + collections
- [ ] 100% test coverage for core features
- [ ] Documentation complete
- [ ] Sample apps for common scenarios
- [ ] Performance benchmarks show < 5% overhead
- [ ] Published to NuGet

### Community Success
- Adoption by at least 3 real projects
- Positive feedback on API design
- Active contributors
- Integration into LiteDB ecosystem

---

**Last Updated**: 2025-02-02
**Version**: 0.1.0-prototype
**Status**: ? Working prototype with manual mappers
