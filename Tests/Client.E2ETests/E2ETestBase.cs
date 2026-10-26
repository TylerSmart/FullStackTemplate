using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace App.Client.E2ETests;

public abstract class E2ETestBase : PageTest
{
    protected string ServerUrl => AssemblyInitializer.ServerUrl;

    protected async Task<(string Username, string Email)> RegisterAndLoginAsync(
        string? username = null,
        string? email = null,
        string password = "KitchenSecret123!"
    )
    {
        username ??= $"chef_{Guid.NewGuid():N}"[..12];
        email ??= $"chef_{Guid.NewGuid():N}@example.com";

        await Page.GotoAsync($"{ServerUrl}/login");
        await Page.Locator(".mat-mdc-tab", new() { HasText = "Create Account" }).ClickAsync();

        var registerForm = Page.Locator("form[aria-label='Create account form']");
        await registerForm.Locator("input[formcontrolname='username']").FillAsync(username);
        await registerForm.Locator("input[formcontrolname='email']").FillAsync(email);
        await registerForm.Locator("input[formcontrolname='password']").FillAsync(password);
        await registerForm.Locator("input[formcontrolname='confirmPassword']").FillAsync(password);

        await registerForm.Locator("button[type='submit']").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes.*"), new() { Timeout = 15000 });

        return (username, email);
    }
}
