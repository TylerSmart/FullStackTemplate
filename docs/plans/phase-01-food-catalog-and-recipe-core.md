# Phase 01: Template Cleanup, Canonical Food Catalog & Core Structured Cookbook

> **Branch:** `feature/phase-01-food-catalog-and-recipe-core`
> **Status:** [x] Completed
> **PRD Reference:** `docs/prd.md` — Sections 2, 3, 4.1.1–4.1.5, 5, and 6 (Milestone 1, Slice 1)
> **Target Execution:** Run `/execute-phase` to execute this plan step-by-step.

---

## 1. Scope & Objectives

This phase establishes the foundational LocalRoast domain, removes the template `Todo` vertical slice, builds the responsive LocalRoast navigation shell, and delivers the first two core household slices end-to-end:

1. **Template `Todo` Cleanup & Shared Confirm Dialog Extraction:**
   - Remove all `Todo` classes, endpoints, Angular models/services/components, and tests across `Core/`, `Data/`, `API/`, `Client/`, and `Tests/`.
   - Promote `ConfirmDialogComponent` to `Client/src/app/shared/components/confirm-dialog/` for reuse across Cookbook and Food Catalog views.
   - Keep `auth.guard.ts` and `login.component.ts` untouched (preserve `/dashboard` route as a redirect to `/recipes` in `app.routes.ts` for zero-conflict upstream template merges).
2. **Canonical `FoodItem` Catalog & Store Aisles (`StoreAisle`, `FoodItem`, `FoodItemAlias`):**
   - Global household catalog (all authenticated users collaborate on one shared catalog; `IAuditable` records `CreatedBy`/`ModifiedBy`).
   - Startup idempotent seeder (`HouseholdCatalogSeeder`) populating 13 default grocery `StoreAisle`s, common `IsPantryStaple = true` staples + everyday produce/aromats, and common culinary `FoodItemAlias`es (e.g., `"scallion"` / `"green onion"`, `"cilantro"` / `"fresh coriander"`).
   - Full CRUD + search/filtering API (`/api/store-aisles`, `/api/food-items`) and responsive **Food Catalog** Angular feature (`/catalog`) with 1-tap Pantry Staple toggle, aisle filter, and alias management.
3. **Structured `Recipe` Core CRUD, Sub-Recipe DAG & Culinary Scaling Math:**
   - Structured `Recipe` metadata + provenance (`Title`, `Slug`, `Description`, `Status`, `BaseServings`, `YieldDisplay`, `PrepTimeMinutes`, `CookTimeMinutes`, `TotalTimeMinutes`, `Difficulty`, `SourceUrl`, `SourceName`, `SourceAuthor`, `SourceBookTitle`, `SourceBookPage`, plus media/cook-stat columns ready for later phases).
   - Ordered `RecipeIngredient` rows supporting **Section Headers**, **Quantity Ranges** (`Quantity`–`MaxQuantity`), 21 **Culinary Units** (`MeasurementUnit`), canonical `FoodItemId` binding, and **Sub-Recipe Linking** (`LinkedRecipeId`) protected by strict **Directed Acyclic Graph (DAG)** cycle detection.
   - Ordered `RecipeStep` rows supporting **Step Sections**, **Step-to-Ingredient Callouts** (`RecipeStepIngredient`), and **Structured Step Timers** (`RecipeStepTimer`).
   - Deterministic **Culinary Math & Unit Conversion** engine (`Original` | `UsCustomary` | `Metric` + natural kitchen fraction formatting `1/8`, `1/4`, `1/3`, `1/2`, `2/3`, `3/4`, `1 1/2`) in both `App.Core` and Angular.
   - Responsive **App Shell** (desktop left navigation rail + mobile `360px` bottom navigation bar), **Cookbook List** (`/recipes`), **Recipe Detail** (`/recipes/:id` with live scaling, unit switcher, inline step ingredient chips, timer badges, and expandable sub-recipes), and **Structured Recipe Editor** (`/recipes/new`, `/recipes/:id/edit` with `FoodItem` autocomplete + inline FoodItem creation).

### Explicitly Out of Scope (Deferred to Phase 02+)
- Persistent `/data/uploads` WebP image upload & EXIF processing (Phase 02).
- `Collection` (Cookbooks M:N) & typed `Tag` taxonomy, `"Use Up Ingredients"` Fridge Matcher, Print stylesheet, and public `/share/<token>` links (Phase 02–03).
- Local LLM / `Ollama` ingestion queue, Vision/PDF parsing, Personal Access Tokens, and `/api/mcp` server (Milestone 2).
- Meal Planning Calendar, Smart Shopping Lists, SignalR sync, Cook Mode wake-lock/live timers, and Cook Logs (Milestones 3–4).
- Calorie / macronutrient fields (deferred per `docs/prd.md` Section 2).

---

## 2. File Inventory (Delete, Modify, Create)

### 2.1 Files to Delete (`Todo` Slice Removal)
- `Core/Todos/ITodoService.cs`
- `Core/Todos/TodoModels.cs`
- `Data/Entities/TodoItem.cs`
- `Data/Configurations/TodoItemConfiguration.cs`
- `Data/Services/TodoService.cs`
- `API/Controllers/TodosController.cs`
- `Tests/API.IntegrationTests/TodoIntegrationTests.cs`
- `Client/src/app/core/models/todo.models.ts`
- `Client/src/app/core/services/todo/todo.service.ts`
- `Client/src/app/core/services/todo/todo.service.spec.ts`
- `Client/src/app/features/dashboard/` (delete entire directory after extracting `ConfirmDialogComponent` to `Client/src/app/shared/components/confirm-dialog/`)

### 2.2 Existing Files to Modify
- `Data/Entities/User.cs` — Remove `public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();`.
- `Data/AppDbContext.cs` — Remove `DbSet<TodoItem> TodoItems`; add `DbSet`s for the 8 new entities.
- `Data/DataServiceCollectionExtensions.cs` — Remove `ITodoService`/`TodoService`; register `IFoodCatalogService`/`FoodCatalogService` and `IRecipeService`/`RecipeService`.
- `API/Program.cs` — Invoke `await HouseholdCatalogSeeder.SeedAsync(db);` inside the startup `IServiceScope` right after `if (db.Database.IsRelational()) { db.Database.Migrate(); }` so both SQL Server and EF Core InMemory (`AppWebApplicationFactory`) seed default aisles and staples idempotently.
- `Tests/API.IntegrationTests/OpenApiIntegrationTests.cs` — Replace `StringAssert.Contains(content, "/todos");` with assertions for `"/store-aisles"`, `"/food-items"`, and `"/recipes"`.
- `Client/src/app/app.routes.ts` — Configure `AppShellComponent` authenticated layout with lazy routes (`recipes`, `recipes/new`, `recipes/:id`, `recipes/:id/edit`, `catalog`) and redirect `''` and `'dashboard'` to `'recipes'`.
- `Tests/Client.E2ETests/App.Client.E2ETests.csproj` — Reference `..\..\Core\App.Core.csproj` if helpful for typed API setup or keep standalone with `PlaywrightServerFixture.cs`.
- `Tests/Client.E2ETests/ClientSmokeTests.cs` — Replace placeholder with real Playwright end-to-end verification of login/registration, Food Catalog management, Recipe creation, and Recipe Detail live scaling/unit switching (including `360px` mobile viewport check).

### 2.3 New Backend Files to Create
- `Core/FoodCatalog/FoodCatalogModels.cs`
- `Core/FoodCatalog/IFoodCatalogService.cs`
- `Core/Recipes/RecipeEnums.cs`
- `Core/Recipes/RecipeModels.cs`
- `Core/Recipes/CulinaryMath.cs`
- `Core/Recipes/IRecipeService.cs`
- `Data/Entities/StoreAisle.cs`
- `Data/Entities/FoodItem.cs`
- `Data/Entities/FoodItemAlias.cs`
- `Data/Entities/Recipe.cs`
- `Data/Entities/RecipeIngredient.cs`
- `Data/Entities/RecipeStep.cs`
- `Data/Entities/RecipeStepIngredient.cs`
- `Data/Entities/RecipeStepTimer.cs`
- `Data/Configurations/StoreAisleConfiguration.cs`
- `Data/Configurations/FoodItemConfiguration.cs`
- `Data/Configurations/FoodItemAliasConfiguration.cs`
- `Data/Configurations/RecipeConfiguration.cs`
- `Data/Configurations/RecipeIngredientConfiguration.cs`
- `Data/Configurations/RecipeStepConfiguration.cs`
- `Data/Configurations/RecipeStepIngredientConfiguration.cs`
- `Data/Configurations/RecipeStepTimerConfiguration.cs`
- `Data/Seeding/HouseholdCatalogSeeder.cs`
- `Data/Services/FoodCatalogService.cs`
- `Data/Services/RecipeService.cs`
- `API/Controllers/StoreAislesController.cs`
- `API/Controllers/FoodItemsController.cs`
- `API/Controllers/RecipesController.cs`
- `Tests/Core.UnitTests/CulinaryMathTests.cs`
- `Tests/API.IntegrationTests/FoodCatalogIntegrationTests.cs`
- `Tests/API.IntegrationTests/RecipesIntegrationTests.cs`
- `Tests/Client.E2ETests/PlaywrightServerFixture.cs`

### 2.4 New Frontend Files to Create (`Client/src/app/`)
- `core/models/food-catalog.models.ts`
- `core/models/recipe.models.ts`
- `core/utils/culinary-math.ts`
- `core/utils/culinary-math.spec.ts`
- `core/services/food-catalog/food-catalog.service.ts`
- `core/services/food-catalog/food-catalog.service.spec.ts`
- `core/services/recipe/recipe.service.ts`
- `core/services/recipe/recipe.service.spec.ts`
- `shared/components/confirm-dialog/confirm-dialog.component.ts` (+ `.html`, `.scss`, `.spec.ts`)
- `features/shell/app-shell.component.ts` (+ `.html`, `.scss`, `.spec.ts`)
- `features/food-catalog/food-catalog.component.ts` (+ `.html`, `.scss`, `.spec.ts`)
- `features/food-catalog/food-item-dialog/food-item-dialog.component.ts` (+ `.html`, `.scss`, `.spec.ts`)
- `features/recipes/recipe-list/recipe-list.component.ts` (+ `.html`, `.scss`, `.spec.ts`)
- `features/recipes/recipe-detail/recipe-detail.component.ts` (+ `.html`, `.scss`, `.spec.ts`)
- `features/recipes/recipe-editor/recipe-editor.component.ts` (+ `.html`, `.scss`, `.spec.ts`)

---

## 3. Domain & Database Design (`Core/` & `Data/`)

### 3.1 Enums & Culinary Math (`Core/Recipes/`)

Decorate all enums in `Core/Recipes/RecipeEnums.cs` (namespace `App.Core.Recipes`) with `[JsonConverter(typeof(JsonStringEnumConverter))]` so `ASP.NET Core`, `Scalar OpenAPI`, `HttpClient` integration tests, and Angular string union types serialize identically without custom `JsonSerializerOptions`:

```csharp
using System.Text.Json.Serialization;

namespace App.Core.Recipes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecipeStatus
{
    DraftReview = 0,
    Published = 1,
    Archived = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecipeDifficulty
{
    Easy = 0,
    Medium = 1,
    Project = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeasurementUnit
{
    // Volume - US Customary
    Tsp = 0,
    Tbsp = 1,
    FlOz = 2,
    Cup = 3,
    Pt = 4,
    Qt = 5,
    Gal = 6,

    // Volume - Metric
    Ml = 7,
    L = 8,

    // Weight - US Customary
    Oz = 9,
    Lb = 10,

    // Weight - Metric
    G = 11,
    Kg = 12,

    // Culinary / Count / Custom (non-convertible across US/Metric)
    Pinch = 13,
    Dash = 14,
    Clove = 15,
    Can = 16,
    Bunch = 17,
    Slice = 18,
    Whole = 19,
    Custom = 20,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UnitSystem
{
    Original = 0,
    UsCustomary = 1,
    Metric = 2,
}
```

**`Core/Recipes/CulinaryMath.cs`** (`public static class CulinaryMath`):
- Pure deterministic helpers (zero EF/HTTP dependencies) mirrored in `Client/src/app/core/utils/culinary-math.ts`:
  - `ScaleAndConvert(decimal? quantity, decimal? maxQuantity, MeasurementUnit? unit, string? customUnitName, decimal scaleFactor, UnitSystem targetSystem)` returning `FormattedQuantityResult(decimal? Quantity, decimal? MaxQuantity, MeasurementUnit? Unit, string UnitLabel, string FormattedAmount)`:
    - Multiplies `quantity` and `maxQuantity` by `scaleFactor` (`targetServings / baseServings`, guarding `baseServings > 0` and `scaleFactor > 0`).
    - **Conversion constants:**
      - Volume base `ml`: `Tsp = 4.92892m`, `Tbsp = 14.7868m`, `FlOz = 29.5735m`, `Cup = 236.588m`, `Pt = 473.176m`, `Qt = 946.353m`, `Gal = 3785.41m`, `Ml = 1m`, `L = 1000m`.
      - Weight base `g`: `Oz = 28.3495m`, `Lb = 453.592m`, `G = 1m`, `Kg = 1000m`.
    - When `targetSystem == UnitSystem.Metric`:
      - US Volumes convert to `L` if `ml >= 1000m`, else `Ml`.
      - US Weights convert to `Kg` if `g >= 1000m`, else `G`.
      - Existing Metric units promote `Ml >= 1000m -> L` and `G >= 1000m -> Kg`.
    - When `targetSystem == UnitSystem.UsCustomary`:
      - Metric Volumes (`Ml`, `L`) convert via `ml`: `< 14.7868m -> Tsp`, `< 59.147m -> Tbsp`, `< 946.353m -> Cup`, `>= 946.353m -> Qt`.
      - Metric Weights (`G`, `Kg`) convert via `g`: `< 453.592m -> Oz`, `>= 453.592m -> Lb`.
    - Non-convertible count/culinary units (`Pinch`, `Dash`, `Clove`, `Can`, `Bunch`, `Slice`, `Whole`, `Custom`, `null`) scale numerically without changing unit.
  - `FormatNumber(decimal value, bool isMetric)`:
    - If `isMetric` (`Ml`, `L`, `G`, `Kg`): round values `>= 10m` (`Ml`, `G`) to nearest integer (`"250"`), and smaller values (`L`, `Kg`, `< 10m`) to at most 1–2 trimmed decimal places (`"1.5"`, `"2.25"`).
    - Otherwise (US Customary & Count): snap fractional part within `0.04m` tolerance to kitchen fractions (`1/8`, `1/4`, `1/3`, `3/8`, `1/2`, `5/8`, `2/3`, `3/4`, `7/8`, plus carry to next integer if `>= 0.96m`), rendering `"1/2"` or `"1 1/2"` (fallback to `0.##` when outside tolerance).
    - Ranges format as `"{FormatNumber(min)}–{FormatNumber(max)}"`.
  - `FormatDuration(int durationSeconds)` -> human label (`"45 sec"`, `"15 min"`, `"1 hr 30 min"`).

### 3.2 Entities (`Data/Entities/`)

All entities are `public sealed class <Name> : IAuditable` clean POCOs (no DataAnnotations on entity classes):

1. **`StoreAisle`**:
   - `Guid Id = Guid.NewGuid()`, `string Name = string.Empty`, `string NormalizedName = string.Empty`, `int SortOrder`, `bool IsSystemSeeded`
   - `IAuditable` properties (`CreatedAt`, `CreatedBy = "System"`, `ModifiedAt`, `ModifiedBy = "System"`)
   - `ICollection<FoodItem> FoodItems = new List<FoodItem>()`
2. **`FoodItem`**:
   - `Guid Id = Guid.NewGuid()`, `string Name = string.Empty`, `string NormalizedName = string.Empty`, `Guid? DefaultStoreAisleId`, `bool IsPantryStaple`
   - `IAuditable` properties
   - `StoreAisle? DefaultStoreAisle`, `ICollection<FoodItemAlias> Aliases = new List<FoodItemAlias>()`, `ICollection<RecipeIngredient> RecipeIngredients = new List<RecipeIngredient>()`
3. **`FoodItemAlias`**:
   - `Guid Id = Guid.NewGuid()`, `Guid FoodItemId`, `string Alias = string.Empty`, `string NormalizedAlias = string.Empty`
   - `IAuditable` properties
   - `FoodItem FoodItem = null!`
4. **`Recipe`**:
   - `Guid Id = Guid.NewGuid()`, `string Title = string.Empty`, `string Slug = string.Empty`, `string? Description`
   - `RecipeStatus Status = RecipeStatus.Published`
   - `decimal BaseServings = 4m`, `string? YieldDisplay`
   - `int? PrepTimeMinutes`, `int? CookTimeMinutes`, `int? TotalTimeMinutes`
   - `RecipeDifficulty? Difficulty`
   - `string? SourceUrl`, `string? SourceName`, `string? SourceAuthor`, `string? SourceBookTitle`, `int? SourceBookPage`
   - `string? CoverImagePath`, `string? ThumbnailImagePath`
   - `int TimesCooked`, `DateTimeOffset? LastCookedAt`, `decimal? AverageRating`
   - `IAuditable` properties
   - `ICollection<RecipeIngredient> Ingredients = new List<RecipeIngredient>()`
   - `ICollection<RecipeIngredient> ReferencedAsSubRecipeIn = new List<RecipeIngredient>()`
   - `ICollection<RecipeStep> Steps = new List<RecipeStep>()`
5. **`RecipeIngredient`**:
   - `Guid Id = Guid.NewGuid()`, `Guid RecipeId`, `int SortOrder`, `string? SectionName`
   - `decimal? Quantity`, `decimal? MaxQuantity`, `MeasurementUnit? Unit`, `string? CustomUnitName`
   - `Guid? FoodItemId`, `Guid? LinkedRecipeId`
   - `string DisplayName = string.Empty`, `string? PreparationNote`, `bool IsOptional`, `string? OriginalRawText`
   - `IAuditable` properties
   - `Recipe Recipe = null!`, `FoodItem? FoodItem`, `Recipe? LinkedRecipe`
   - `ICollection<RecipeStepIngredient> StepReferences = new List<RecipeStepIngredient>()`
6. **`RecipeStep`**:
   - `Guid Id = Guid.NewGuid()`, `Guid RecipeId`, `int StepNumber`, `string? SectionName`, `string Instruction = string.Empty`, `string? ImagePath`
   - `IAuditable` properties
   - `Recipe Recipe = null!`
   - `ICollection<RecipeStepIngredient> IngredientReferences = new List<RecipeStepIngredient>()`
   - `ICollection<RecipeStepTimer> Timers = new List<RecipeStepTimer>()`
7. **`RecipeStepIngredient`**:
   - `Guid Id = Guid.NewGuid()`, `Guid RecipeStepId`, `Guid RecipeIngredientId`
   - `IAuditable` properties
   - `RecipeStep RecipeStep = null!`, `RecipeIngredient RecipeIngredient = null!`
8. **`RecipeStepTimer`**:
   - `Guid Id = Guid.NewGuid()`, `Guid RecipeStepId`, `int SortOrder`, `int DurationSeconds`, `string Label = string.Empty`
   - `IAuditable` properties
   - `RecipeStep RecipeStep = null!`

### 3.3 EF Core Fluent Configurations (`Data/Configurations/`) & SQL Server Cascade Safety

> [!WARNING]
> **SQL Server Multiple Cascade Paths Prevention:** Every foreign key below is designed so `dotnet ef database update` succeeds cleanly on SQL Server 2022 without cycle or diamond-cascade errors. Follow the exact `DeleteBehavior` rules below!

- **`StoreAisleConfiguration` (`StoreAisles`):**
  - PK `Id`. `Name` (`MaxLength(100)`, required), `NormalizedName` (`MaxLength(100)`, required, **Unique Index**), `SortOrder` (index on `SortOrder`), `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).
- **`FoodItemConfiguration` (`FoodItems`):**
  - PK `Id`. `Name` (`MaxLength(150)`, required), `NormalizedName` (`MaxLength(150)`, required, **Unique Index**), `IsPantryStaple` (index).
  - FK `DefaultStoreAisleId` -> `StoreAisle.FoodItems` with **`DeleteBehavior.Restrict`** (`IsRequired(false)`).
  - `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).
- **`FoodItemAliasConfiguration` (`FoodItemAliases`):**
  - PK `Id`. `Alias` (`MaxLength(150)`, required), `NormalizedAlias` (`MaxLength(150)`, required, **Unique Index**).
  - FK `FoodItemId` -> `FoodItem.Aliases` with **`DeleteBehavior.Cascade`**.
  - `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).
- **`RecipeConfiguration` (`Recipes`):**
  - PK `Id`. `Title` (`MaxLength(200)`, required, index), `Slug` (`MaxLength(220)`, required, **Unique Index**), `Description` (`MaxLength(2000)`).
  - `Status` stored as string (`HasConversion<string>()`, `MaxLength(32)`, index).
  - `BaseServings` (`HasPrecision(8, 2)`), `YieldDisplay` (`MaxLength(100)`).
  - `Difficulty` (`HasConversion<string>()`, `MaxLength(32)`).
  - `SourceUrl` (`MaxLength(2048)`), `SourceName` (`MaxLength(200)`), `SourceAuthor` (`MaxLength(200)`), `SourceBookTitle` (`MaxLength(200)`), `CoverImagePath` (`MaxLength(500)`), `ThumbnailImagePath` (`MaxLength(500)`).
  - `AverageRating` (`HasPrecision(3, 2)`).
  - `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).
- **`RecipeIngredientConfiguration` (`RecipeIngredients`):**
  - PK `Id`. Composite index `(RecipeId, SortOrder)`.
  - `SectionName` (`MaxLength(150)`), `Quantity` (`HasPrecision(10, 4)`), `MaxQuantity` (`HasPrecision(10, 4)`).
  - `Unit` (`HasConversion<string>()`, `MaxLength(32)`), `CustomUnitName` (`MaxLength(50)`).
  - `DisplayName` (`MaxLength(200)`, required), `PreparationNote` (`MaxLength(200)`), `OriginalRawText` (`MaxLength(500)`).
  - FK `RecipeId` -> `Recipe.Ingredients` with **`DeleteBehavior.Cascade`**.
  - FK `FoodItemId` -> `FoodItem.RecipeIngredients` with **`DeleteBehavior.Restrict`** (`IsRequired(false)`).
  - FK `LinkedRecipeId` -> `Recipe.ReferencedAsSubRecipeIn` with **`DeleteBehavior.Restrict`** (`IsRequired(false)` — **MUST** be `Restrict` to prevent self-referencing cascade path conflict with `RecipeId`!).
  - `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).
- **`RecipeStepConfiguration` (`RecipeSteps`):**
  - PK `Id`. Composite Unique Index `(RecipeId, StepNumber)`.
  - `SectionName` (`MaxLength(150)`), `Instruction` (`MaxLength(4000)`, required), `ImagePath` (`MaxLength(500)`).
  - FK `RecipeId` -> `Recipe.Steps` with **`DeleteBehavior.Cascade`**.
  - `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).
- **`RecipeStepIngredientConfiguration` (`RecipeStepIngredients`):**
  - PK `Id`. Composite Unique Index `(RecipeStepId, RecipeIngredientId)`.
  - FK `RecipeStepId` -> `RecipeStep.IngredientReferences` with **`DeleteBehavior.Cascade`**.
  - FK `RecipeIngredientId` -> `RecipeIngredient.StepReferences` with **`DeleteBehavior.Restrict`** (**CRITICAL:** `Restrict` avoids diamond cascade `Recipes -> RecipeSteps -> RecipeStepIngredients` vs `Recipes -> RecipeIngredients -> RecipeStepIngredients` in SQL Server! In `RecipeService.UpdateRecipeAsync` and `RecipeService.DeleteRecipeAsync`, explicitly remove `RecipeStepIngredients` for the recipe before removing `RecipeIngredients` or `Recipe`!).
  - `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).
- **`RecipeStepTimerConfiguration` (`RecipeStepTimers`):**
  - PK `Id`. Composite index `(RecipeStepId, SortOrder)`.
  - `Label` (`MaxLength(150)`, required).
  - FK `RecipeStepId` -> `RecipeStep.Timers` with **`DeleteBehavior.Cascade`**.
  - `CreatedBy`/`ModifiedBy` (`MaxLength(256)`, required).

**Migration Name:** `ReplaceTodosWithFoodCatalogAndRecipes`

### 3.4 Seed Data (`Data/Seeding/HouseholdCatalogSeeder.cs`)

`public static class HouseholdCatalogSeeder` with `public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)`:
1. If `!await db.StoreAisles.AnyAsync(cancellationToken)`, insert the 13 default aisles (`IsSystemSeeded = true`):
   `10 Produce`, `20 Bakery`, `30 Deli`, `40 Meat & Seafood`, `50 Dairy & Eggs`, `60 Canned & Jarred`, `70 Dry Goods & Pasta`, `80 International`, `90 Spices & Baking`, `100 Frozen`, `110 Beverages`, `120 Household`, `130 Other`.
2. If `!await db.FoodItems.AnyAsync(cancellationToken)`, insert starter canonical `FoodItem`s mapped to those aisles:
   - **Pantry Staples (`IsPantryStaple = true`):**
     - `Water` (`Beverages`), `Kosher Salt` (`Spices & Baking`, aliases: `["Salt", "Fine Sea Salt"]`), `Black Pepper` (`Spices & Baking`, aliases: `["Freshly Ground Black Pepper", "Peppercorns"]`), `Extra-Virgin Olive Oil` (`Spices & Baking`, aliases: `["Olive Oil", "EVOO"]`), `Neutral Cooking Oil` (`Spices & Baking`, aliases: `["Vegetable Oil", "Canola Oil"]`), `All-Purpose Flour` (`Spices & Baking`, aliases: `["AP Flour", "Plain Flour"]`), `Granulated Sugar` (`Spices & Baking`, aliases: `["White Sugar"]`), `Baking Powder` (`Spices & Baking`), `Baking Soda` (`Spices & Baking`), `Unsalted Butter` (`Dairy & Eggs`, aliases: `["Butter"]`).
   - **Common Household Ingredients (`IsPantryStaple = false`):**
     - `Garlic` (`Produce`, aliases: `["Garlic Cloves"]`), `Yellow Onion` (`Produce`, aliases: `["Onion", "Sweet Onion"]`), `Green Onion` (`Produce`, aliases: `["Scallion", "Spring Onion"]`), `Cilantro` (`Produce`, aliases: `["Fresh Coriander"]`), `Lemon` (`Produce`), `Lime` (`Produce`), `Large Eggs` (`Dairy & Eggs`, aliases: `["Egg", "Eggs"]`), `Chicken Thighs` (`Meat & Seafood`, aliases: `["Boneless Skinless Chicken Thighs"]`).

### 3.5 DTOs, Requests & Service Contracts (`Core/`)

#### `Core/FoodCatalog/FoodCatalogModels.cs` & `IFoodCatalogService.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace App.Core.FoodCatalog;

public record StoreAisleDto(Guid Id, string Name, int SortOrder, bool IsSystemSeeded);

public record FoodItemDto(
    Guid Id,
    string Name,
    Guid? DefaultStoreAisleId,
    string? DefaultStoreAisleName,
    int? DefaultStoreAisleSortOrder,
    bool IsPantryStaple,
    IReadOnlyList<string> Aliases,
    int ReferencedRecipeCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt
);

public record FoodItemQueryParameters(
    string? Search = null,
    Guid? StoreAisleId = null,
    bool? IsPantryStaple = null
);

public record CreateFoodItemRequest(
    [property: Required, MaxLength(150)] string Name,
    Guid? DefaultStoreAisleId = null,
    bool IsPantryStaple = false,
    IReadOnlyList<string>? Aliases = null
);

public record UpdateFoodItemRequest(
    [property: Required, MaxLength(150)] string Name,
    Guid? DefaultStoreAisleId = null,
    bool IsPantryStaple = false,
    IReadOnlyList<string>? Aliases = null
);
```

`IFoodCatalogService`:
- `Task<IReadOnlyList<StoreAisleDto>> GetStoreAislesAsync(CancellationToken cancellationToken = default);`
- `Task<IReadOnlyList<FoodItemDto>> GetFoodItemsAsync(FoodItemQueryParameters query, CancellationToken cancellationToken = default);`
- `Task<FoodItemDto?> GetFoodItemByIdAsync(Guid id, CancellationToken cancellationToken = default);`
- `Task<FoodItemDto> CreateFoodItemAsync(CreateFoodItemRequest request, CancellationToken cancellationToken = default);`
- `Task<FoodItemDto?> UpdateFoodItemAsync(Guid id, UpdateFoodItemRequest request, CancellationToken cancellationToken = default);`
- `Task<bool> DeleteFoodItemAsync(Guid id, CancellationToken cancellationToken = default);`

#### `Core/Recipes/RecipeModels.cs` & `IRecipeService.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace App.Core.Recipes;

public record RecipeSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    RecipeStatus Status,
    decimal BaseServings,
    string? YieldDisplay,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    int? TotalTimeMinutes,
    RecipeDifficulty? Difficulty,
    string? SourceName,
    string? ThumbnailImagePath,
    int IngredientCount,
    int StepCount,
    int TimesCooked,
    decimal? AverageRating,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt
);

public record SubRecipePreviewDto(
    Guid Id,
    string Title,
    string Slug,
    decimal BaseServings,
    string? YieldDisplay,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps
);

public record RecipeIngredientDto(
    Guid Id,
    int SortOrder,
    string? SectionName,
    decimal? Quantity,
    decimal? MaxQuantity,
    MeasurementUnit? Unit,
    string? CustomUnitName,
    Guid? FoodItemId,
    string? FoodItemName,
    bool IsPantryStaple,
    Guid? LinkedRecipeId,
    string? LinkedRecipeTitle,
    string? LinkedRecipeSlug,
    string DisplayName,
    string? PreparationNote,
    bool IsOptional,
    string? OriginalRawText
);

public record RecipeStepTimerDto(Guid Id, int SortOrder, int DurationSeconds, string Label);

public record RecipeStepDto(
    Guid Id,
    int StepNumber,
    string? SectionName,
    string Instruction,
    string? ImagePath,
    IReadOnlyList<Guid> IngredientIds,
    IReadOnlyList<RecipeStepTimerDto> Timers
);

public record RecipeDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    RecipeStatus Status,
    decimal BaseServings,
    string? YieldDisplay,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    int? TotalTimeMinutes,
    RecipeDifficulty? Difficulty,
    string? SourceUrl,
    string? SourceName,
    string? SourceAuthor,
    string? SourceBookTitle,
    int? SourceBookPage,
    string? CoverImagePath,
    string? ThumbnailImagePath,
    int TimesCooked,
    DateTimeOffset? LastCookedAt,
    decimal? AverageRating,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps,
    IReadOnlyList<SubRecipePreviewDto> LinkedSubRecipes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset ModifiedAt,
    string ModifiedBy
);

public record RecipeQueryParameters(
    string? Search = null,
    RecipeStatus? Status = null,
    RecipeDifficulty? Difficulty = null,
    int? MaxTotalTimeMinutes = null,
    Guid? FoodItemId = null
);

public record RecipeIngredientInput(
    [property: MaxLength(150)] string? SectionName,
    decimal? Quantity,
    decimal? MaxQuantity,
    MeasurementUnit? Unit,
    [property: MaxLength(50)] string? CustomUnitName,
    Guid? FoodItemId,
    Guid? LinkedRecipeId,
    [property: Required, MaxLength(200)] string DisplayName,
    [property: MaxLength(200)] string? PreparationNote,
    bool IsOptional = false,
    [property: MaxLength(500)] string? OriginalRawText = null
);

public record RecipeStepTimerInput(
    [property: Range(1, 864000)] int DurationSeconds,
    [property: Required, MaxLength(150)] string Label
);

public record RecipeStepInput(
    [property: MaxLength(150)] string? SectionName,
    [property: Required, MaxLength(4000)] string Instruction,
    IReadOnlyList<int>? IngredientIndices = null,
    IReadOnlyList<RecipeStepTimerInput>? Timers = null
);

public record UpsertRecipeRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(2000)] string? Description,
    RecipeStatus Status = RecipeStatus.Published,
    [property: Range(typeof(decimal), "0.25", "999")] decimal BaseServings = 4m,
    [property: MaxLength(100)] string? YieldDisplay = null,
    [property: Range(0, 10080)] int? PrepTimeMinutes = null,
    [property: Range(0, 10080)] int? CookTimeMinutes = null,
    [property: Range(0, 10080)] int? TotalTimeMinutes = null,
    RecipeDifficulty? Difficulty = null,
    [property: MaxLength(2048)] string? SourceUrl = null,
    [property: MaxLength(200)] string? SourceName = null,
    [property: MaxLength(200)] string? SourceAuthor = null,
    [property: MaxLength(200)] string? SourceBookTitle = null,
    [property: Range(1, 10000)] int? SourceBookPage = null,
    IReadOnlyList<RecipeIngredientInput>? Ingredients = null,
    IReadOnlyList<RecipeStepInput>? Steps = null
);
```

`IRecipeService`:
- `Task<IReadOnlyList<RecipeSummaryDto>> GetRecipesAsync(RecipeQueryParameters query, CancellationToken cancellationToken = default);`
- `Task<RecipeDetailDto?> GetRecipeByIdAsync(Guid id, CancellationToken cancellationToken = default);`
- `Task<RecipeDetailDto> CreateRecipeAsync(UpsertRecipeRequest request, CancellationToken cancellationToken = default);`
- `Task<RecipeDetailDto?> UpdateRecipeAsync(Guid id, UpsertRecipeRequest request, CancellationToken cancellationToken = default);`
- `Task<bool> DeleteRecipeAsync(Guid id, CancellationToken cancellationToken = default);`

### 3.6 Service Business Logic & Invariants (`Data/Services/`)

#### `FoodCatalogService`
- **Normalization Helper:** Trim and collapse internal whitespace; `NormalizedName = NormalizeKey(name)` (`Trim().ToLowerInvariant()`). Throw `DomainValidationException` if blank after trimming.
- **Uniqueness & Alias Collision Checks (`409 Conflict` via `ConflictException`):**
  - A `FoodItem`'s `NormalizedName` must not match any other `FoodItem.NormalizedName` **or** any other item's `FoodItemAlias.NormalizedAlias`.
  - Each cleaned alias on a `FoodItem` (ignoring aliases identical to its own `NormalizedName` and deduplicating case-insensitively) must not collide with any `FoodItem.NormalizedName` or another item's `FoodItemAlias.NormalizedAlias`.
- **Store Aisle Validation:** If `DefaultStoreAisleId` is provided, verify it exists in `StoreAisles` (else throw `DomainValidationException("Selected store aisle does not exist.")`).
- **Search & Filtering (`GetFoodItemsAsync`):**
  - Uses `.AsNoTracking()`.
  - When `Search` is non-empty, match `f.NormalizedName.Contains(norm)` OR `f.Aliases.Any(a => a.NormalizedAlias.Contains(norm))`.
  - Filter by `StoreAisleId` and `IsPantryStaple` when provided.
  - Sort by `Name` ascending (`DefaultStoreAisle.SortOrder` available on DTO for grouped UI views).
- **Safe Delete (`DeleteFoodItemAsync`):**
  - Return `false` if not found.
  - If `await dbContext.RecipeIngredients.AnyAsync(ri => ri.FoodItemId == id, cancellationToken)`, throw `ConflictException("Cannot delete a food item that is used in one or more recipes.")`.

#### `RecipeService`
- **Validation (`DomainValidationException` -> `400 Bad Request`):**
  - `Title` trimmed non-empty; `BaseServings > 0`.
  - If `TotalTimeMinutes` is `null` and either `PrepTimeMinutes` or `CookTimeMinutes` is provided, compute `TotalTimeMinutes = (PrepTimeMinutes ?? 0) + (CookTimeMinutes ?? 0)`.
  - For each `RecipeIngredientInput`:
    - `DisplayName` trimmed non-empty.
    - `Quantity` (if set) `> 0`; if `MaxQuantity` is set, `Quantity` must also be set and `MaxQuantity >= Quantity`.
    - If `Unit == MeasurementUnit.Custom`, `CustomUnitName` must be non-empty; otherwise clear `CustomUnitName = null`.
    - `FoodItemId` and `LinkedRecipeId` are **mutually exclusive** (throw `DomainValidationException` if both are non-null).
    - Verify all referenced `FoodItemId`s exist in `FoodItems` and all `LinkedRecipeId`s exist in `Recipes`.
  - For each `RecipeStepInput`:
    - `Instruction` trimmed non-empty.
    - Every index in `IngredientIndices` (deduplicated) must satisfy `0 <= index < ingredients.Count` (throw `DomainValidationException` if out of bounds).
    - Every timer in `Timers` must have `DurationSeconds > 0` and non-empty `Label`.
- **Slug Generation:**
  - Generate kebab-case ASCII slug from `Title` (fallback `"recipe"` if non-alphanumeric). If taken by another recipe, append `-2`, `-3`, etc. On `UpdateRecipeAsync`, keep existing slug if `Title` (normalized) did not change; otherwise regenerate a unique slug.
- **Sub-Recipe DAG Cycle Detection:**
  - On `UpdateRecipeAsync(id, ...)`, if any ingredient has `LinkedRecipeId == id`, throw `DomainValidationException("A recipe cannot reference itself as a sub-recipe.")`.
  - Load all existing `(RecipeId, LinkedRecipeId)` pairs from `dbContext.RecipeIngredients.AsNoTracking().Where(ri => ri.LinkedRecipeId != null && ri.RecipeId != id)`. Traverse reachable nodes starting from each proposed `LinkedRecipeId`; if `id` is encountered (e.g., `A -> B -> A` or `A -> B -> C -> A`), throw `ConflictException("Sub-recipe link would create a circular recipe dependency.")`.
- **SQL-Server-Safe Child Graph Replacement & Deletion:**
  - In `UpdateRecipeAsync`: explicitly load and remove existing `RecipeStepIngredients` (`dbContext.RecipeStepIngredients.RemoveRange(...)`), `RecipeStepTimers`, `RecipeSteps`, and `RecipeIngredients` for the recipe, call `await dbContext.SaveChangesAsync(cancellationToken)` (or delete in order), then insert the newly ordered `RecipeIngredient` (`0..N-1`), `RecipeStep` (`1..M`), `RecipeStepIngredient` (mapped via `createdIngredients[idx].Id`), and `RecipeStepTimer` entities.
  - In `DeleteRecipeAsync(id)`:
    - Return `false` if recipe does not exist.
    - If `await dbContext.RecipeIngredients.AnyAsync(ri => ri.LinkedRecipeId == id && ri.RecipeId != id, cancellationToken)`, throw `ConflictException("Cannot delete a recipe while another recipe links to it as a sub-recipe.")`.
    - Explicitly remove `RecipeStepIngredients` belonging to `id`'s steps (`dbContext.RecipeStepIngredients.Where(rsi => rsi.RecipeStep.RecipeId == id)`), then remove `Recipe` and `SaveChangesAsync`.
- **Read Queries (`GetRecipesAsync`, `GetRecipeByIdAsync`):**
  - Always `.AsNoTracking()`.
  - `GetRecipesAsync`: default when `query.Status` is null returns non-archived recipes (`Status != RecipeStatus.Archived`); when `query.Status` is specified, filters exact `Status`. `Search` matches `Title`, `Description`, `SourceName`, `SourceAuthor`, `SourceBookTitle`, `Ingredients.DisplayName`, `Ingredients.FoodItem.Name`, or `Ingredients.FoodItem.Aliases.Alias`. Filters `Difficulty`, `MaxTotalTimeMinutes` (`TotalTimeMinutes != null && TotalTimeMinutes <= max`), and `FoodItemId`. Ordered by `ModifiedAt` descending.
  - `GetRecipeByIdAsync`: loads ordered `Ingredients` (`SortOrder`), ordered `Steps` (`StepNumber`) with `IngredientIds` and `Timers` (`SortOrder`), and populates `LinkedSubRecipes` (`SubRecipePreviewDto`) for all distinct `LinkedRecipeId`s referenced by the recipe's ingredients.

---

## 4. API Endpoints (`API/Controllers/`)

All controllers use `[ApiController]`, `[Authorize]`, and relative routes (mounted under `/api` by `Program.cs`). Controllers stay strictly thin HTTP adapters translating `DomainValidationException -> 400 ProblemDetails`, `ConflictException -> 409 ProblemDetails`, `NotFoundException / null / false -> 404 ProblemDetails`.

| Controller | Verb | Route (`/api/...`) | Request Body / Query | Success | Error Cases |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `StoreAislesController` | `GET` | `/store-aisles` | — | `200 OK` (`IReadOnlyList<StoreAisleDto>`) | `401` |
| `FoodItemsController` | `GET` | `/food-items` | `[FromQuery] string? search, Guid? storeAisleId, bool? isPantryStaple` | `200 OK` (`IReadOnlyList<FoodItemDto>`) | `401` |
| `FoodItemsController` | `GET` | `/food-items/{id:guid}` | — | `200 OK` (`FoodItemDto`) | `401`, `404` |
| `FoodItemsController` | `POST` | `/food-items` | `[FromBody] CreateFoodItemRequest` | `201 Created` (`FoodItemDto`) | `400`, `401`, `409` |
| `FoodItemsController` | `PUT` | `/food-items/{id:guid}` | `[FromBody] UpdateFoodItemRequest` | `200 OK` (`FoodItemDto`) | `400`, `401`, `404`, `409` |
| `FoodItemsController` | `DELETE` | `/food-items/{id:guid}` | — | `204 No Content` | `401`, `404`, `409` (referenced by recipe) |
| `RecipesController` | `GET` | `/recipes` | `[FromQuery] string? search, RecipeStatus? status, RecipeDifficulty? difficulty, int? maxTotalTimeMinutes, Guid? foodItemId` | `200 OK` (`IReadOnlyList<RecipeSummaryDto>`) | `401` |
| `RecipesController` | `GET` | `/recipes/{id:guid}` | — | `200 OK` (`RecipeDetailDto`) | `401`, `404` |
| `RecipesController` | `POST` | `/recipes` | `[FromBody] UpsertRecipeRequest` | `201 Created` (`RecipeDetailDto`) | `400`, `401`, `409` |
| `RecipesController` | `PUT` | `/recipes/{id:guid}` | `[FromBody] UpsertRecipeRequest` | `200 OK` (`RecipeDetailDto`) | `400`, `401`, `404`, `409` |
| `RecipesController` | `DELETE` | `/recipes/{id:guid}` | — | `204 No Content` | `401`, `404`, `409` (linked as sub-recipe) |

---

## 5. Angular Frontend Design (`Client/`)

All components **MUST** follow `.agents/rules/frontend-angular.md`: separate `.ts`, `.html`, `.scss` files (never inline `template` or `styles`), no `standalone: true` property, `@Service()` on services, `inject()` DI, signals (`signal()`, `computed()`), native `@if` / `@for` control flow, Material Symbols Outlined icons, and Material 3 CSS variables (`--mat-sys-*`, `--app-success*`, `--app-warning*`).

### 5.1 Models, Utilities & `@Service()` Services (`src/app/core/`)
- **`core/models/food-catalog.models.ts`**: `StoreAisleDto`, `FoodItemDto`, `FoodItemQuery`, `CreateFoodItemRequest`, `UpdateFoodItemRequest`.
- **`core/models/recipe.models.ts`**: `RecipeStatus`, `RecipeDifficulty`, `MeasurementUnit`, `UnitSystem`, `MEASUREMENT_UNIT_OPTIONS` (code + label + group), `RecipeSummaryDto`, `SubRecipePreviewDto`, `RecipeIngredientDto`, `RecipeStepTimerDto`, `RecipeStepDto`, `RecipeDetailDto`, `RecipeQuery`, `RecipeIngredientInput`, `RecipeStepTimerInput`, `RecipeStepInput`, `UpsertRecipeRequest`.
- **`core/utils/culinary-math.ts`** (+ `culinary-math.spec.ts`):
  - `scaleAndConvertIngredient(ingredient, scaleFactor, unitSystem)` -> `{ formattedQuantity: string; unitLabel: string; fullLabel: string }`.
  - `formatKitchenNumber(value: number, isMetric: boolean): string`.
  - `formatStepDuration(durationSeconds: number): string`.
- **`core/services/food-catalog/food-catalog.service.ts`** (`@Service()`, `apiUrl('/store-aisles')` & `apiUrl('/food-items')`):
  - `getStoreAisles()`, `getFoodItems(query?)`, `getFoodItem(id)`, `createFoodItem(req)`, `updateFoodItem(id, req)`, `deleteFoodItem(id)`.
- **`core/services/recipe/recipe.service.ts`** (`@Service()`, `apiUrl('/recipes')`):
  - `getRecipes(query?)`, `getRecipe(id)`, `createRecipe(req)`, `updateRecipe(id, req)`, `deleteRecipe(id)`.

### 5.2 Authenticated App Shell & Routing (`src/app/features/shell/` & `app.routes.ts`)
- **`AppShellComponent` (`features/shell/app-shell.component.{ts,html,scss,spec.ts}`):**
  - Top bar with `APP_BRAND_ICON` (`skillet`) + `APP_NAME` (`LocalRoast`), quick `+ New Recipe` action button, Theme menu (`Light` / `Dark` / `System` / `Customize Theme...`), and User Account menu (`Account Settings` / `Log Out`).
  - **Desktop / Tablet (`>= 768px`):** Persistent left navigation rail (`width: 220px` or compact rail) with `<a mat-list-item routerLink="/recipes" routerLinkActive="...">` (`menu_book` **Cookbook**) and `<a routerLink="/catalog">` (`nutrition` **Food Catalog**).
  - **Mobile (`< 768px`, clean at `360px`):** Fixed bottom navigation bar (`height: 64px`, `padding-bottom: env(safe-area-inset-bottom)`) with thumb-friendly icon+label tabs (`Cookbook`, `Food Catalog`) and bottom content padding so cards/buttons are never obscured.
- **Routes (`app.routes.ts`):**
  - `''` -> `redirectTo: 'recipes'`
  - `'dashboard'` -> `redirectTo: 'recipes'` (preserves `guestGuard` & `LoginComponent` compatibility without touching template auth files)
  - `'login'` -> `LoginComponent` (`guestGuard`)
  - `''` (`component: AppShellComponent`, `canActivate: [authGuard]`) children:
    - `'recipes'` -> lazy `RecipeListComponent`
    - `'recipes/new'` -> lazy `RecipeEditorComponent`
    - `'recipes/:id'` -> lazy `RecipeDetailComponent`
    - `'recipes/:id/edit'` -> lazy `RecipeEditorComponent`
    - `'catalog'` -> lazy `FoodCatalogComponent`
  - `'**'` -> `redirectTo: 'recipes'`

### 5.3 Feature Components (`src/app/features/`)

1. **`FoodCatalogComponent` (`/catalog`) & `FoodItemDialogComponent`:**
   - Search bar (searches canonical names & aliases), Store Aisle `mat-select` filter, and filter toggle chips (`All`, `Pantry Staples Only`, `Shopping Items`).
   - Responsive list/grid grouped or badged by Store Aisle: displays `Name`, alias chips, referenced recipe count badge, 1-tap `mat-slide-toggle` or chip button to toggle `isPantryStaple` in-place, plus Edit and Delete (`ConfirmDialogComponent`) actions.
   - `FoodItemDialogComponent`: reactive form for `Name`, `DefaultStoreAisleId`, `IsPantryStaple` checkbox, and `MatChipsModule` alias input (add/remove synonym chips). Also reusable directly from the Recipe Editor to create a new `FoodItem` on the fly!

2. **`RecipeListComponent` (`/recipes`):**
   - Header with Cookbook count, search input, quick filter chips (`All Active`, `Quick <= 30m`, `Quick <= 45m`, `Drafts`, `Archived`, plus `Difficulty` selector), and primary `New Recipe` button (`routerLink="/recipes/new"`).
   - Responsive recipe cards (`1 col` at `360px`, `2–3 cols` on tablet/desktop): recipe placeholder artwork (`skillet` / `restaurant_menu` icon gradient when `thumbnailImagePath` is null), `Title`, `Status` badge (if `DraftReview`/`Archived`), `TotalTimeMinutes` pill, `Difficulty` badge, `BaseServings` label, and `Description` snippet. Clicking navigates to `/recipes/:id`.

3. **`RecipeDetailComponent` (`/recipes/:id`):**
   - Header metadata: `Title`, `Description`, `Status` chip, `Prep` / `Cook` / `Total` time pills, `Difficulty` pill, `YieldDisplay` or servings, and provenance link/book citation (`SourceUrl`, `SourceName`, `SourceAuthor`, `SourceBookTitle` + page). Action buttons: `Edit` (`/recipes/:id/edit`) and `Delete` (`ConfirmDialogComponent`).
   - **Interactive Culinary Controls Bar:**
     - Quick multiplier chips (`0.5x`, `1x`, `1.5x`, `2x`, `3x`) + `- / +` **Target Servings** stepper (`computed` `scaleFactor = targetServings() / recipe.baseServings`).
     - **Unit System Switcher** (`mat-button-toggle-group`: `Original` | `US` | `Metric`).
   - **Two-Column Desktop / Stacked `360px` Mobile Body:**
     - **Ingredients Panel:** Grouped by `SectionName` (default group when null). Each row renders scaled/converted quantity + unit (`culinary-math.ts`), `DisplayName`, `PreparationNote`, `Optional` tag, `Pantry` subtle badge, or—if `LinkedRecipeId` is set—an expandable **Sub-Recipe Card** showing scaled sub-recipe ingredients & steps inline (plus link to `/recipes/:linkedId`). Includes interactive check-off boxes for kitchen prep convenience.
     - **Instructions Panel:** Grouped by step `SectionName`, ordered by `StepNumber`. Each step renders **Inline Scaled Ingredient Callout Chips** (for every `id` in `step.ingredientIds`, resolved against the scaled ingredient map!) and **Step Timer Badges** (`timer` icon + formatted duration + label, e.g., `15 min · Simmer sauce`).

4. **`RecipeEditorComponent` (`/recipes/new` & `/recipes/:id/edit`):**
   - Clean, mobile-safe (`360px`) structured form:
     - **Metadata Card:** `Title`, `Status` (`Published` / `DraftReview` / `Archived`), `Description`, `BaseServings`, `YieldDisplay`, `PrepTimeMinutes`, `CookTimeMinutes`, `TotalTimeMinutes`, `Difficulty`, and collapsible **Source & Provenance** fields (`SourceUrl`, `SourceName`, `SourceAuthor`, `SourceBookTitle`, `SourceBookPage`).
     - **Ingredients Builder:** Dynamic list with Move Up / Move Down / Remove controls, optional `SectionName`, row mode toggle (`Food Ingredient` vs `Sub-Recipe Component`), `Quantity`, optional `MaxQuantity`, `Unit` selector (+ `CustomUnitName` when `Custom`), **`FoodItem` Autocomplete** (filtering loaded `FoodItemDto[]` by name & aliases, with an inline `+ Create "<query>" in Food Catalog` option opening `FoodItemDialogComponent` and auto-selecting the created item!), or **Sub-Recipe Selector** (excluding current recipe ID), `DisplayName`, `PreparationNote`, and `IsOptional`.
     - **Steps Builder:** Dynamic ordered steps with Move Up / Move Down / Remove controls, optional `SectionName`, `Instruction` textarea, **Step Ingredient Callout Multi-Select / Chips** (selecting from the current `Ingredients` list by index), and **Step Timers** sub-list (`Minutes` + `Seconds` inputs converted to `DurationSeconds` + `Label`).

---

## 6. Step-by-Step Execution Checklist (For Implementation Agent)

- [x] **Task 1: Backend Core, Entities, Configurations, Seeder & Migration**
  - Delete all `Todo` backend files (`Core/Todos/*`, `Data/Entities/TodoItem.cs`, `Data/Configurations/TodoItemConfiguration.cs`, `Data/Services/TodoService.cs`) and remove `TodoItems` from `User.cs` and `AppDbContext.cs`.
  - Implement `Core/FoodCatalog/*`, `Core/Recipes/*` (`RecipeEnums.cs`, `RecipeModels.cs`, `CulinaryMath.cs`, `IRecipeService.cs`).
  - Implement the 8 entities in `Data/Entities/`, the 8 fluent configurations in `Data/Configurations/` (strictly honoring `DeleteBehavior.Restrict` on `FoodItem.DefaultStoreAisleId`, `RecipeIngredient.FoodItemId`, `RecipeIngredient.LinkedRecipeId`, and `RecipeStepIngredient.RecipeIngredientId`), `Data/Seeding/HouseholdCatalogSeeder.cs`, `Data/Services/FoodCatalogService.cs`, and `Data/Services/RecipeService.cs`.
  - Register `IFoodCatalogService` and `IRecipeService` in `Data/DataServiceCollectionExtensions.cs` and wire `await HouseholdCatalogSeeder.SeedAsync(db);` in `API/Program.cs`.
  - Ensure SQL Server is up, generate & apply the EF Core migration, and format migrations:
    ```powershell
    .\scripts\ensure-local-infra.ps1
    dotnet ef migrations add ReplaceTodosWithFoodCatalogAndRecipes --project Data/App.Data.csproj
    dotnet ef database update --project Data/App.Data.csproj
    dotnet csharpier format Data/Migrations
    ```
  - Write unit tests in `Tests/Core.UnitTests/CulinaryMathTests.cs` covering fraction snapping, ranges, US-to-Metric & Metric-to-US volume/weight conversions, count unit preservation, and duration formatting, then run:
    ```powershell
    dotnet test --project Tests/Core.UnitTests/App.Core.UnitTests.csproj
    dotnet test --project Tests/Infrastructure.UnitTests/App.Infrastructure.UnitTests.csproj
    ```

- [x] **Task 2: API Controllers & Integration Tests**
  - Delete `API/Controllers/TodosController.cs` and `Tests/API.IntegrationTests/TodoIntegrationTests.cs`.
  - Create thin HTTP controllers: `StoreAislesController.cs`, `FoodItemsController.cs`, and `RecipesController.cs`.
  - Update `Tests/API.IntegrationTests/OpenApiIntegrationTests.cs` (`/store-aisles`, `/food-items`, `/recipes`).
  - Create `Tests/API.IntegrationTests/FoodCatalogIntegrationTests.cs` (seeded aisles & staples verification, search by alias, create/update/toggle pantry staple, `409` duplicate name/alias conflict, `409` delete protection when referenced by a recipe, global household visibility across two users).
  - Create `Tests/API.IntegrationTests/RecipesIntegrationTests.cs` (create/get/list/filter/update/delete structured recipes with sections, ranges, step ingredient callout indices, and step timers; cross-user global household collaboration; Sub-Recipe inline preview + self-link `400` + `A -> B -> A` cycle `409` + delete-while-linked `409`).
  - Build and run API integration tests until 100% green:
    ```powershell
    dotnet build App.slnx
    dotnet test --project Tests/API.IntegrationTests/App.API.IntegrationTests.csproj
    ```

- [x] **Task 3: Angular Client, Culinary Math & Frontend Unit Tests**
  - Move `ConfirmDialogComponent` to `Client/src/app/shared/components/confirm-dialog/` and delete `Client/src/app/core/models/todo.models.ts`, `Client/src/app/core/services/todo/`, and `Client/src/app/features/dashboard/`.
  - Implement `food-catalog.models.ts`, `recipe.models.ts`, `culinary-math.ts`, `FoodCatalogService`, and `RecipeService`.
  - Implement `AppShellComponent`, `FoodCatalogComponent`, `FoodItemDialogComponent`, `RecipeListComponent`, `RecipeDetailComponent`, and `RecipeEditorComponent` (strictly external `.ts`/`.html`/`.scss` files, accessible labels, and responsive `360px` + desktop layouts), and wire `app.routes.ts`.
  - Write `.spec.ts` unit tests for `culinary-math.spec.ts`, `food-catalog.service.spec.ts`, `recipe.service.spec.ts`, `app-shell.component.spec.ts`, `food-catalog.component.spec.ts`, `recipe-list.component.spec.ts`, `recipe-detail.component.spec.ts`, and `recipe-editor.component.spec.ts`.
  - Run frontend lint, production build, and unit tests in `Client/` until 100% green:
    ```powershell
    # In Client/:
    npm run lint
    npm run build:production
    npm test -- --watch=false
    ```

- [x] **Task 4: Playwright E2E Verification & Final Formatting**
  - Create `Tests/Client.E2ETests/PlaywrightServerFixture.cs` (starts `App.API` child process on a free loopback port with `--environment Testing --UseInMemoryDatabase=true --webroot <Client/dist/App.Client/browser>` and JWT settings, polling `/api/openapi/v1.json` until ready) and update `Tests/Client.E2ETests/ClientSmokeTests.cs` to verify end-to-end user registration, Food Catalog item creation, structured Recipe creation with ingredients/steps/timers, Recipe Detail live `2x` scaling & `Metric` unit switching, and `360px` mobile navigation bar rendering.
  - Ensure Playwright Chromium is installed if needed (`pwsh Tests/Client.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium`), build the Angular production bundle (`npm run build:production` in `Client/`), and run the full verification suite:
    ```powershell
    dotnet csharpier format .
    # In Client/:
    npm run format
    npm run lint:fix
    npm run build:production
    npm test -- --watch=false
    # From repo root:
    dotnet build App.slnx
    dotnet test --project Tests/Core.UnitTests/App.Core.UnitTests.csproj
    dotnet test --project Tests/Infrastructure.UnitTests/App.Infrastructure.UnitTests.csproj
    dotnet test --project Tests/API.IntegrationTests/App.API.IntegrationTests.csproj
    dotnet test --project Tests/Client.E2ETests/App.Client.E2ETests.csproj
    ```
  - Update this plan's status header to `[x] Completed` and commit clean changes per `.agents/skills/execute-phase/SKILL.md`.
