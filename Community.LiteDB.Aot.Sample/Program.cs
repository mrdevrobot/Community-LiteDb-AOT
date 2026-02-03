using Community.LiteDB.Aot.Sample.Domain;
using Community.LiteDB.Aot.Sample.Infrastructure;
using LiteDB;

Console.WriteLine("========================================");
Console.WriteLine("Community.LiteDB.Aot - Sample Demo");
Console.WriteLine("========================================\n");

// Use temporary database
var dbPath = Path.Combine(Path.GetTempPath(), "community_litedb_aot_sample.db");
Console.WriteLine($"Database path: {dbPath}\n");

// Clean up old database
if (File.Exists(dbPath))
{
    File.Delete(dbPath);
}

try
{
    using var db = new AppDbContext(dbPath);
    
    Console.WriteLine("=== INSERT ===");
    
    // Insert single customer
    var customer1 = new Customer
    {
        Name = "John Doe",
        Email = "john@example.com",
        Age = 30,
        City = "New York"
    };
    
    db.Customers.Insert(customer1);
    Console.WriteLine($"Inserted: {customer1.Name} (ID: {customer1.Id})");
    
    // Insert multiple customers
    var customers = new[]
    {
        new Customer { Name = "Jane Smith", Email = "jane@example.com", Age = 25, City = "Los Angeles" },
        new Customer { Name = "Bob Johnson", Email = "bob@example.com", Age = 35, City = "Chicago" },
        new Customer { Name = "Alice Brown", Email = "alice@example.com", Age = 28, City = "New York" },
        new Customer { Name = "Charlie Wilson", Email = "charlie@example.com", Age = 42, City = "Houston" }
    };
    
    db.Customers.InsertBulk(customers);
    Console.WriteLine($"Inserted {customers.Length} more customers\n");
    
    Console.WriteLine("=== QUERY - FindById ===");
    var found = db.Customers.FindById(new BsonValue(1));
    Console.WriteLine($"Found: {found?.Name} - {found?.Email}\n");
    
    Console.WriteLine("=== QUERY - String-based (AOT-safe) ===");
    var adults = db.Customers.Find("Age >= @minAge", new BsonValue(30));
    Console.WriteLine($"Customers with Age >= 30:");
    foreach (var c in adults)
    {
        Console.WriteLine($"  - {c.Name}, {c.Age} years, {c.City}");
    }
    Console.WriteLine();
    
    Console.WriteLine("=== QUERY - LIKE operator ===");
    var newYorkers = db.Customers.Find("City = 'New York'");
    Console.WriteLine($"Customers in New York:");
    foreach (var c in newYorkers)
    {
        Console.WriteLine($"  - {c.Name}");
    }
    Console.WriteLine();
    
    Console.WriteLine("=== UPDATE ===");
    customer1.Age = 31;
    customer1.City = "Boston";
    db.Customers.Update(customer1);
    Console.WriteLine($"Updated {customer1.Name}: Age={customer1.Age}, City={customer1.City}\n");
    
    Console.WriteLine("=== AGGREGATION ===");
    var total = db.Customers.Count();
    var activeCount = db.Customers.Count("Active = true");
    Console.WriteLine($"Total customers: {total}");
    Console.WriteLine($"Active customers: {activeCount}\n");
    
    Console.WriteLine("=== TRANSACTION ===");
    db.BeginTrans();
    try
    {
        var newCustomer = new Customer
        {
            Name = "Test Transaction",
            Email = "test@example.com",
            Age = 99,
            City = "Test City"
        };
        
        db.Customers.Insert(newCustomer);
        Console.WriteLine($"Inserted in transaction: {newCustomer.Name}");
        
        // Rollback
        db.Rollback();
        Console.WriteLine("Transaction rolled back\n");
    }
    catch
    {
        db.Rollback();
        throw;
    }
    
    Console.WriteLine("=== DELETE ===");
    db.Customers.Delete(new BsonValue(1));
    Console.WriteLine("Deleted customer ID=1\n");
    
    Console.WriteLine("=== FINAL COUNT ===");
    var finalCount = db.Customers.Count();
    Console.WriteLine($"Remaining customers: {finalCount}\n");
    
    Console.WriteLine("=== ALL CUSTOMERS ===");
    var all = db.Customers.FindAll();
    foreach (var c in all)
    {
        Console.WriteLine($"  {c.Id}. {c.Name} - {c.Email} - {c.City} (Age: {c.Age})");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("\n========================================");
Console.WriteLine("Demo completed!");
Console.WriteLine("========================================");

