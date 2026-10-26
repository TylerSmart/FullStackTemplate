namespace App.Core.Authentication;

public sealed record UserSummary(
    Guid Id,
    string Email,
    string Username,
    string? ThemeSettings = null
);
