using App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public sealed class RecipeStepIngredientConfiguration
    : IEntityTypeConfiguration<RecipeStepIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeStepIngredient> builder)
    {
        builder.ToTable("RecipeStepIngredients");

        builder.HasKey(rsi => rsi.Id);

        builder.HasIndex(rsi => new { rsi.RecipeStepId, rsi.RecipeIngredientId }).IsUnique();

        builder
            .HasOne(rsi => rsi.RecipeStep)
            .WithMany(rs => rs.IngredientReferences)
            .HasForeignKey(rsi => rsi.RecipeStepId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(rsi => rsi.RecipeIngredient)
            .WithMany(ri => ri.StepReferences)
            .HasForeignKey(rsi => rsi.RecipeIngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rsi => rsi.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(rsi => rsi.ModifiedBy).IsRequired().HasMaxLength(256);
    }
}
