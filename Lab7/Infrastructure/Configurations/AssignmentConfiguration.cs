using Lab7.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lab7.Infrastructure.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(x => x.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ReturnedAt)
            .HasColumnName("returned_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AssetId)
            .IsUnique()
            .HasFilter("returned_at is null");
    }
}