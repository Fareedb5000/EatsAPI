using EatsAPI.Contracts;
using EatsAPI.Data;
using EatsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatsAPI.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController(
    IRestaurantService restaurantService,
    AppDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<RestaurantResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RestaurantResponse>>> GetActive(
        CancellationToken cancellationToken) =>
        Ok(await restaurantService.GetActiveAsync(cancellationToken));

    [HttpGet("{restaurantId:int}")]
    [AllowAnonymous]
    [ProducesResponseType<RestaurantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantResponse>> GetActiveById(
        int restaurantId,
        CancellationToken cancellationToken)
    {
        var restaurant = await restaurantService.GetByIdAsync(restaurantId, cancellationToken);
        return restaurant is null || !restaurant.IsActive ? NotFound() : Ok(restaurant);
    }

    [HttpGet("{restaurantId:int}/menu")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<MenuItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MenuItemResponse>>> GetMenu(
        int restaurantId,
        CancellationToken cancellationToken)
    {
        if (!await context.Restaurants.AnyAsync(
                restaurant => restaurant.Id == restaurantId && restaurant.IsActive,
                cancellationToken))
        {
            return NotFound();
        }

        return Ok(await context.MenuItems
            .AsNoTracking()
            .Where(item => item.RestaurantId == restaurantId && item.IsAvailable)
            .OrderBy(item => item.Name)
            .Select(item => new MenuItemResponse(
                item.Id,
                item.RestaurantId,
                item.Name,
                item.Description,
                item.Price,
                item.ImageUrl,
                item.IsAvailable))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IReadOnlyList<RestaurantResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RestaurantResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await restaurantService.GetAllAsync(cancellationToken));

    [HttpGet("admin/{id:int}", Name = nameof(GetById))]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RestaurantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var restaurant = await restaurantService.GetByIdAsync(id, cancellationToken);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpPost("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RestaurantResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RestaurantResponse>> Create(
        [FromBody] RestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var restaurant = await restaurantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
    }

    [HttpPut("admin/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RestaurantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantResponse>> Update(
        int id,
        [FromBody] RestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var restaurant = await restaurantService.UpdateAsync(id, request, cancellationToken);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpDelete("admin/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await restaurantService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
