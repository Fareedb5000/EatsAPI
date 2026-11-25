namespace EatsAPI.Services.Storage;

public sealed class LocalMenuImageStorage(IWebHostEnvironment environment) : IMenuImageStorage
{
    public async Task<string> SaveAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadDirectory = Path.Combine(webRoot, "uploads", "menu");
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadDirectory, fileName);
        await using var stream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/menu/{fileName}";
    }
}
