using System.ComponentModel.DataAnnotations;

namespace EatsAPI.Contracts;

/// <summary>Credentials for the Savorly administrator account.</summary>
public sealed record AdminLoginRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public required string UserName { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public required string Password { get; init; }
}

/// <summary>Bearer token issued to an authenticated administrator.</summary>
public sealed record AdminLoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn);
