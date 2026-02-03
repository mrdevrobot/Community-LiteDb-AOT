namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// DDD ValueObject: Strongly-typed ID for Order
/// This is a typical DDD pattern to avoid primitive obsession
/// </summary>
public class OrderId
{
    public Guid Value { get; private set; }
    
    private OrderId() { } // For serialization
    
    public OrderId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty", nameof(value));
        
        Value = value;
    }
    
    public static OrderId NewId() => new(Guid.NewGuid());
    
    public override string ToString() => Value.ToString();
    public override bool Equals(object? obj) => obj is OrderId other && Value.Equals(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>
/// Order entity using strongly-typed OrderId as primary key
/// </summary>
public class OrderWithStronglyTypedId
{
    public OrderId Id { get; private set; } = OrderId.NewId();
    
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    
    // Private constructor for deserialization
    private OrderWithStronglyTypedId() { }
    
    public OrderWithStronglyTypedId(string customerName, decimal totalAmount)
    {
        CustomerName = customerName;
        TotalAmount = totalAmount;
    }
}
