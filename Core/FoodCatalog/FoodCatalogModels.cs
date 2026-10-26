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
    [Required, MaxLength(150)] string Name,
    Guid? DefaultStoreAisleId = null,
    bool IsPantryStaple = false,
    IReadOnlyList<string>? Aliases = null
);

public record UpdateFoodItemRequest(
    [Required, MaxLength(150)] string Name,
    Guid? DefaultStoreAisleId = null,
    bool IsPantryStaple = false,
    IReadOnlyList<string>? Aliases = null
);
