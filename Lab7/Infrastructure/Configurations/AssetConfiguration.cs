using Lab7.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lab7.Infrastructure.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.InventoryNumber)
            .HasColumnName("inventory_number")
            .IsRequired();

        builder.Property(x => x.Model)
            .HasColumnName("model")
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .IsRequired();

        builder.Property(x => x.Assigned)
            .HasColumnName("assigned")
            .IsRequired();

        builder.HasIndex(x => x.InventoryNumber)
            .IsUnique()
            .HasDatabaseName("uq_assets_inventory_number");
    }
}