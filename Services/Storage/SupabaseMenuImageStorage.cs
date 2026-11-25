using System.Net.Http.Headers;

namespace EatsAPI.Services.Storage;

public sealed class SupabaseMenuImageStorage(
    HttpClient httpClient,
    IConfiguration configuration) : IMenuImageStorage
{
    public async Task<string> SaveAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var supabaseUrl = configuration["Storage:SupabaseUrl"]!.TrimEnd('/');
        var serviceRoleKey = configuration["Storage:SupabaseServiceRoleKey"]!;
        var bucket = configuration["Storage:MenuImageBucket"] ?? "menu-images";
        var objectPath = $"menu/{Guid.NewGuid():N}{extension}";
        var escapedBucket = Uri.EscapeDataString(bucket);
        var escapedObjectPath = string.Join(
            '/',
            objectPath.Split('/').Select(Uri.EscapeDataString));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{supabaseUrl}/storage/v1/object/{escapedBucket}/{escapedObjectPath}");
        request.Headers.Add("apikey", serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
        request.Headers.Add("x-upsert", "false");
        request.Content = new StreamContent(file.OpenReadStream());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Supabase Storage returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        var publicUrl =
            $"{supabaseUrl}/storage/v1/object/public/{escapedBucket}/{escapedObjectPath}";
        return publicUrl;
    }
}
