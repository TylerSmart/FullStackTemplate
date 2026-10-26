using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace App.Client.E2ETests;

[TestClass]
public sealed class ClientSmokeTests : PageTest
{
    private PlaywrightServerFixture? _server;

    [TestInitialize]
    public async Task SetupAsync()
    {
        _server = await PlaywrightServerFixture.StartAsync();
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task CompleteE2EFlow_Register_CreateCatalogItem_CreateRecipe_ScaleAndConvert_MobileNav()
    {
        var serverUrl = _server!.ServerUrl;

        // 1. Navigate to Login Page
        await Page.GotoAsync($"{serverUrl}/login");
        await Expect(Page.Locator("h1")).ToContainTextAsync("Welcome to LocalRoast");
        await Expect(Page.Locator(".auth-card-header p"))
            .ToContainTextAsync("Sign in to access your household cookbook");

        // 2. Switch to Register tab and register new household user
        var uniqueEmail = $"chef_{Guid.NewGuid():N}@example.com";
        var uniqueUsername = $"chef_{Guid.NewGuid():N}"[..12];

        // Click "Create Account" tab
        await Page.Locator(".mat-mdc-tab", new() { HasText = "Create Account" }).ClickAsync();

        var registerForm = Page.Locator("form[aria-label='Create account form']");
        await registerForm.Locator("input[formcontrolname='username']").FillAsync(uniqueUsername);
        await registerForm.Locator("input[formcontrolname='email']").FillAsync(uniqueEmail);
        await registerForm
            .Locator("input[formcontrolname='password']")
            .FillAsync("KitchenSecret123!");
        await registerForm
            .Locator("input[formcontrolname='confirmPassword']")
            .FillAsync("KitchenSecret123!");

        var submitBtn = registerForm.Locator("button[type='submit']");
        await submitBtn.ClickAsync();

        // 3. Verify redirected to Cookbook / Recipes page
        await Expect(Page)
            .ToHaveURLAsync(
                new System.Text.RegularExpressions.Regex(".*/recipes.*"),
                new() { Timeout = 15000 }
            );
        await Expect(Page.Locator(".brand-title")).ToContainTextAsync("LocalRoast");
        await Expect(Page.Locator(".user-name")).ToContainTextAsync(uniqueUsername);

        // 4. Navigate to Food Catalog and create a food item
        await Page.Locator("nav.desktop-nav a", new() { HasText = "Food Catalog" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/catalog.*"));
        await Expect(Page.Locator(".page-title")).ToContainTextAsync("Canonical Food Catalog");

        // Click "Add Food Item"
        await Page.Locator("button", new() { HasText = "Add Food Item" }).ClickAsync();
        await Expect(Page.Locator("mat-dialog-container h2")).ToContainTextAsync("New Food Item");

        var testFoodItemName = $"San Marzano Tomatoes {Guid.NewGuid():N}"[..25];
        await Page.Locator("input[formcontrolname='name']").FillAsync(testFoodItemName);
        await Page.Locator("input[formcontrolname='aliasesText']")
            .FillAsync("Canned Plum Tomatoes, Pelati");
        await Page.Locator("mat-checkbox[formcontrolname='isPantryStaple'] label").ClickAsync();

        await Page.Locator("mat-dialog-actions button", new() { HasText = "Create Item" })
            .ClickAsync();

        // Verify item card appears in catalog
        await Expect(Page.Locator(".food-item-card", new() { HasText = testFoodItemName }))
            .ToBeVisibleAsync();

        // 5. Navigate to Create Recipe
        await Page.Locator("nav.desktop-nav a", new() { HasText = "Cookbook" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/recipes.*"));

        await Page.Locator("a", new() { HasText = "New Recipe" }).ClickAsync();
        await Expect(Page)
            .ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/recipes/new.*"));
        await Expect(Page.Locator(".page-title")).ToContainTextAsync("New Recipe");

        var recipeTitle = "Classic Marinara Sauce";
        await Page.Locator("input[formcontrolname='title']").FillAsync(recipeTitle);
        await Page.Locator("input[formcontrolname='baseServings']").FillAsync("4");
        await Page.Locator("input[formcontrolname='prepTimeMinutes']").FillAsync("10");
        await Page.Locator("input[formcontrolname='cookTimeMinutes']").FillAsync("30");
        await Page.Locator("textarea[formcontrolname='description']")
            .FillAsync("Simple rich tomato sauce");

        // Fill ingredient
        await Page.Locator("input[formcontrolname='displayName']")
            .First.FillAsync(testFoodItemName);
        await Page.Locator("input[formcontrolname='quantity']").First.FillAsync("2");

        // Select Unit: Cup
        await Page.Locator("mat-select[formcontrolname='unit']").First.ClickAsync();
        await Page.Locator("mat-option", new() { HasText = "Cup (cup)" }).ClickAsync();

        // Fill Step Instruction
        await Page.Locator("textarea[formcontrolname='instruction']")
            .First.FillAsync("Simmer tomatoes over medium heat until thickened.");

        // Add Step Timer
        await Page.Locator(".add-timer-btn").First.ClickAsync();
        await Page.Locator("input[formcontrolname='minutes']").First.FillAsync("20");
        await Page.Locator("input[formcontrolname='label']").First.FillAsync("Simmer gently");

        // Submit recipe
        await Page.Locator("button[type='submit']", new() { HasText = "Save Recipe" })
            .First.ClickAsync();

        // 6. Verify Recipe Detail Page
        await Expect(Page)
            .ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/recipes/[a-f0-9-]+$"));
        await Expect(Page.Locator(".recipe-title")).ToContainTextAsync(recipeTitle);
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("2 cups");
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync(testFoodItemName);
        await Expect(Page.Locator(".timer-badge")).ToContainTextAsync("20 min");

        // 7. Test Live 2x Scaling
        await Page.Locator("button.mult-chip", new() { HasText = "2x" }).ClickAsync();
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("4 cups");

        // 8. Test Metric Unit Switching
        await Page.Locator("mat-button-toggle", new() { HasText = "Metric" }).ClickAsync();
        // 4 cups is ~946 ml -> in metric >= 10 rounds to nearest ml
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("946 ml");

        // 9. Verify 360px Mobile Viewport and Bottom Navigation Bar
        await Page.SetViewportSizeAsync(360, 640);
        await Expect(Page.Locator("nav.mobile-bottom-nav")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav.mobile-bottom-nav a", new() { HasText = "Cookbook" }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator("nav.mobile-bottom-nav a", new() { HasText = "Catalog" }))
            .ToBeVisibleAsync();
    }
}
