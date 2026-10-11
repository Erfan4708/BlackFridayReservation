using BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Middlewares
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        public const string TenantHeaderName = "X-Tenant-Key";

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            if (!context.Request.Headers.TryGetValue(TenantHeaderName, out var tenantKeyValues) || string.IsNullOrWhiteSpace(tenantKeyValues))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid X-Tenant-Key header." });
                return;
            }

            var tenantKey = tenantKeyValues.ToString();
            var tenant = await dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantKey == tenantKey);

            if (tenant == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant not found." });
                return;
            }

            context.Items["TenantId"] = tenant.Id;
            context.Items["TenantKey"] = tenant.TenantKey;

            await _next(context);
        }
    }
}
