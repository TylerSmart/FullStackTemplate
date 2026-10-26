using App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Data.Seeding;

public static class HouseholdCatalogSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default
    )
    {
        var hasAisles = await db.StoreAisles.AnyAsync(cancellationToken);
        if (!hasAisles)
        {
            var defaultAisles = new (string Name, int SortOrder)[]
            {
                ("Produce", 10),
                ("Bakery", 20),
                ("Deli", 30),
                ("Meat & Seafood", 40),
                ("Dairy & Eggs", 50),
                ("Canned & Jarred", 60),
                ("Dry Goods & Pasta", 70),
                ("International", 80),
                ("Spices & Baking", 90),
                ("Frozen", 100),
                ("Beverages", 110),
                ("Household", 120),
                ("Other", 130),
            };

            foreach (var (name, sortOrder) in defaultAisles)
            {
                db.StoreAisles.Add(
                    new StoreAisle
                    {
                        Name = name,
                        NormalizedName = name.Trim().ToLowerInvariant(),
                        SortOrder = sortOrder,
                        IsSystemSeeded = true,
                    }
                );
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        var hasFoodItems = await db.FoodItems.AnyAsync(cancellationToken);
        if (!hasFoodItems)
        {
            var aisles = await db.StoreAisles.ToDictionaryAsync(
                a => a.NormalizedName,
                a => a.Id,
                cancellationToken
            );

            Guid? GetAisleId(string aisleName) =>
                aisles.TryGetValue(aisleName.Trim().ToLowerInvariant(), out var id) ? id : null;

            var starterFoodItems = new (
                string Name,
                string AisleName,
                bool IsPantryStaple,
                string[] Aliases
            )[]
            {
                ("Water", "Beverages", true, []),
                ("Kosher Salt", "Spices & Baking", true, ["Salt", "Fine Sea Salt"]),
                (
                    "Black Pepper",
                    "Spices & Baking",
                    true,
                    ["Freshly Ground Black Pepper", "Peppercorns"]
                ),
                ("Extra-Virgin Olive Oil", "Spices & Baking", true, ["Olive Oil", "EVOO"]),
                ("Neutral Cooking Oil", "Spices & Baking", true, ["Vegetable Oil", "Canola Oil"]),
                ("All-Purpose Flour", "Spices & Baking", true, ["AP Flour", "Plain Flour"]),
                ("Granulated Sugar", "Spices & Baking", true, ["White Sugar"]),
                ("Baking Powder", "Spices & Baking", true, []),
                ("Baking Soda", "Spices & Baking", true, []),
                ("Unsalted Butter", "Dairy & Eggs", true, ["Butter"]),
                ("Garlic", "Produce", false, ["Garlic Cloves"]),
                ("Yellow Onion", "Produce", false, ["Onion", "Sweet Onion"]),
                ("Green Onion", "Produce", false, ["Scallion", "Spring Onion"]),
                ("Cilantro", "Produce", false, ["Fresh Coriander"]),
                ("Lemon", "Produce", false, []),
                ("Lime", "Produce", false, []),
                ("Large Eggs", "Dairy & Eggs", false, ["Egg", "Eggs"]),
                ("Chicken Thighs", "Meat & Seafood", false, ["Boneless Skinless Chicken Thighs"]),
            };

            foreach (var (name, aisleName, isPantryStaple, aliases) in starterFoodItems)
            {
                var foodItem = new FoodItem
                {
                    Name = name,
                    NormalizedName = name.Trim().ToLowerInvariant(),
                    DefaultStoreAisleId = GetAisleId(aisleName),
                    IsPantryStaple = isPantryStaple,
                };

                foreach (var alias in aliases)
                {
                    var cleanAlias = alias.Trim();
                    if (!string.IsNullOrWhiteSpace(cleanAlias))
                    {
                        foodItem.Aliases.Add(
                            new FoodItemAlias
                            {
                                Alias = cleanAlias,
                                NormalizedAlias = cleanAlias.ToLowerInvariant(),
                            }
                        );
                    }
                }

                db.FoodItems.Add(foodItem);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
