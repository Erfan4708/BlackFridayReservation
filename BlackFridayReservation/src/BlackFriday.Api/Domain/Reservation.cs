namespace BlackFridayReservation.src.BlackFriday.Api.Domain
{

    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid SaleEventId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public SaleEvent SaleEvent { get; set; } = null!;
        public BackgroundJob? BackgroundJob { get; set; }
    }

    public enum ReservationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Failed = 2,
        Cancelled = 3
    }
}
