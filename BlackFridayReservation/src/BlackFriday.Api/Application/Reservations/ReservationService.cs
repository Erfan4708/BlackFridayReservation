using BlackFridayReservation.src.BlackFriday.Api.Domain;
using BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Application.Reservations
{
    public interface IReservationService
    {
        Task<ReservationResult> CreateReservationAsync(Guid tenantId, Guid saleEventId, string customerId, int quantity, string idempotencyKey, CancellationToken cancellationToken);
    }

    public record ReservationResult(bool Success, string Message, Reservation? Reservation, int StatusCode);

    public class ReservationService : IReservationService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(AppDbContext dbContext, ILogger<ReservationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ReservationResult> CreateReservationAsync(
            Guid tenantId,
            Guid saleEventId,
            string customerId,
            int quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            if (quantity <= 0)
            {
                return new ReservationResult(false, "Quantity must be greater than zero.", null, 400);
            }

            var existingReservation = await _dbContext.Reservations
                .Include(r => r.BackgroundJob)
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existingReservation != null)
            {
                if (existingReservation.SaleEventId == saleEventId &&
                    existingReservation.Quantity == quantity &&
                    existingReservation.CustomerId == customerId)
                {
                    _logger.LogInformation("Idempotent request matched for key {IdempotencyKey} and tenant {TenantId}.", idempotencyKey, tenantId);
                    return new ReservationResult(true, "Reservation already exists (Idempotent match).", existingReservation, 200);
                }

                _logger.LogWarning("Idempotency conflict for key {IdempotencyKey} and tenant {TenantId}.", idempotencyKey, tenantId);
                return new ReservationResult(false, "Idempotency key reused with different payload.", null, 409);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var nowUtc = DateTime.UtcNow;

                var affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE SaleEvents
                SET RemainingQuantity = RemainingQuantity - {quantity}
                WHERE Id = {saleEventId} 
                  AND TenantId = {tenantId} 
                  AND StartsAtUtc <= {nowUtc} 
                  AND EndsAtUtc > {nowUtc} 
                  AND RemainingQuantity >= {quantity}", cancellationToken);

                if (affectedRows == 0)
                {
                    var saleEvent = await _dbContext.SaleEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == saleEventId && s.TenantId == tenantId, cancellationToken);

                    if (saleEvent == null)
                    {
                        return new ReservationResult(false, "Sale event not found for this tenant.", null, 404);
                    }
                    if (!saleEvent.IsActive(nowUtc))
                    {
                        return new ReservationResult(false, "Sale event is not currently active.", null, 400);
                    }
                    return new ReservationResult(false, "Insufficient remaining stock or concurrency conflict.", null, 400);
                }

                var reservation = new Reservation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleEventId = saleEventId,
                    CustomerId = customerId,
                    Quantity = quantity,
                    IdempotencyKey = idempotencyKey,
                    Status = ReservationStatus.Confirmed,
                    CreatedAtUtc = nowUtc
                };

                _dbContext.Reservations.Add(reservation);

                var backgroundJob = new BackgroundJob
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ReservationId = reservation.Id,
                    Status = JobStatus.Pending,
                    Attempts = 0,
                    MaxAttempts = 3,
                    NextAttemptAtUtc = nowUtc,
                    CreatedAtUtc = nowUtc
                };

                _dbContext.BackgroundJobs.Add(backgroundJob);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new ReservationResult(true, "Reservation created successfully.", reservation, 201);
            }
            catch (DbUpdateException ex) when (ex.Message.Contains("IX_Reservations_TenantId_IdempotencyKey") || (ex.InnerException?.Message.Contains("IX_Reservations_TenantId_IdempotencyKey") == true))
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Concurrency race detected on idempotency key {IdempotencyKey}.", idempotencyKey);

                var concurrentExisting = await _dbContext.Reservations
                    .Include(r => r.BackgroundJob)
                    .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, cancellationToken);

                if (concurrentExisting != null)
                {
                    return new ReservationResult(true, "Reservation already exists (Concurrent match).", concurrentExisting, 200);
                }

                return new ReservationResult(false, "Idempotency conflict occurred during concurrent requests.", null, 409);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
