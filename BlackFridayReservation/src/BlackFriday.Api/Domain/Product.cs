namespace BlackFridayReservation.src.BlackFriday.Api.Domain
{
    public sealed class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal RegularPrice { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public ICollection<SaleEvent> SaleEvents { get; set; } = new List<SaleEvent>();
    }
}
