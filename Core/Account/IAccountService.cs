using App.Core.Authentication;

namespace App.Core.Account;

public interface IAccountService
{
    Task<AccountDto?> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AuthResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default
    );

    Task<AuthResponse> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default
    );
}
