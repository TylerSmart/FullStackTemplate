namespace App.Data.Entities;

public sealed class StoreAisle : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsSystemSeeded { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = "System";

    public DateTimeOffset ModifiedAt { get; set; }

    public string ModifiedBy { get; set; } = "System";

    public ICollection<FoodItem> FoodItems { get; set; } = new List<FoodItem>();
}
