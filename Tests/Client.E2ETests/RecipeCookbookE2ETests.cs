using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace App.Client.E2ETests;

[TestClass]
public sealed class RecipeCookbookE2ETests : E2ETestBase
{
    [TestMethod]
    public async Task Recipe_CreateWithSectionsAndTimers_DetailRendersAndScales()
    {
        await RegisterAndLoginAsync();

        // 1. Navigate to New Recipe
        await Page.Locator("a[routerlink='/recipes/new']").First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes/new.*"));
        await Expect(Page.Locator(".page-title")).ToContainTextAsync("New Recipe");

        var recipeTitle = $"Tuscan Bean Soup {Guid.NewGuid():N}"[..24];
        await Page.Locator("input[formcontrolname='title']").FillAsync(recipeTitle);
        await Page.Locator("input[formcontrolname='baseServings']").FillAsync("6");
        await Page.Locator("input[formcontrolname='yieldDisplay']").FillAsync("6 hearty bowls");
        await Page.Locator("input[formcontrolname='prepTimeMinutes']").FillAsync("15");
        await Page.Locator("input[formcontrolname='cookTimeMinutes']").FillAsync("45");
        await Page.Locator("textarea[formcontrolname='description']")
            .FillAsync("Classic rustic Tuscan soup with cannellini beans.");

        // First Ingredient
        await Page.Locator("input[formcontrolname='sectionName']").First.FillAsync("Soup Base");
        await Page.Locator("input[formcontrolname='displayName']")
            .First.FillAsync("Cannellini Beans");
        await Page.Locator("input[formcontrolname='quantity']").First.FillAsync("2");
        await Page.Locator("mat-select[formcontrolname='unit']").First.ClickAsync();
        await Page.Locator("mat-option", new() { HasText = "Can" }).First.ClickAsync();
        await Page.Locator("input[formcontrolname='preparationNote']")
            .First.FillAsync("rinsed and drained");

        // First Step
        await Page.Locator(
                "input[formcontrolname='sectionName']",
                new() { HasNot = Page.Locator("input[formcontrolname='displayName']") }
            )
            .First.FillAsync("Simmering");
        await Page.Locator("textarea[formcontrolname='instruction']")
            .First.FillAsync("Combine beans and broth in pot; simmer gently.");

        // Step Timer
        await Page.Locator(".add-timer-btn").First.ClickAsync();
        await Page.Locator("input[formcontrolname='minutes']").First.FillAsync("30");
        await Page.Locator("input[formcontrolname='label']").First.FillAsync("Low simmer");

        // Save
        await Page.Locator("button[type='submit']", new() { HasText = "Save Recipe" })
            .First.ClickAsync();

        // 2. Verify Recipe Detail Page
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes/[a-f0-9-]+$"));
        await Expect(Page.Locator(".recipe-title")).ToContainTextAsync(recipeTitle);
        await Expect(Page.Locator(".recipe-stats-strip")).ToContainTextAsync("6 hearty bowls");
        await Expect(Page.Locator(".recipe-stats-strip")).ToContainTextAsync("15 min");
        await Expect(Page.Locator(".recipe-stats-strip")).ToContainTextAsync("45 min");

        // Verify Ingredients
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("2 cans");
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("Cannellini Beans");
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("rinsed and drained");

        // Verify Step Timer
        await Expect(Page.Locator(".timer-badge")).ToContainTextAsync("30 min");

        // 3. Test Live 2x Scaling
        await Page.Locator("button.mult-chip", new() { HasText = "2x" }).ClickAsync();
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("4 cans");

        // 4. Test Live 0.5x Scaling
        await Page.Locator("button.mult-chip", new() { HasText = "0.5x" }).ClickAsync();
        await Expect(Page.Locator(".ingredient-row")).ToContainTextAsync("1 can");
    }

    [TestMethod]
    public async Task Recipe_FilterByStatus_AndSearchCookbook()
    {
        await RegisterAndLoginAsync();

        // Create Recipe 1: Draft
        await Page.Locator("a[routerlink='/recipes/new']").First.ClickAsync();
        var draftTitle = $"Draft Goulash {Guid.NewGuid():N}"[..22];
        await Page.Locator("input[formcontrolname='title']").FillAsync(draftTitle);
        await Page.Locator("mat-select[formcontrolname='status']").ClickAsync();
        await Page.Locator("mat-option", new() { HasText = "Draft" }).ClickAsync();
        await Page.Locator("input[formcontrolname='displayName']").First.FillAsync("Beef Shank");
        await Page.Locator("input[formcontrolname='quantity']").First.FillAsync("1");
        await Page.Locator("textarea[formcontrolname='instruction']")
            .First.FillAsync("Brown meat in batches.");
        await Page.Locator("button[type='submit']", new() { HasText = "Save Recipe" })
            .First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes/[a-f0-9-]+$"));

        // Create Recipe 2: Published
        await Page.Locator("nav.desktop-nav a", new() { HasText = "Cookbook" }).ClickAsync();
        await Page.Locator("a[routerlink='/recipes/new']").First.ClickAsync();
        var pubTitle = $"Published Risotto {Guid.NewGuid():N}"[..24];
        await Page.Locator("input[formcontrolname='title']").FillAsync(pubTitle);
        await Page.Locator("mat-select[formcontrolname='status']").ClickAsync();
        await Page.Locator("mat-option", new() { HasText = "Published" }).ClickAsync();
        await Page.Locator("input[formcontrolname='displayName']").First.FillAsync("Arborio Rice");
        await Page.Locator("input[formcontrolname='quantity']").First.FillAsync("2");
        await Page.Locator("textarea[formcontrolname='instruction']")
            .First.FillAsync("Stir constantly with hot stock.");
        await Page.Locator("button[type='submit']", new() { HasText = "Save Recipe" })
            .First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes/[a-f0-9-]+$"));

        // Go to Cookbook list
        await Page.Locator("nav.desktop-nav a", new() { HasText = "Cookbook" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/recipes$"));

        // Default status is Published, so Published recipe is visible and Draft is hidden
        await Expect(Page.Locator(".recipe-card", new() { HasText = pubTitle })).ToBeVisibleAsync();
        await Expect(Page.Locator(".recipe-card", new() { HasText = draftTitle }))
            .Not.ToBeVisibleAsync();

        // Filter by Draft / Review
        await Page.Locator("mat-select", new() { HasText = "Published" }).ClickAsync();
        await Page.Locator("mat-option", new() { HasText = "Draft / Review" }).ClickAsync();
        await Expect(Page.Locator(".recipe-card", new() { HasText = draftTitle }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator(".recipe-card", new() { HasText = pubTitle }))
            .Not.ToBeVisibleAsync();

        // Switch filter to All Recipes
        await Page.Locator("mat-select", new() { HasText = "Draft / Review" }).ClickAsync();
        await Page.Locator("mat-option", new() { HasText = "All Recipes" }).ClickAsync();
        await Expect(Page.Locator(".recipe-card", new() { HasText = draftTitle }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator(".recipe-card", new() { HasText = pubTitle })).ToBeVisibleAsync();

        // Search by keyword
        var searchInput = Page.Locator(".search-field input");
        await searchInput.FillAsync(pubTitle);
        await Expect(Page.Locator(".recipe-card", new() { HasText = pubTitle })).ToBeVisibleAsync();
        await Expect(Page.Locator(".recipe-card", new() { HasText = draftTitle }))
            .Not.ToBeVisibleAsync();
    }
}
