using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.Core.Account;
using App.Core.Authentication;
using App.Data;
using App.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace App.API.IntegrationTests;

[TestClass]
public sealed class AccountIntegrationTests
{
    private AppWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new AppWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task GetAccount_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/account");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAccount_ForRegisteredUser_ReturnsAccountMetadata()
    {
        var auth = await RegisterAndAuthorizeAsync("alice@example.com", "alice", "Password123!");

        var response = await _client.GetAsync("/api/account");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.IsNotNull(account);
        Assert.AreEqual(auth.Id, account.Id);
        Assert.AreEqual("alice@example.com", account.Email);
        Assert.AreEqual("alice", account.Username);
        Assert.IsTrue(account.HasPassword);
        Assert.IsNull(account.ExternalProvider);
    }

    [TestMethod]
    public async Task UpdateProfile_UsernameChangeOnly_DoesNotRequirePassword()
    {
        await RegisterAndAuthorizeAsync("bob@example.com", "bob", "Password123!");

        var response = await _client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateProfileRequest(Username: "bobby", Email: "bob@example.com")
        );

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var updatedAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(updatedAuth);
        Assert.AreEqual("bobby", updatedAuth.Username);
        Assert.AreEqual("bob@example.com", updatedAuth.Email);
        Assert.IsFalse(string.IsNullOrWhiteSpace(updatedAuth.Token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            updatedAuth.Token
        );
        var me = await _client.GetFromJsonAsync<CurrentUserDto>("/api/auth/me");
        Assert.IsNotNull(me);
        Assert.AreEqual("bobby", me.Username);
    }

    [TestMethod]
    public async Task UpdateProfile_DuplicateUsername_ReturnsConflict()
    {
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("taken@example.com", "takenname", "Password123!")
        );
        await RegisterAndAuthorizeAsync("second@example.com", "second", "Password123!");

        var response = await _client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateProfileRequest(Username: "TakenName", Email: "second@example.com")
        );

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_EmailChangeWithoutOrWrongPassword_ReturnsBadRequest()
    {
        await RegisterAndAuthorizeAsync("carol@example.com", "carol", "Password123!");

        var missingPw = await _client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateProfileRequest(Username: "carol", Email: "carol.new@example.com")
        );
        Assert.AreEqual(HttpStatusCode.BadRequest, missingPw.StatusCode);

        var wrongPw = await _client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateProfileRequest(
                Username: "carol",
                Email: "carol.new@example.com",
                CurrentPassword: "WrongPassword!"
            )
        );
        Assert.AreEqual(HttpStatusCode.BadRequest, wrongPw.StatusCode);

        var validChange = await _client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateProfileRequest(
                Username: "carol",
                Email: "carol.new@example.com",
                CurrentPassword: "Password123!"
            )
        );
        Assert.AreEqual(HttpStatusCode.OK, validChange.StatusCode);

        var updatedAuth = await validChange.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(updatedAuth);
        Assert.AreEqual("carol.new@example.com", updatedAuth.Email);
    }

    [TestMethod]
    public async Task ChangePassword_UpdatesCredentialsAndAllowsLoginWithNewPassword()
    {
        await RegisterAndAuthorizeAsync("dave@example.com", "dave", "OldPassword123!");

        var badAttempt = await _client.PutAsJsonAsync(
            "/api/account/password",
            new ChangePasswordRequest(
                CurrentPassword: "WrongPassword!",
                NewPassword: "NewPassword456!"
            )
        );
        Assert.AreEqual(HttpStatusCode.BadRequest, badAttempt.StatusCode);

        var goodAttempt = await _client.PutAsJsonAsync(
            "/api/account/password",
            new ChangePasswordRequest(
                CurrentPassword: "OldPassword123!",
                NewPassword: "NewPassword456!"
            )
        );
        Assert.AreEqual(HttpStatusCode.OK, goodAttempt.StatusCode);

        using var anonClient = _factory.CreateClient();
        var oldLogin = await anonClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("dave@example.com", "OldPassword123!")
        );
        Assert.AreEqual(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await anonClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("dave@example.com", "NewPassword456!")
        );
        Assert.AreEqual(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [TestMethod]
    public async Task ExternalOnlyUser_CanChangeUsernameAndSetInitialPassword_CannotChangeEmailWithoutPassword()
    {
        Guid externalUserId;
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "sso@example.com",
                NormalizedEmail = "SSO@EXAMPLE.COM",
                Username = "ssouser",
                NormalizedUsername = "SSOUSER",
                PasswordHash = string.Empty,
                ExternalProvider = "oidc",
                ExternalSubjectId = "sub-12345",
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            externalUserId = user.Id;
            token = jwt.GenerateToken(new UserSummary(user.Id, user.Email, user.Username));
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var account = await _client.GetFromJsonAsync<AccountDto>("/api/account");
        Assert.IsNotNull(account);
        Assert.AreEqual(externalUserId, account.Id);
        Assert.IsFalse(account.HasPassword);
        Assert.AreEqual("oidc", account.ExternalProvider);

        // Email change is blocked when no password exists
        var emailBlocked = await _client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateProfileRequest(Username: "ssouser", Email: "different@example.com")
        );
        Assert.AreEqual(HttpStatusCode.BadRequest, emailBlocked.StatusCode);
        var problem = await emailBlocked.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.IsNotNull(problem);

        // Username change succeeds without password
        var usernameChanged = await _client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateProfileRequest(Username: "sso_renamed", Email: "sso@example.com")
        );
        Assert.AreEqual(HttpStatusCode.OK, usernameChanged.StatusCode);

        // Setting initial password succeeds without CurrentPassword
        var setPassword = await _client.PutAsJsonAsync(
            "/api/account/password",
            new ChangePasswordRequest(CurrentPassword: null, NewPassword: "InitialPassword123!")
        );
        Assert.AreEqual(HttpStatusCode.OK, setPassword.StatusCode);

        var refreshedAccount = await _client.GetFromJsonAsync<AccountDto>("/api/account");
        Assert.IsNotNull(refreshedAccount);
        Assert.IsTrue(refreshedAccount.HasPassword);
    }

    private async Task<AuthResponse> RegisterAndAuthorizeAsync(
        string email,
        string username,
        string password
    )
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, username, password)
        );
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            auth.Token
        );
        return auth;
    }
}
