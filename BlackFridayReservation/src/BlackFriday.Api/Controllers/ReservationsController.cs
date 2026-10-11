using BlackFridayReservation.src.BlackFriday.Api.Application.Reservations;
using BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly AppDbContext _dbContext;

        public ReservationsController(IReservationService reservationService, AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _reservationService = reservationService;
        }

        private Guid GetTenantId() => (Guid)HttpContext.Items["TenantId"]!;

        public record CreateReservationRequest(Guid SaleEventId, string CustomerId, int Quantity, string IdempotencyKey);

        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
        {
            var tenantId = GetTenantId();

            var result = await _reservationService.CreateReservationAsync(
                tenantId,
                request.SaleEventId,
                request.CustomerId,
                request.Quantity,
                request.IdempotencyKey,
                cancellationToken);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { error = result.Message });
            }

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                reservationId = result.Reservation?.Id,
                saleEventId = result.Reservation?.SaleEventId,
                quantity = result.Reservation?.Quantity,
                status = result.Reservation?.Status.ToString(),
                createdAt = result.Reservation?.CreatedAtUtc
            });
        }

        [HttpGet("dead-letter-jobs")]
        public async Task<IActionResult> GetDeadLetterJobs(CancellationToken cancellationToken)
        {
            var tenantId = GetTenantId();
            var deadLetters = await _dbContext.BackgroundJobs
                .Where(j => j.TenantId == tenantId && j.Status == Domain.JobStatus.DeadLetter)
                .Select(j => new
                {
                    j.Id,
                    j.ReservationId,
                    j.Attempts,
                    j.LastError,
                    j.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return Ok(deadLetters);
        }
    }
}
