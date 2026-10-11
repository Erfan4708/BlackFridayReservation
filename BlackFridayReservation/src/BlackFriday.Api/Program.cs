using BlackFridayReservation.src.BlackFriday.Api.Application.Reservations;
using BlackFridayReservation.src.BlackFriday.Api.Application;
using BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence;
using BlackFridayReservation.src.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("X-Tenant-Key", new OpenApiSecurityScheme
    {
        Description = "",
        Name = "X-Tenant-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "X-Tenant-Key"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Caching & Application Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISaleEventService, CachedSaleEventService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

// Background Worker for asynchronous processing
builder.Services.AddHostedService<BackgroundJobWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Tenant Resolution Middleware
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

// Apply migrations and seed data automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(dbContext);
}

app.Run();

public partial class Program { }