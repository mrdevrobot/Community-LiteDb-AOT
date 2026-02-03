namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// DDD Value Object - Immutable with private setters
/// Tests: Private/init-only setters support
/// </summary>
public class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    
    // Private constructor for DDD
    private Money() { }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        if (string.IsNullOrEmpty(currency))
            throw new ArgumentException("Currency is required", nameof(currency));
            
        Amount = amount;
        Currency = currency;
    }
    
    public override string ToString() => $"{Amount} {Currency}";
}

/// <summary>
/// Product entity using Value Object
/// </summary>
public class ProductWithMoney
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Money Price { get; private set; } = new Money(0, "USD");
    
    public void SetPrice(Money price)
    {
        Price = price ?? throw new ArgumentNullException(nameof(price));
    }
}
