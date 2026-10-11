using BlackFridayReservation.src.BlackFriday.Api.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence.Configurations
{
    public class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
    {
        public void Configure(EntityTypeBuilder<BackgroundJob> builder)
        {
            builder.HasKey(j => j.Id);

            // unique job per reservation and tenant
            builder.HasIndex(j => new { j.TenantId, j.ReservationId }).IsUnique();

            // index for fast polling of pending/due jobs
            builder.HasIndex(j => new { j.TenantId, j.Status, j.NextAttemptAtUtc });

            builder.HasOne(j => j.Tenant)
                .WithMany(t => t.BackgroundJobs)
                .HasForeignKey(j => j.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(j => j.Reservation)
                .WithOne(r => r.BackgroundJob)
                .HasForeignKey<BackgroundJob>(j => new { j.TenantId, j.ReservationId })
                .HasPrincipalKey<Reservation>(r => new { r.TenantId, r.Id })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
