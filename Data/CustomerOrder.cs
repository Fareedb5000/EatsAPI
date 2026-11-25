namespace EatsAPI.Data;

public sealed class CustomerOrder
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public Guid CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public required string CustomerPhone { get; set; }
    public required string DeliveryAddress { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Placed";
    public DateTimeOffset PlacedAt { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public AppUser Customer { get; set; } = null!;
    public ICollection<CustomerOrderItem> Items { get; set; } = [];
}

public sealed class CustomerOrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public required string Name { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public CustomerOrder Order { get; set; } = null!;
}
