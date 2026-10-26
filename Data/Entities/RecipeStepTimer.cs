namespace App.Data.Entities;

public sealed class RecipeStepTimer : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RecipeStepId { get; set; }

    public int SortOrder { get; set; }

    public int DurationSeconds { get; set; }

    public string Label { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = "System";

    public DateTimeOffset ModifiedAt { get; set; }

    public string ModifiedBy { get; set; } = "System";

    public RecipeStep RecipeStep { get; set; } = null!;
}
