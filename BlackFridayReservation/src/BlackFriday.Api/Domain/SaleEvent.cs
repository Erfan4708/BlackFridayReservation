namespace BlackFridayReservation.src.BlackFriday.Api.Domain
{
    public sealed class SaleEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid ProductId { get; set; }
        public decimal SalePrice { get; set; }
        public int InitialQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // Helper property to check if active
        public bool IsActive(DateTime nowUtc) => StartsAtUtc <= nowUtc && EndsAtUtc > nowUtc;
    }
}
