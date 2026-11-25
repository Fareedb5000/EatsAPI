using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EatsAPI.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EatsAPI.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminAuthController(
    IConfiguration configuration) : ControllerBase
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    [HttpPost("login")]
    [ProducesResponseType<AdminLoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AdminLoginResponse> Login([FromBody] AdminLoginRequest request)
    {
        var adminUserName = configuration["Admin:UserName"];
        var adminPassword = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminUserName)
            || string.IsNullOrWhiteSpace(adminPassword))
        {
            return Problem(
                "Administrator credentials are not configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!IsMatch(request.UserName, adminUserName)
            || !IsMatch(request.Password, adminPassword))
        {
            return Unauthorized("Username or password is incorrect.");
        }

        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key must be configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "Savorly";
        var audience = configuration["Jwt:Audience"] ?? "Savorly";
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "savorly-admin"),
            new Claim(ClaimTypes.Name, adminUserName),
            new Claim(ClaimTypes.Role, "Admin")
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

        return Ok(new AdminLoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            (int)TokenLifetime.TotalSeconds));
    }

    private static bool IsMatch(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}
