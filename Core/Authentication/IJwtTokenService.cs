using System.Security.Claims;

namespace App.Core.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(UserSummary user);

    ClaimsPrincipal? ValidateToken(string token);
}
