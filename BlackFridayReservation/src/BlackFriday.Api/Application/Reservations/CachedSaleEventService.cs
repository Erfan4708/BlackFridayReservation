using BlackFridayReservation.src.BlackFriday.Api.Domain;
using BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BlackFridayReservation.src.BlackFriday.Api.Application.Reservations
{
    public interface ISaleEventService
    {
        Task<IEnumerable<SaleEvent>> GetActiveSaleEventsAsync(Guid tenantId, CancellationToken cancellationToken);
        Task<SaleEvent?> GetByIdAsync(Guid tenantId, Guid saleEventId, CancellationToken cancellationToken);
    }

    public class CachedSaleEventService : ISaleEventService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        public CachedSaleEventService(AppDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<IEnumerable<SaleEvent>> GetActiveSaleEventsAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            string cacheKey = $"tenant_{tenantId}_sale_events";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<SaleEvent>? cachedEvents) && cachedEvents != null)
            {
                return cachedEvents;
            }

            var events = await _dbContext.SaleEvents
                .Include(s => s.Product)
                .Where(s => s.TenantId == tenantId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _cache.Set(cacheKey, events, CacheTtl);
            return events;
        }

        public async Task<SaleEvent?> GetByIdAsync(Guid tenantId, Guid saleEventId, CancellationToken cancellationToken)
        {
            string cacheKey = $"tenant_{tenantId}_sale_event_{saleEventId}";

            if (_cache.TryGetValue(cacheKey, out SaleEvent? cachedEvent) && cachedEvent != null)
            {
                return cachedEvent;
            }

            var saleEvent = await _dbContext.SaleEvents
                .Include(s => s.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == saleEventId, cancellationToken);

            if (saleEvent != null)
            {
                _cache.Set(cacheKey, saleEvent, CacheTtl);
            }

            return saleEvent;
        }
    }
}
