using EatsAPI.Contracts;
using EatsAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace EatsAPI.Services;

public sealed class RestaurantService(AppDbContext context) : IRestaurantService
{
    public async Task<IReadOnlyList<RestaurantResponse>> GetActiveAsync(
        CancellationToken cancellationToken) =>
        await context.Restaurants
            .AsNoTracking()
            .Where(restaurant => restaurant.IsActive)
            .OrderBy(restaurant => restaurant.Name)
            .Select(restaurant => ToResponse(restaurant))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RestaurantResponse>> GetAllAsync(
        CancellationToken cancellationToken) =>
        await context.Restaurants
            .AsNoTracking()
            .OrderBy(restaurant => restaurant.Name)
            .Select(restaurant => ToResponse(restaurant))
            .ToListAsync(cancellationToken);

    public async Task<RestaurantResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var restaurant = await context.Restaurants
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return restaurant is null ? null : ToResponse(restaurant);
    }

    public async Task<RestaurantResponse> CreateAsync(
        RestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var restaurant = new Restaurant
        {
            Name = request.Name.Trim(),
            Cuisine = request.Cuisine.Trim(),
            Rating = request.Rating,
            DeliveryTime = request.DeliveryTime.Trim(),
            Price = request.Price.Trim(),
            ImageKey = request.ImageKey.ToLowerInvariant(),
            IsActive = request.IsActive
        };
        context.Restaurants.Add(restaurant);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(restaurant);
    }

    public async Task<RestaurantResponse?> UpdateAsync(
        int id,
        RestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var restaurant = await context.Restaurants
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (restaurant is null) return null;

        restaurant.Name = request.Name.Trim();
        restaurant.Cuisine = request.Cuisine.Trim();
        restaurant.Rating = request.Rating;
        restaurant.DeliveryTime = request.DeliveryTime.Trim();
        restaurant.Price = request.Price.Trim();
        restaurant.ImageKey = request.ImageKey.ToLowerInvariant();
        restaurant.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(restaurant);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var restaurant = await context.Restaurants
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (restaurant is null) return false;

        context.Restaurants.Remove(restaurant);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static RestaurantResponse ToResponse(Restaurant restaurant) =>
        new(
            restaurant.Id,
            restaurant.Name,
            restaurant.Cuisine,
            restaurant.Rating,
            restaurant.DeliveryTime,
            restaurant.Price,
            restaurant.ImageKey,
            restaurant.IsActive);
}
