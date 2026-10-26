using App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public sealed class StoreAisleConfiguration : IEntityTypeConfiguration<StoreAisle>
{
    public void Configure(EntityTypeBuilder<StoreAisle> builder)
    {
        builder.ToTable("StoreAisles");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);

        builder.Property(a => a.NormalizedName).IsRequired().HasMaxLength(100);
        builder.HasIndex(a => a.NormalizedName).IsUnique();

        builder.HasIndex(a => a.SortOrder);

        builder.Property(a => a.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(a => a.ModifiedBy).IsRequired().HasMaxLength(256);
    }
}
