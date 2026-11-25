using System.ComponentModel.DataAnnotations;

namespace EatsAPI.Contracts;

/// <summary>Details required to create a Savorly account.</summary>
public sealed record SignUpRequest
{
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string FirstName { get; init; }

    [Required, StringLength(100, MinimumLength = 1), RegularExpression(@".*\S.*")]
    public required string LastName { get; init; }

    [Required, StringLength(50, MinimumLength = 3), RegularExpression(@".*\S.*")]
    public required string UserName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}

/// <summary>Credentials used to sign in to a Savorly account.</summary>
public sealed record SignInRequest
{
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}

/// <summary>Public profile information for an authenticated Savorly user.</summary>
public sealed record UserResponse(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName);

/// <summary>JWT credentials and profile returned after successful authentication.</summary>
public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserResponse User);
