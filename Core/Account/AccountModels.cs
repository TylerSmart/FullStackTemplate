using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace App.Core.Account;

public sealed record AccountDto(
    Guid Id,
    string Email,
    string Username,
    bool HasPassword,
    string? ExternalProvider,
    string? ThemeSettings = null
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

public sealed record UpdateThemeSettingsRequest(JsonElement Settings);
