namespace BlackFridayReservation.src.BlackFriday.Api.Domain
{
    public sealed class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TenantKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<SaleEvent> SaleEvents { get; set; } = new List<SaleEvent>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<BackgroundJob> BackgroundJobs { get; set; } = new List<BackgroundJob>();
    }
}
