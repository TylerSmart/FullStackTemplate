namespace App.Data.Entities;

public sealed class FoodItemAlias : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FoodItemId { get; set; }

    public string Alias { get; set; } = string.Empty;

    public string NormalizedAlias { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = "System";

    public DateTimeOffset ModifiedAt { get; set; }

    public string ModifiedBy { get; set; } = "System";

    public FoodItem FoodItem { get; set; } = null!;
}
