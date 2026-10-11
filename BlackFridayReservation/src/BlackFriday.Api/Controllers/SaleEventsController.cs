using BlackFridayReservation.src.BlackFriday.Api.Application.Reservations;
using Microsoft.AspNetCore.Mvc;

namespace BlackFridayReservation.src.BlackFriday.Api.Controllers
{
    [ApiController]
    [Route("api/sale-events")]
    public class SaleEventsController : ControllerBase
    {
        private readonly ISaleEventService _saleEventService;

        public SaleEventsController(ISaleEventService saleEventService)
        {
            _saleEventService = saleEventService;
        }

        private Guid GetTenantId() => (Guid)HttpContext.Items["TenantId"]!;

        [HttpGet]
        public async Task<IActionResult> GetActiveSaleEvents(CancellationToken cancellationToken)
        {
            var tenantId = GetTenantId();
            var events = await _saleEventService.GetActiveSaleEventsAsync(tenantId, cancellationToken);
            return Ok(events);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetSaleEvent(Guid id, CancellationToken cancellationToken)
        {
            var tenantId = GetTenantId();
            var saleEvent = await _saleEventService.GetByIdAsync(tenantId, id, cancellationToken);

            if (saleEvent == null)
                return NotFound(new { error = "Sale event not found." });

            return Ok(saleEvent);
        }
    }
}
