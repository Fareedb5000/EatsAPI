using System.Security.Claims;
using EatsAPI.Contracts;
using EatsAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatsAPI.Controllers;

[ApiController]
[Route("api")]
public sealed class CustomerOrdersController(AppDbContext context) : ControllerBase
{
    private const decimal DeliveryFee = 2.99m;
    private const decimal ServiceFee = 1.99m;

    [HttpPost("orders")]
    [Authorize]
    [ProducesResponseType<VendorOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VendorOrderResponse>> PlaceOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            return Unauthorized();
        }

        var customer = await context.Users.SingleOrDefaultAsync(
            user => user.Id == customerId,
            cancellationToken);
        if (customer is null) return Unauthorized();

        var restaurantIsActive = await context.Restaurants.AnyAsync(
            restaurant => restaurant.Id == request.RestaurantId && restaurant.IsActive,
            cancellationToken);
        if (!restaurantIsActive)
        {
            return BadRequest("This restaurant is not currently accepting orders.");
        }

        var requestedItems = request.Items
            .GroupBy(item => item.MenuItemId)
            .Select(group => new { MenuItemId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToList();
        if (requestedItems.Any(item => item.Quantity > 50))
        {
            return BadRequest("A menu item quantity cannot exceed 50.");
        }

        var itemIds = requestedItems.Select(item => item.MenuItemId).ToArray();
        var menuItems = await context.MenuItems
            .Where(item => item.RestaurantId == request.RestaurantId
                && item.IsAvailable
                && itemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        if (menuItems.Count != itemIds.Length)
        {
            return BadRequest("One or more selected menu items are no longer available.");
        }

        var orderItems = requestedItems.Select(requested =>
        {
            var menuItem = menuItems.Single(item => item.Id == requested.MenuItemId);
            return new CustomerOrderItem
            {
                Name = menuItem.Name,
                UnitPrice = menuItem.Price,
                Quantity = requested.Quantity
            };
        }).ToList();
        var subtotal = orderItems.Sum(item => item.UnitPrice * item.Quantity);
        var order = new CustomerOrder
        {
            RestaurantId = request.RestaurantId,
            CustomerId = customer.Id,
            CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
            CustomerEmail = customer.Email,
            CustomerPhone = request.CustomerPhone.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            Subtotal = subtotal,
            DeliveryFee = DeliveryFee,
            ServiceFee = ServiceFee,
            Total = subtotal + DeliveryFee + ServiceFee,
            Status = "Placed",
            PlacedAt = DateTimeOffset.UtcNow,
            Items = orderItems
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ToOrderResponse(order));
    }

    [HttpGet("orders/mine")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<CustomerOrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerOrderResponse>>> GetMyOrders(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            return Unauthorized();
        }

        var orders = await context.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == customerId)
            .Include(order => order.Restaurant)
            .Include(order => order.Items)
            .OrderByDescending(order => order.PlacedAt)
            .ToListAsync(cancellationToken);

        return Ok(orders.Select(order => new CustomerOrderResponse(
            order.Id,
            order.Restaurant.Name,
            order.Subtotal,
            order.DeliveryFee,
            order.ServiceFee,
            order.Total,
            order.Status,
            order.PlacedAt,
            order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemResponse(
                    item.Name,
                    item.UnitPrice,
                    item.Quantity))
                .ToList())).ToList());
    }

    [HttpGet("orders/{id:int}")]
    [Authorize]
    [ProducesResponseType<CustomerOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerOrderResponse>> GetMyOrder(
        int id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            return Unauthorized();
        }

        var order = await context.Orders
            .AsNoTracking()
            .Where(candidate => candidate.Id == id && candidate.CustomerId == customerId)
            .Include(candidate => candidate.Restaurant)
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null) return NotFound();

        return Ok(new CustomerOrderResponse(
            order.Id,
            order.Restaurant.Name,
            order.Subtotal,
            order.DeliveryFee,
            order.ServiceFee,
            order.Total,
            order.Status,
            order.PlacedAt,
            order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemResponse(
                    item.Name,
                    item.UnitPrice,
                    item.Quantity))
                .ToList()));
    }

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
            order.Items.Select(item => new OrderItemResponse(
                item.Name,
                item.UnitPrice,
                item.Quantity)).ToList());
}
