using System.ComponentModel.DataAnnotations;

namespace EatsAPI.Contracts;

/// <summary>Restaurant fields that can be changed by an administrator.</summary>
public sealed record RestaurantRequest
{
    [Required, StringLength(150, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string Name { get; init; }

    [Required, StringLength(150, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string Cuisine { get; init; }

    [Range(typeof(decimal), "0", "5")]
    public decimal Rating { get; init; }

    [Required, StringLength(50, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string DeliveryTime { get; init; }

    [Required, StringLength(20, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string Price { get; init; }

    [Required, RegularExpression("^(italian|sushi)$")]
    public required string ImageKey { get; init; }

    public bool IsActive { get; init; }
}

/// <summary>Restaurant details returned by the Savorly API.</summary>
public sealed record RestaurantResponse(
    int Id,
    string Name,
    string Cuisine,
    decimal Rating,
    string DeliveryTime,
    string Price,
    string ImageKey,
    bool IsActive);
