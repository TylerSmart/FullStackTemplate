using System.ComponentModel.DataAnnotations;

namespace App.Core.Account;

public sealed record AccountDto(
    Guid Id,
    string Email,
    string Username,
    bool HasPassword,
    string? ExternalProvider
);

public sealed record UpdateProfileRequest(
    [Required, MinLength(3), MaxLength(100)] string Username,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(100)] string? CurrentPassword = null
);

public sealed record ChangePasswordRequest(
    [MaxLength(100)] string? CurrentPassword,
    [Required, MinLength(6), MaxLength(100)] string NewPassword
);
