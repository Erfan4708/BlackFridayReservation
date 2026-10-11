using BlackFridayReservation.src.BlackFriday.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence.Configurations
{

    public class SaleEventConfiguration : IEntityTypeConfiguration<SaleEvent>
    {
        public void Configure(EntityTypeBuilder<SaleEvent> builder)
        {
            builder.HasKey(s => s.Id);

            // Composite alternate key for tenant-aware relationship
            builder.HasAlternateKey(s => new { s.TenantId, s.Id });

            builder.Property(s => s.SalePrice).HasColumnType("decimal(18,2)");

            builder.ToTable(t => t.HasCheckConstraint("CK_SaleEvent_InitialQuantity_Positive", "[InitialQuantity] > 0"));
            builder.ToTable(t => t.HasCheckConstraint("CK_SaleEvent_RemainingQuantity_Range", "[RemainingQuantity] >= 0 AND [RemainingQuantity] <= [InitialQuantity]"));
            builder.ToTable(t => t.HasCheckConstraint("CK_SaleEvent_Prices_NonNegative", "[SalePrice] >= 0"));
            builder.ToTable(t => t.HasCheckConstraint("CK_SaleEvent_Dates_Valid", "[EndsAtUtc] > [StartsAtUtc]"));

            builder.HasIndex(s => new { s.TenantId, s.StartsAtUtc, s.EndsAtUtc });

            builder.HasOne(s => s.Tenant)
                .WithMany(t => t.SaleEvents)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Composite foreign key to ensure product belongs to the same tenant
            builder.HasOne(s => s.Product)
                .WithMany(p => p.SaleEvents)
                .HasForeignKey(s => new { s.TenantId, s.ProductId })
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
