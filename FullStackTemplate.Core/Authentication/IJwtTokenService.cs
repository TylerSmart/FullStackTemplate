using System.Security.Claims;

namespace FullStackTemplate.Core.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(UserSummary user);

    ClaimsPrincipal? ValidateToken(string token);
}
