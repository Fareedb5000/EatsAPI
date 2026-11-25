using System.ComponentModel.DataAnnotations;

namespace EatsAPI.Contracts;

/// <summary>Details required to create a vendor account and store.</summary>
public sealed record CreateVendorRequest
{
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string FirstName { get; init; }

    [Required, StringLength(100, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string LastName { get; init; }

    [Required, StringLength(50, MinimumLength = 3), RegularExpression(@".*\S.*")]
    public required string UserName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }

    [Required, StringLength(150, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string StoreName { get; init; }

    [Required, StringLength(150, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string Cuisine { get; init; }

    [Required, StringLength(50, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string DeliveryTime { get; init; }

    [Required, StringLength(20, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string Price { get; init; }

    [Required, RegularExpression("^(italian|sushi)$")]
    public required string ImageKey { get; init; }
}

/// <summary>Credentials used to sign in to a vendor account.</summary>
public sealed record VendorSignInRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public required string UserName { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}

/// <summary>JWT credentials and store details returned after vendor authentication.</summary>
public sealed record VendorAuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    RestaurantResponse Restaurant);

public sealed record MenuItemRequest
{
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string Name { get; init; }

    [Required, StringLength(500), RegularExpression(@".*\S.*")]
    public required string Description { get; init; }

    [Range(typeof(decimal), "0.01", "10000")]
    public decimal Price { get; init; }

    [StringLength(500)]
    public string? ImageUrl { get; init; }

    public bool IsAvailable { get; init; }
}

public sealed record MenuItemResponse(
    int Id,
    int RestaurantId,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable);

public sealed record MenuImageResponse(string ImageUrl);

public sealed record CreateOrderRequest
{
    [Range(1, int.MaxValue)]
    public int RestaurantId { get; init; }

    [Required, StringLength(300, MinimumLength = 5), RegularExpression(@".*\S.*")]
    public required string DeliveryAddress { get; init; }

    [Required, Phone, StringLength(30, MinimumLength = 7)]
    public required string CustomerPhone { get; init; }

    [Required, MinLength(1), MaxLength(50)]
    public required IReadOnlyList<CreateOrderItemRequest> Items { get; init; }
}

public sealed record CreateOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int MenuItemId { get; init; }

    [Range(1, 50)]
    public int Quantity { get; init; }
}

public sealed record OrderStatusRequest
{
    [Required, RegularExpression("^(Preparing|Ready|Completed|Cancelled)$")]
    public required string Status { get; init; }
}

public sealed record OrderItemResponse(
    string Name,
    decimal UnitPrice,
    int Quantity);

public sealed record VendorOrderResponse(
    int Id,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string DeliveryAddress,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal ServiceFee,
    decimal Total,
    string Status,
    DateTimeOffset PlacedAt,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record CustomerOrderResponse(
    int Id,
    string RestaurantName,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal ServiceFee,
    decimal Total,
    string Status,
    DateTimeOffset PlacedAt,
    IReadOnlyList<OrderItemResponse> Items);
