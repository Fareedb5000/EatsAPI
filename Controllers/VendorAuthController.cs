using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EatsAPI.Contracts;
using EatsAPI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace EatsAPI.Controllers;

[ApiController]
[Route("api/vendor/auth")]
public sealed class VendorAuthController(
    AppDbContext context,
    IPasswordHasher<AppUser> passwordHasher,
    IConfiguration configuration) : ControllerBase
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    [HttpPost("register")]
    [ProducesResponseType<VendorAuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VendorAuthResponse>> Register(
        [FromBody] CreateVendorRequest request,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var normalizedUserName = userName.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();

        if (await context.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail
                    || user.NormalizedUserName == normalizedUserName,
                cancellationToken))
        {
            return Conflict("The email address or username is already registered.");
        }

        var restaurant = new Restaurant
        {
            Name = request.StoreName.Trim(),
            Cuisine = request.Cuisine.Trim(),
            Rating = 0,
            DeliveryTime = request.DeliveryTime.Trim(),
            Price = request.Price.Trim(),
            ImageKey = request.ImageKey.ToLowerInvariant(),
            IsActive = true
        };
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            VendorRestaurant = restaurant
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        context.Users.Add(user);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateUser(exception))
        {
            return Conflict("The email address or username is already registered.");
        }

        return Created(
            "/api/vendor/restaurant",
            CreateAuthResponse(restaurant, user));
    }

    [HttpPost("signin")]
    [ProducesResponseType<VendorAuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<VendorAuthResponse>> SignIn(
        [FromBody] VendorSignInRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();
        var user = await context.Users
            .Include(candidate => candidate.VendorRestaurant)
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedUserName == normalizedUserName
                    && candidate.VendorRestaurantId != null,
                cancellationToken);

        if (user?.VendorRestaurant is null
            || passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Username or password is incorrect.");
        }

        return Ok(CreateAuthResponse(user.VendorRestaurant, user));
    }

    private VendorAuthResponse CreateAuthResponse(Restaurant restaurant, AppUser user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key must be configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "Savorly";
        var audience = configuration["Jwt:Audience"] ?? "Savorly";
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, "Vendor"),
            new Claim("vendor_restaurant_id", restaurant.Id.ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now,
            now.Add(TokenLifetime),
            credentials);

        return new VendorAuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            (int)TokenLifetime.TotalSeconds,
            ToResponse(restaurant));
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

    private static bool IsDuplicateUser(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
