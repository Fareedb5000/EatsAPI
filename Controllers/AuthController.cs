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
[Route("api/auth")]
public class AuthController(
    AppDbContext context,
    IPasswordHasher<AppUser> passwordHasher,
    IConfiguration configuration) : ControllerBase
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp(
        [FromBody] SignUpRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var userName = request.UserName.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var normalizedUserName = userName.ToUpperInvariant();

        if (await context.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail
                    || user.NormalizedUserName == normalizedUserName,
                cancellationToken))
        {
            return Conflict("The email address or username is already registered.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
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

        return StatusCode(StatusCodes.Status201Created, CreateAuthResponse(user));
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn(
        [FromBody] SignInRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await context.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Email or password is incorrect.");
        }

        return Ok(CreateAuthResponse(user));
    }

    private AuthResponse CreateAuthResponse(AppUser user)
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
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("given_name", user.FirstName),
            new Claim("family_name", user.LastName)
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

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            (int)TokenLifetime.TotalSeconds,
            new UserResponse(
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName));
    }

    private static bool IsDuplicateUser(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
