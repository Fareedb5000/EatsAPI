namespace EatsAPI.Data;

public class Restaurant
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Cuisine { get; set; }
    public decimal Rating { get; set; }
    public required string DeliveryTime { get; set; }
    public required string Price { get; set; }
    public required string ImageKey { get; set; }
    public bool IsActive { get; set; }
}
