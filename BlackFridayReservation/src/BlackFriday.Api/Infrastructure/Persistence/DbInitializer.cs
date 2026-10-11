using BlackFridayReservation.src.BlackFriday.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            if (await context.Tenants.AnyAsync())
            {
                return;
            }

            var now = DateTime.UtcNow;

            var electronicsTenant = new Tenant { Id = Guid.NewGuid(), TenantKey = "tenant-electronics", Name = "Electronics Store" };
            var fashionTenant = new Tenant { Id = Guid.NewGuid(), TenantKey = "tenant-fashion", Name = "Fashion Boutique" };

            context.Tenants.AddRange(electronicsTenant, fashionTenant);

            var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, SKU = "SKU-ELEC-01", Name = "65-inch 4K OLED TV", RegularPrice = 1500m },
            new() { Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, SKU = "SKU-ELEC-02", Name = "Wireless Noise-Canceling Headphones", RegularPrice = 300m },
            new() { Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, SKU = "SKU-ELEC-03", Name = "Flagship Smartphone 5G", RegularPrice = 900m },
            new() { Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, SKU = "SKU-ELEC-04", Name = "Gaming Laptop RTX 4080", RegularPrice = 2200m },
            new() { Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, SKU = "SKU-ELEC-05", Name = "Smartwatch Series 9", RegularPrice = 400m },

            new() { Id = Guid.NewGuid(), TenantId = fashionTenant.Id, SKU = "SKU-FASH-01", Name = "Classic Leather Jacket", RegularPrice = 250m },
            new() { Id = Guid.NewGuid(), TenantId = fashionTenant.Id, SKU = "SKU-FASH-02", Name = "Designer Silk Scarf", RegularPrice = 80m },
            new() { Id = Guid.NewGuid(), TenantId = fashionTenant.Id, SKU = "SKU-FASH-03", Name = "Running Sneakers Pro", RegularPrice = 140m },
            new() { Id = Guid.NewGuid(), TenantId = fashionTenant.Id, SKU = "SKU-FASH-04", Name = "Wool Winter Overcoat", RegularPrice = 320m },
            new() { Id = Guid.NewGuid(), TenantId = fashionTenant.Id, SKU = "SKU-FASH-05", Name = "Casual Denim Jeans", RegularPrice = 90m }
        };

            context.Products.AddRange(products);

            var saleEvents = new List<SaleEvent>
        {
            // Electronics Events
            new() {
                Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, ProductId = products[0].Id,
                SalePrice = 999m, InitialQuantity = 100, RemainingQuantity = 100,
                StartsAtUtc = now.AddMinutes(-30), EndsAtUtc = now.AddHours(2)
            },
            new() {
                Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, ProductId = products[1].Id,
                SalePrice = 199m, InitialQuantity = 150, RemainingQuantity = 150,
                StartsAtUtc = now.AddHours(5), EndsAtUtc = now.AddHours(8)
            },
            new() {
                Id = Guid.NewGuid(), TenantId = electronicsTenant.Id, ProductId = products[2].Id,
                SalePrice = 699m, InitialQuantity = 80, RemainingQuantity = 0,
                StartsAtUtc = now.AddHours(-5), EndsAtUtc = now.AddHours(-1)
            },

            // Fashion Events
            new() {
                Id = Guid.NewGuid(), TenantId = fashionTenant.Id, ProductId = products[5].Id,
                SalePrice = 150m, InitialQuantity = 120, RemainingQuantity = 120,
                StartsAtUtc = now.AddMinutes(-10), EndsAtUtc = now.AddHours(1)
            },
            new() {
                Id = Guid.NewGuid(), TenantId = fashionTenant.Id, ProductId = products[6].Id,
                SalePrice = 45m, InitialQuantity = 200, RemainingQuantity = 200,
                StartsAtUtc = now.AddHours(2), EndsAtUtc = now.AddHours(4)
            },
            new() {
                Id = Guid.NewGuid(), TenantId = fashionTenant.Id, ProductId = products[7].Id,
                SalePrice = 90m, InitialQuantity = 75, RemainingQuantity = 5,
                StartsAtUtc = now.AddHours(-6), EndsAtUtc = now.AddHours(-2)
            }
        };

            context.SaleEvents.AddRange(saleEvents);
            await context.SaveChangesAsync();
        }
    }
}
