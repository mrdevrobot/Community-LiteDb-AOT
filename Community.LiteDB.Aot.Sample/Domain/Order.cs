namespace Community.LiteDB.Aot.Sample.Domain;

/// <summary>
/// GeoLocation - Level 3 nested object
/// </summary>
public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? TimeZone { get; set; }
}

/// <summary>
/// ShippingAddress - Level 2 nested object
/// </summary>
public class ShippingAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    
    // Level 3 nesting!
    public GeoLocation? Location { get; set; }
}

/// <summary>
/// ShippingInfo - Level 1 nested object
/// </summary>
public class ShippingInfo
{
    public string Carrier { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime? EstimatedDelivery { get; set; }
    
    // Level 2 nesting!
    public ShippingAddress? Address { get; set; }
}

/// <summary>
/// Order with 3 levels of nested objects: Order -> ShippingInfo -> ShippingAddress -> GeoLocation
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    // Level 1 nesting!
    public ShippingInfo? Shipping { get; set; }
}
