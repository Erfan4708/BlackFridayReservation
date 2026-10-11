namespace BlackFridayReservation.src.BlackFriday.Api.Domain
{
    public class BackgroundJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid ReservationId { get; set; }
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public int Attempts { get; set; } = 0;
        public int MaxAttempts { get; set; } = 3;
        public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LeaseUntilUtc { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public Reservation Reservation { get; set; } = null!;
    }

    public enum JobStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        DeadLetter = 3
    }
}
