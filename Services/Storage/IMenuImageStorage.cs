namespace EatsAPI.Services.Storage;

public interface IMenuImageStorage
{
    Task<string> SaveAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken);
}
