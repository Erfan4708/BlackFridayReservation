using BlackFridayReservation.src.BlackFriday.Api.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.HasKey(r => r.Id);

            // Composite alternate key for job relationship
            builder.HasAlternateKey(r => new { r.TenantId, r.Id });

            builder.Property(r => r.CustomerId).IsRequired().HasMaxLength(100);
            builder.Property(r => r.IdempotencyKey).IsRequired().HasMaxLength(150);

            builder.ToTable(t => t.HasCheckConstraint("CK_Reservation_Quantity_Positive", "[Quantity] > 0"));

            // Idempotency uniqueness per tenant
            builder.HasIndex(r => new { r.TenantId, r.IdempotencyKey }).IsUnique();
            builder.HasIndex(r => new { r.TenantId, r.SaleEventId });

            builder.HasOne(r => r.Tenant)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.SaleEvent)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => new { r.TenantId, r.SaleEventId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
