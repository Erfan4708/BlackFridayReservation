using BlackFridayReservation.src.BlackFriday.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<SaleEvent> SaleEvents => Set<SaleEvent>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
