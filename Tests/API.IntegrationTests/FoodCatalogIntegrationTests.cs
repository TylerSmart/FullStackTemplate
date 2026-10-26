using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.Core.Authentication;
using App.Core.FoodCatalog;
using App.Core.Recipes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace App.API.IntegrationTests;

[TestClass]
public sealed class FoodCatalogIntegrationTests
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

    private static async Task<AuthResponse> RegisterAndAuthenticateAsync(
        HttpClient client,
        string email,
        string username
    )
    {
        var register = new RegisterRequest(email, username, "Password123!");
        var response = await client.PostAsJsonAsync("/api/auth/register", register);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            auth.Token
        );
        return auth;
    }

    [TestMethod]
    public async Task GetStoreAisles_ReturnsSeededDefaultAisles()
    {
        await RegisterAndAuthenticateAsync(_client, "aisle_user@example.com", "aisleuser");

        var response = await _client.GetAsync("/api/store-aisles");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var aisles = await response.Content.ReadFromJsonAsync<List<StoreAisleDto>>();
        Assert.IsNotNull(aisles);
        Assert.IsTrue(aisles.Count >= 13);
        Assert.IsTrue(aisles.Any(a => a.Name == "Produce" && a.IsSystemSeeded));
        Assert.IsTrue(aisles.Any(a => a.Name == "Spices & Baking" && a.IsSystemSeeded));
    }

    [TestMethod]
    public async Task GetFoodItems_ReturnsSeededPantryStaplesAndSupportsAliasSearch()
    {
        await RegisterAndAuthenticateAsync(_client, "staples_user@example.com", "staplesuser");

        var allItemsResponse = await _client.GetAsync("/api/food-items");
        Assert.AreEqual(HttpStatusCode.OK, allItemsResponse.StatusCode);

        var allItems = await allItemsResponse.Content.ReadFromJsonAsync<List<FoodItemDto>>();
        Assert.IsNotNull(allItems);
        Assert.IsTrue(allItems.Count >= 18);
        Assert.IsTrue(allItems.Any(f => f.Name == "Kosher Salt" && f.IsPantryStaple));

        // Search by alias "scallion" -> should find "Green Onion"
        var aliasSearchResponse = await _client.GetAsync("/api/food-items?search=scallion");
        Assert.AreEqual(HttpStatusCode.OK, aliasSearchResponse.StatusCode);

        var matchedItems = await aliasSearchResponse.Content.ReadFromJsonAsync<List<FoodItemDto>>();
        Assert.IsNotNull(matchedItems);
        Assert.IsTrue(matchedItems.Any(f => f.Name == "Green Onion"));
    }

    [TestMethod]
    public async Task CreateUpdateAndToggleFoodItem_Succeeds()
    {
        await RegisterAndAuthenticateAsync(_client, "catalog_user@example.com", "cataloguser");

        var aisles = await (await _client.GetAsync("/api/store-aisles")).Content.ReadFromJsonAsync<
            List<StoreAisleDto>
        >();
        Assert.IsNotNull(aisles);
        var produceAisle = aisles.First(a => a.Name == "Produce");

        // Create
        var createReq = new CreateFoodItemRequest(
            Name: "Avocado",
            DefaultStoreAisleId: produceAisle.Id,
            IsPantryStaple: false,
            Aliases: ["Hass Avocado", "Alligator Pear"]
        );
        var createRes = await _client.PostAsJsonAsync("/api/food-items", createReq);
        Assert.AreEqual(HttpStatusCode.Created, createRes.StatusCode);

        var created = await createRes.Content.ReadFromJsonAsync<FoodItemDto>();
        Assert.IsNotNull(created);
        Assert.AreEqual("Avocado", created.Name);
        Assert.AreEqual(produceAisle.Id, created.DefaultStoreAisleId);
        Assert.IsFalse(created.IsPantryStaple);
        Assert.AreEqual(2, created.Aliases.Count);

        // Update & toggle pantry staple
        var updateReq = new UpdateFoodItemRequest(
            Name: "Avocado",
            DefaultStoreAisleId: produceAisle.Id,
            IsPantryStaple: true,
            Aliases: ["Hass Avocado"]
        );
        var updateRes = await _client.PutAsJsonAsync($"/api/food-items/{created.Id}", updateReq);
        Assert.AreEqual(HttpStatusCode.OK, updateRes.StatusCode);

        var updated = await updateRes.Content.ReadFromJsonAsync<FoodItemDto>();
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated.IsPantryStaple);
        Assert.AreEqual(1, updated.Aliases.Count);
    }

    [TestMethod]
    public async Task CreateFoodItem_WithDuplicateNameOrAlias_ReturnsConflict()
    {
        await RegisterAndAuthenticateAsync(_client, "conflict_user@example.com", "conflictuser");

        // Duplicate of existing seeded item
        var dupReq = new CreateFoodItemRequest("Kosher Salt");
        var dupRes = await _client.PostAsJsonAsync("/api/food-items", dupReq);
        Assert.AreEqual(HttpStatusCode.Conflict, dupRes.StatusCode);

        // Collision with existing alias "scallion"
        var aliasCollisionReq = new CreateFoodItemRequest("Scallion");
        var aliasCollisionRes = await _client.PostAsJsonAsync("/api/food-items", aliasCollisionReq);
        Assert.AreEqual(HttpStatusCode.Conflict, aliasCollisionRes.StatusCode);
    }

    [TestMethod]
    public async Task DeleteFoodItem_ReferencedByRecipe_ReturnsConflict()
    {
        await RegisterAndAuthenticateAsync(_client, "del_user@example.com", "deluser");

        var createFood = await (
            await _client.PostAsJsonAsync(
                "/api/food-items",
                new CreateFoodItemRequest("Special Honey")
            )
        ).Content.ReadFromJsonAsync<FoodItemDto>();
        Assert.IsNotNull(createFood);

        // Create recipe referencing this food item
        var recipeReq = new UpsertRecipeRequest(
            Title: "Honey Glazed Carrots",
            Description: "Delicious carrots",
            Ingredients:
            [
                new RecipeIngredientInput(
                    SectionName: null,
                    Quantity: 2m,
                    MaxQuantity: null,
                    Unit: MeasurementUnit.Tbsp,
                    CustomUnitName: null,
                    FoodItemId: createFood.Id,
                    LinkedRecipeId: null,
                    DisplayName: "Special Honey",
                    PreparationNote: null
                ),
            ],
            Steps:
            [
                new RecipeStepInput(
                    SectionName: null,
                    Instruction: "Drizzle honey over carrots.",
                    IngredientIndices: [0]
                ),
            ]
        );
        var recipeRes = await _client.PostAsJsonAsync("/api/recipes", recipeReq);
        Assert.AreEqual(HttpStatusCode.Created, recipeRes.StatusCode);

        // Try deleting food item -> should return 409 Conflict
        var deleteRes = await _client.DeleteAsync($"/api/food-items/{createFood.Id}");
        Assert.AreEqual(HttpStatusCode.Conflict, deleteRes.StatusCode);
    }

    [TestMethod]
    public async Task GlobalHouseholdTenancy_ItemsVisibleAcrossUsers()
    {
        await RegisterAndAuthenticateAsync(_client, "user_a@example.com", "usera");

        var createFood = await (
            await _client.PostAsJsonAsync(
                "/api/food-items",
                new CreateFoodItemRequest("Smoked Paprika")
            )
        ).Content.ReadFromJsonAsync<FoodItemDto>();
        Assert.IsNotNull(createFood);

        // Other household user registers
        using var clientB = _factory.CreateClient();
        await RegisterAndAuthenticateAsync(clientB, "user_b@example.com", "userb");

        var getRes = await clientB.GetAsync($"/api/food-items/{createFood.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getRes.StatusCode);

        var fetched = await getRes.Content.ReadFromJsonAsync<FoodItemDto>();
        Assert.IsNotNull(fetched);
        Assert.AreEqual("Smoked Paprika", fetched.Name);
    }
}
