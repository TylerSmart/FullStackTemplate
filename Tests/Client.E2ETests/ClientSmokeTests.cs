using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace App.Client.E2ETests;

[TestClass]
public sealed class ClientSmokeTests : E2ETestBase
{
    [TestMethod]
    public async Task CompleteE2EFlow_Register_CreateCatalogItem_CreateRecipe_ScaleAndConvert_MobileNav()
    {
        // 1. Register and verify authenticated landing on recipes
        var (username, _) = await RegisterAndLoginAsync();
        await Expect(Page.Locator(".brand-title")).ToContainTextAsync("LocalRoast");
        await Expect(Page.Locator(".user-name")).ToContainTextAsync(username);

        // 2. Navigate to Food Catalog and create a food item
        await Page.Locator("nav.desktop-nav a", new() { HasText = "Food Catalog" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/catalog.*"));
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

        // 3. Navigate to Create Recipe
        await Page.Locator("nav.desktop-nav a", new() { HasText = "Cookbook" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes.*"));

        await Page.Locator("a[routerlink='/recipes/new']").First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes/new.*"));
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

        // 4. Verify Recipe Detail Page
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes/[a-f0-9-]+$"));
        await Expect(Page.Locator(".recipe-title")).ToContainTextAsync(recipeTitle);
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("2 cups");
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync(testFoodItemName);
        await Expect(Page.Locator(".timer-badge")).ToContainTextAsync("20 min");

        // 5. Test Live 2x Scaling
        await Page.Locator("button.mult-chip", new() { HasText = "2x" }).ClickAsync();
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("4 cups");

        // 6. Test Metric Unit Switching
        await Page.Locator("mat-button-toggle", new() { HasText = "Metric" }).ClickAsync();
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("946 ml");

        // 7. Verify 360px Mobile Viewport and Bottom Navigation Bar
        await Page.SetViewportSizeAsync(360, 640);
        await Expect(Page.Locator("nav.mobile-bottom-nav")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav.mobile-bottom-nav a", new() { HasText = "Cookbook" }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator("nav.mobile-bottom-nav a", new() { HasText = "Catalog" }))
            .ToBeVisibleAsync();
    }
}
