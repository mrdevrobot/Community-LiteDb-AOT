# Community.LiteDB.Aot

**AOT-compatible wrapper for LiteDB** - Use LiteDB with Native AOT compilation and Clean Architecture

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Community.LiteDB.Aot)](https://www.nuget.org/packages/Community.LiteDB.Aot)

## ?? Overview

`Community.LiteDB.Aot` is a thin, AOT-compatible layer on top of [LiteDB](https://github.com/litedb-org/LiteDB) that enables:

- ? **Native AOT compilation** - Full support for .NET 8 Native AOT
- ? **Clean Architecture** - Zero dependencies in domain entities
- ? **EF Core-style API** - Familiar `DbContext` pattern
- ? **Same database format** - Compatible with standard LiteDB files
- ? **Progressive migration** - Use alongside existing LiteDB code

## ?? Quick Start

### Installation

```bash
dotnet add package Community.LiteDB.Aot
```

### Basic Usage

```csharp
// 1. Domain Entity (Clean - no dependencies)
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// 2. DbContext (Infrastructure layer)
public partial class AppDbContext : LiteDbContext
{
    public AotLiteCollection<Customer> Customers => Collection<Customer>();
    
    public AppDbContext(string filename) : base(filename) { }
    
    protected override void OnModelCreating(EntityModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.Id).AutoIncrement();
            entity.Property(x => x.Email).HasIndex().IsUnique();
        });
    }
}

// 3. Usage
using var db = new AppDbContext("myapp.db");

// Insert
var customer = new Customer { Name = "John", Email = "john@example.com", Age = 30 };
db.Customers.Insert(customer);

// Query (string-based, AOT-safe)
var adults = db.Customers.Find("Age >= 18");
var john = db.Customers.FindById(new BsonValue(1));

// Update
customer.Age = 31;
db.Customers.Update(customer);

// Delete
db.Customers.Delete(new BsonValue(1));
```

## ?? Architecture

```
???????????????????????????????????????
?  Your Application (AOT-published)   ?
???????????????????????????????????????
?  Community.LiteDB.Aot (NEW)         ?  ? Thin wrapper
?  - LiteDbContext                    ?  ? Configuration API
?  - AotLiteCollection<T>             ?  ? Type-safe collections
???????????????????????????????????????
?  LiteDB 5.0 (UNCHANGED)             ?  ? Core engine
?  ? Engine.* (kept by trimmer)      ?
?  ? Document.* (kept by trimmer)    ?
?  ? Client.Mapper.* (TRIMMED)       ?  ? Reflection removed!
???????????????????????????????????????
```

**Key benefit**: The AOT trimmer automatically removes unused reflection-based parts of LiteDB (~30-40% size reduction)

## ?? Key Features

### 1. Clean Architecture Support

Your domain entities stay **pure** - zero infrastructure dependencies:

```csharp
// ? Clean domain entity
public class Order
{
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Configuration is separate (infrastructure layer)
modelBuilder.Entity<Order>(entity =>
{
    entity.HasKey(x => x.OrderId);
    entity.Property(x => x.Total).IsRequired();
});
```

### 2. String-Based Queries (AOT-Safe)

No LINQ expressions that require compilation:

```csharp
// Simple queries
db.Customers.Find("Age > 18")
db.Customers.Find("City = 'NYC' AND Active = true")

// Parameterized (injection-safe)
db.Customers.Find("Name LIKE @search", new BsonValue("%John%"))

// Complex conditions
db.Customers.Find(@"
    (Age >= 18 AND City = 'NYC') OR 
    (VIP = true AND TotalOrders > 100)
")
```

### 3. Transactions

```csharp
db.BeginTrans();
try
{
    db.Customers.Insert(customer);
    db.Orders.Insert(order);
    db.Commit();
}
catch
{
    db.Rollback();
    throw;
}
```

### 4. Indexes

```csharp
modelBuilder.Entity<Customer>(entity =>
{
    entity.Property(x => x.Email)
          .HasIndex("idx_email")
          .IsUnique();
    
    entity.Property(x => x.City)
          .HasIndex("idx_city");
});
```

## ?? Migration from Standard LiteDB

### Gradual Migration

You can use **both** packages in the same application:

```csharp
// Old code (reflection-based)
var oldDb = new LiteDatabase("app.db");
var customersOld = oldDb.GetCollection<Customer>();

// New code (AOT-compatible)
var newDb = new AppDbContext("app.db");
var customersNew = newDb.Customers;

// Both access THE SAME database file!
```

### Migration Steps

1. **Install** `Community.LiteDB.Aot` (keep LiteDB installed)
2. **Create** your `DbContext` class
3. **Migrate** repositories one at a time
4. **Remove** old LiteDB code when done

## ?? Package Structure

- **Community.LiteDB.Aot** - Runtime package (~50 KB)
  - Core interfaces and collections
  - Depends on `LiteDB >= 5.0.21`

- **Community.LiteDB.Aot.SourceGenerators** *(Coming soon)*
  - Compile-time code generation
  - Automatic mapper creation from `OnModelCreating`

## ?? Current Limitations

### Manual Mappers Required

For now, you need to create mappers manually:

```csharp
// TEMPORARY: Manual mapper (will be generated automatically later)
public partial class AppDbContext
{
    protected override void RegisterMappers()
    {
        RegisterMapper(new CustomerMapper());
    }
}

internal class CustomerMapper : IEntityMapper<Customer>
{
    public BsonDocument Serialize(Customer entity) => new BsonDocument
    {
        ["_id"] = entity.Id,
        ["Name"] = entity.Name,
        // ...
    };
    
    public Customer Deserialize(BsonDocument doc) => new Customer
    {
        Id = doc["_id"].AsInt32,
        Name = doc["Name"].AsString,
        // ...
    };
    
    // ... other interface members
}
```

**Coming soon**: Source generator will create these automatically!

### String-Based Queries Only

No LINQ support yet (not AOT-compatible):

```csharp
// ? Not supported (requires Expression.Compile)
db.Customers.Find(x => x.Age > 18)

// ? Use string-based instead
db.Customers.Find("Age > 18")
```

## ??? Roadmap

### v1.0 (Current) - MVP
- [x] Core runtime package
- [x] LiteDbContext base class
- [x] AotLiteCollection<T>
- [x] String-based queries
- [x] Manual mapper interface
- [x] Sample project

### v1.1 (Next) - Source Generator
- [ ] Incremental source generator
- [ ] Automatic mapper generation from `OnModelCreating`
- [ ] Compile-time validation
- [ ] Better error messages

### v1.2 - Enhanced Queries
- [ ] Fluent query builder API
- [ ] Strongly-typed query extensions
- [ ] Pre-compiled queries

### v2.0 - Advanced Features
- [ ] DbRef support
- [ ] Change tracking (optional)
- [ ] Migrations support
- [ ] Async API

## ?? Contributing

Contributions are welcome! This is a **community project**.

1. Fork the repository
2. Create your feature branch
3. Add tests
4. Submit a pull request

## ?? License

MIT License - see [LICENSE](LICENSE) file for details

## ?? Acknowledgments

- Built on top of [LiteDB](https://github.com/litedb-org/LiteDB) by Maurício David
- Inspired by Entity Framework Core's DbContext pattern
- Community-driven project

## ?? Support

- **Issues**: [GitHub Issues](https://github.com/litedb-org/LiteDB/issues)
- **Discussions**: [GitHub Discussions](https://github.com/litedb-org/LiteDB/discussions)
- **Documentation**: [LiteDB Wiki](https://github.com/litedb-org/LiteDB/wiki)

---

**Made with ?? by the LiteDB community**
