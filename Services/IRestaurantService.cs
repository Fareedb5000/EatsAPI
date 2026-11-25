using EatsAPI.Contracts;

namespace EatsAPI.Services;

public interface IRestaurantService
{
    Task<IReadOnlyList<RestaurantResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RestaurantResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<RestaurantResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<RestaurantResponse> CreateAsync(RestaurantRequest request, CancellationToken cancellationToken);
    Task<RestaurantResponse?> UpdateAsync(int id, RestaurantRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
