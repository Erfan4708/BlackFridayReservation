using BlackFridayReservation.src.BlackFriday.Api.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);

            // Composite alternate key to support tenant-aware foreign keys from other tables
            builder.HasAlternateKey(p => new { p.TenantId, p.Id });

            builder.Property(p => p.SKU).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.RegularPrice).HasColumnType("decimal(18,2)");

            // Unique SKU within a tenant
            builder.HasIndex(p => new { p.TenantId, p.SKU }).IsUnique();

            builder.HasOne(p => p.Tenant)
                .WithMany(t => t.Products)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
