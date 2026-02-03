# Community.LiteDB.Aot

**AOT-compatible wrapper for LiteDB** - Use LiteDB with Native AOT compilation and Clean Architecture

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Community.LiteDB.Aot)](https://www.nuget.org/packages/Community.LiteDB.Aot)

## :dart: Overview

`Community.LiteDB.Aot` is a thin, AOT-compatible layer on top of [LiteDB](https://github.com/litedb-org/LiteDB) that enables:

- :zap: **Native AOT compilation** - Full support for .NET 8 Native AOT
- :building_construction: **Clean Architecture** - Zero dependencies in domain entities
- :art: **EF Core-style API** - Familiar `DbContext` pattern
- :floppy_disk: **Same database format** - Compatible with standard LiteDB files
- :arrows_counterclockwise: **Progressive migration** - Use alongside existing LiteDB code
- :rocket: **Source Generators** - Compile-time mapper generation with Expression Trees
- :dart: **DDD Support** - Value Objects, Strongly-Typed IDs, Private Setters
- :label: **Data Annotations** - [Key], [Required], [MaxLength], [Range] support

## :sparkles: Key Features

### 1. **Native AOT Compilation**
Full support for .NET 8 Native AOT with automatic trimming of reflection-based code (~30-40% size reduction).

### 2. **Clean Architecture Support**
Your domain entities stay **pure** - zero infrastructure dependencies:

```csharp
// Clean domain entity
public class Order
{
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. **Source Generator with Expression Trees**
Compile-time mapper generation using Expression Trees for:
- :white_check_mark: **Private setters** support (DDD Value Objects)
- :white_check_mark: **Complex nested objects** (3+ levels deep)
- :white_check_mark: **Collections of nested objects** (List<T>)
- :white_check_mark: **Near-native performance** (compiled delegates)
- :white_check_mark: **Full AOT compatibility** (no runtime reflection)

### 4. **Strongly-Typed IDs (DDD Value Objects)**
Support for strongly-typed IDs to avoid primitive obsession:

```csharp
public class OrderId
{
    public Guid Value { get; private set; }
    
    public OrderId(Guid value) => Value = value;
    public static OrderId NewId() => new(Guid.NewGuid());
}

public class Order
{
    public OrderId Id { get; private set; } = OrderId.NewId();
    public string CustomerName { get; set; }
    // ...
}
```

### 5. **Data Annotations Support**
Full support for standard Data Annotations attributes:

```csharp
using System.ComponentModel.DataAnnotations;

public class UserProfile
{
    [Key]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; }
    
    [Range(18, 120)]
    public int Age { get; set; }
}
```

Supported attributes:
- `[Key]` - Primary key detection (auto or manual)
- `[Required]` - Not null validation
- `[MaxLength]` - String length validation
- `[Range]` - Numeric range validation

### 6. **Complex Nested Objects**
Support for deeply nested objects and collections:

```csharp
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // List of nested objects
    public List<Address> Offices { get; set; } = new();
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public Location Coordinates { get; set; }  // 2nd level nesting
}
```

### 7. **DDD Value Objects with Private Setters**
Full support for immutable Value Objects:

```csharp
public class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}

public class Product
{
    public int Id { get; set; }
    public Money Price { get; private set; }
    
    public void SetPrice(Money price) => Price = price;
}
```

## :package: Quick Start

### Installation

```bash
dotnet add package Community.LiteDB.Aot
dotnet add package Community.LiteDB.Aot.SourceGenerators
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
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });
    }
}

// 3. Usage
using var db = new AppDbContext("myapp.db");

// Insert
var customer = new Customer { Name = "John", Email = "john@example.com", Age = 30 };
db.Customers.Insert(customer);

// Query
var adults = db.Customers.FindAll().Where(c => c.Age >= 18).ToList();
var john = db.Customers.FindById(new BsonValue(1));

// Update
customer.Age = 31;
db.Customers.Update(customer);

// Delete
db.Customers.Delete(new BsonValue(1));
```

## :building_construction: Architecture

```
+-------------------------------------+
|  Your Application (AOT-published)  |
+-------------------------------------+
|  Community.LiteDB.Aot (NEW)        |  <- Thin wrapper
|  - LiteDbContext                   |  <- Configuration API
|  - AotLiteCollection<T>            |  <- Type-safe collections
+-------------------------------------+
|  Source Generator (Compile-time)   |  <- Expression Trees
|  - IEntityMapper<T>                |  <- BsonDocument <-> T
|  - Expression Trees for setters    |  <- Private setters support
+-------------------------------------+
|  LiteDB 5.0 (UNCHANGED)            |  <- Core engine
|  + Engine.* (kept by trimmer)      |
|  + Document.* (kept by trimmer)    |
|  - Client.Mapper.* (TRIMMED)       |  <- Reflection removed!
+-------------------------------------+
```

**Key benefit**: The AOT trimmer automatically removes unused reflection-based parts of LiteDB (~30-40% size reduction)

## :books: Advanced Examples

### Using [Key] Attribute

```csharp
using System.ComponentModel.DataAnnotations;

public class Product
{
    [Key]  // <- Source generator automatically detects this
    public int ProductId { get; set; }
    
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// DbContext - NO configuration needed for [Key]!
public partial class AppDbContext : LiteDbContext
{
    public AotLiteCollection<Product> Products => Collection<Product>();
    
    protected override void OnModelCreating(EntityModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToCollection("products");
        });
    }
}
```

### Transactions

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

### Indexes and Constraints

```csharp
modelBuilder.Entity<Customer>(entity =>
{
    entity.Property(x => x.Email)
          .HasIndex("idx_email")
          .IsUnique();
    
    entity.Property(x => x.City)
          .HasIndex("idx_city");
    
    entity.Property(x => x.Name)
          .IsRequired()
          .HasMaxLength(100);
});
```

## :arrows_counterclockwise: Migration from Standard LiteDB

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

## :package: Package Structure

### Community.LiteDB.Aot
Runtime package (~50 KB)
- Core interfaces and collections
- `LiteDbContext` base class
- `AotLiteCollection<T>` wrapper
- Depends on `LiteDB >= 5.0.21`

### Community.LiteDB.Aot.SourceGenerators
Compile-time source generator
- Automatic `IEntityMapper<T>` generation
- Expression Trees for property setters
- Support for private setters and nested objects
- [Key] attribute detection
- Data Annotations validation

## :test_tube: Testing

The project includes comprehensive test suites:

- **KeyAttributeTests** - [Key] attribute detection and auto-increment
- **PrivateSetterTests** - DDD Value Objects with private setters
- **ValueObjectIdTests** - Strongly-typed IDs (OrderId, CustomerId, etc.)
- **NestedObjectTests** - Complex nested objects and collections
- **DataAnnotationsTests** - Validation attributes support

Run tests:
```bash
dotnet test
```

## :bar_chart: Benchmarks

Run benchmarks to compare performance:

```bash
cd Community.LiteDB.Aot.Benchmarks
dotnet run -c Release
```

Expected results:
- **Expression Trees** - Near-native performance for property access
- **Compiled delegates** - 100-1000x faster than reflection
- **AOT trimming** - 30-40% smaller executable size

## :handshake: Contributing

Contributions are welcome! Please open issues or pull requests on [GitHub](https://github.com/mrdevrobot/Community-LiteDb-AOT).

## :page_facing_up: License

This project is licensed under the MIT License.

## :link: Links

- **Repository**: https://github.com/mrdevrobot/Community-LiteDb-AOT
- **NuGet Package**: https://www.nuget.org/packages/Community.LiteDB.Aot
- **LiteDB**: https://github.com/litedb-org/LiteDB

## :pray: Credits

Built on top of the excellent [LiteDB](https://github.com/litedb-org/LiteDB) project by Mauricio David.

---

**Made with :heart: by MrDevRobot**

