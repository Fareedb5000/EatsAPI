using System.Security.Claims;
using EatsAPI.Contracts;
using EatsAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EatsAPI.Services.Storage;

namespace EatsAPI.Controllers;

[ApiController]
[Route("api/vendor")]
[Authorize(Roles = "Vendor")]
public sealed class VendorController(
    AppDbContext context,
    IMenuImageStorage menuImageStorage,
    ILogger<VendorController> logger) : ControllerBase
{
    private const long MaxMenuImageBytes = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> SupportedImageTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    [HttpGet("restaurant")]
    [ProducesResponseType<RestaurantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantResponse>> GetRestaurant(
        CancellationToken cancellationToken)
    {
        var restaurantId = GetRestaurantId();
        if (restaurantId is null) return Unauthorized();

        var restaurant = await context.Restaurants
            .AsNoTracking()
            .Where(item => item.Id == restaurantId.Value)
            .Select(item => new RestaurantResponse(
                item.Id,
                item.Name,
                item.Cuisine,
                item.Rating,
                item.DeliveryTime,
                item.Price,
                item.ImageKey,
                item.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpGet("menu")]
    [ProducesResponseType<IReadOnlyList<MenuItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MenuItemResponse>>> GetMenu(
        CancellationToken cancellationToken)
    {
        var restaurantId = GetRestaurantId();
        if (restaurantId is null) return Unauthorized();
        return Ok(await context.MenuItems
            .AsNoTracking()
            .Where(item => item.RestaurantId == restaurantId.Value)
            .OrderBy(item => item.Name)
            .Select(item => ToMenuResponse(item))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("menu-images")]
    [RequestSizeLimit(MaxMenuImageBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMenuImageBytes + 64 * 1024)]
    [ProducesResponseType<MenuImageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MenuImageResponse>> UploadMenuImage(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxMenuImageBytes)
        {
            return BadRequest("Choose an image smaller than 5 MB.");
        }

        if (!SupportedImageTypes.TryGetValue(file.ContentType, out var extension))
        {
            return BadRequest("Choose a JPEG, PNG, or WebP image.");
        }

        try
        {
            var imageUrl = await menuImageStorage.SaveAsync(file, extension, cancellationToken);
            return Ok(new MenuImageResponse(imageUrl));
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Menu image storage upload failed.");
            return Problem(
                "The menu image could not be stored. Please try again.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost("menu")]
    [ProducesResponseType<MenuItemResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MenuItemResponse>> CreateMenuItem(
        [FromBody] MenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var restaurantId = GetRestaurantId();
        if (restaurantId is null) return Unauthorized();
        if (!IsValidImageUrl(request.ImageUrl))
        {
            return BadRequest("Image URL must be an HTTP(S) URL or an uploaded menu image path.");
        }
        if (!await context.Restaurants.AnyAsync(
                restaurant => restaurant.Id == restaurantId.Value,
                cancellationToken))
        {
            return NotFound("The vendor restaurant was not found.");
        }

        var item = new MenuItem
        {
            RestaurantId = restaurantId.Value,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            ImageUrl = NormalizeImageUrl(request.ImageUrl),
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.MenuItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ToMenuResponse(item));
    }

    [HttpPut("menu/{id:int}")]
    [ProducesResponseType<MenuItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemResponse>> UpdateMenuItem(
        int id,
        [FromBody] MenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var restaurantId = GetRestaurantId();
        if (restaurantId is null) return Unauthorized();
        if (!IsValidImageUrl(request.ImageUrl))
        {
            return BadRequest("Image URL must be an HTTP(S) URL or an uploaded menu image path.");
        }
        var item = await context.MenuItems.SingleOrDefaultAsync(
            candidate => candidate.Id == id && candidate.RestaurantId == restaurantId.Value,
            cancellationToken);
        if (item is null) return NotFound();

        item.Name = request.Name.Trim();
        item.Description = request.Description.Trim();
        item.Price = request.Price;
        item.ImageUrl = NormalizeImageUrl(request.ImageUrl);
        item.IsAvailable = request.IsAvailable;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(ToMenuResponse(item));
    }

    [HttpDelete("menu/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMenuItem(
        int id,
        CancellationToken cancellationToken)
    {
        var restaurantId = GetRestaurantId();
        if (restaurantId is null) return Unauthorized();
        var item = await context.MenuItems.SingleOrDefaultAsync(
            candidate => candidate.Id == id && candidate.RestaurantId == restaurantId.Value,
            cancellationToken);
        if (item is null) return NotFound();

        context.MenuItems.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("orders")]
    [ProducesResponseType<IReadOnlyList<VendorOrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VendorOrderResponse>>> GetOrders(
        CancellationToken cancellationToken)
    {
        var restaurantId = GetRestaurantId();
        if (restaurantId is null) return Unauthorized();

        var orders = await context.Orders
            .AsNoTracking()
            .Where(order => order.RestaurantId == restaurantId.Value)
            .Include(order => order.Items)
            .OrderByDescending(order => order.PlacedAt)
            .ToListAsync(cancellationToken);
        return Ok(orders.Select(ToOrderResponse).ToList());
    }

    [HttpPut("orders/{id:int}/status")]
    [ProducesResponseType<VendorOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendorOrderResponse>> UpdateOrderStatus(
        int id,
        [FromBody] OrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var restaurantId = GetRestaurantId();
        if (restaurantId is null) return Unauthorized();
        var order = await context.Orders
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id
                    && candidate.RestaurantId == restaurantId.Value,
                cancellationToken);
        if (order is null) return NotFound();
        if (!CanTransition(order.Status, request.Status))
        {
            return BadRequest($"An order cannot move from {order.Status} to {request.Status}.");
        }

        order.Status = request.Status;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(ToOrderResponse(order));
    }

    private int? GetRestaurantId() =>
        int.TryParse(User.FindFirstValue("vendor_restaurant_id"), out var restaurantId)
            ? restaurantId
            : null;

    private static bool CanTransition(string current, string next) =>
        (current, next) is ("Placed", "Preparing" or "Cancelled")
            or ("Preparing", "Ready" or "Cancelled")
            or ("Ready", "Completed" or "Cancelled");

    private static string? NormalizeImageUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsValidImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        const string uploadPath = "/uploads/menu/";
        if (value.StartsWith(uploadPath, StringComparison.Ordinal))
        {
            var fileName = value[uploadPath.Length..];
            var extension = Path.GetExtension(fileName);
            return Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _)
                && SupportedImageTypes.Values.Any(
                    supported => string.Equals(supported, extension, StringComparison.OrdinalIgnoreCase));
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }

    private static MenuItemResponse ToMenuResponse(MenuItem item) =>
        new(
            item.Id,
            item.RestaurantId,
            item.Name,
            item.Description,
            item.Price,
            item.ImageUrl,
            item.IsAvailable);

    private static VendorOrderResponse ToOrderResponse(CustomerOrder order) =>
        new(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            order.CustomerPhone,
            order.DeliveryAddress,
            order.Subtotal,
            order.DeliveryFee,
            order.ServiceFee,
            order.Total,
            order.Status,
            order.PlacedAt,
            order.Items
                .OrderBy(item => item.Name)
                .Select(item => new OrderItemResponse(
                    item.Name,
                    item.UnitPrice,
                    item.Quantity))
                .ToList());
}
