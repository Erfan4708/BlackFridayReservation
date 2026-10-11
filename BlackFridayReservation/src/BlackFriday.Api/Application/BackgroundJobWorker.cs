using BlackFridayReservation.src.BlackFriday.Api.Domain;
using BlackFridayReservation.src.BlackFriday.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlackFridayReservation.src.BlackFriday.Api.Application
{
    public class BackgroundJobWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobWorker> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

        public BackgroundJobWorker(IServiceProvider serviceProvider, ILogger<BackgroundJobWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundJobWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing background jobs.");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task ProcessJobsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var nowUtc = DateTime.UtcNow;

            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE BackgroundJobs
            SET Status = 0, NextAttemptAtUtc = {nowUtc}
            WHERE Status = 1 AND LeaseUntilUtc <= {nowUtc}", cancellationToken);

            var eligibleJob = await dbContext.BackgroundJobs
                .Include(j => j.Reservation)
                .Where(j => j.Status == JobStatus.Pending && j.NextAttemptAtUtc <= nowUtc)
                .OrderBy(j => j.NextAttemptAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (eligibleJob == null) return;

            var leaseUntil = nowUtc.AddMinutes(2);
            var affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE BackgroundJobs
            SET Status = 1, 
                Attempts = Attempts + 1, 
                LeaseUntilUtc = {leaseUntil}
            WHERE Id = {eligibleJob.Id} 
              AND Status = 0 
              AND Attempts < MaxAttempts", cancellationToken);

            if (affectedRows == 0) return;

            _logger.LogInformation("Claimed job {JobId} for tenant {TenantId}, attempt {Attempt}.", eligibleJob.Id, eligibleJob.TenantId, eligibleJob.Attempts + 1);

            try
            {
                await SimulateDownstreamWorkflowAsync(eligibleJob);

                eligibleJob.Status = JobStatus.Completed;
                eligibleJob.CompletedAtUtc = DateTime.UtcNow;
                eligibleJob.LeaseUntilUtc = null;
                eligibleJob.LastError = null;

                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully completed job {JobId}.", eligibleJob.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Job {JobId} failed on attempt {Attempt}.", eligibleJob.Id, eligibleJob.Attempts);

                eligibleJob.LastError = ex.Message;
                eligibleJob.LeaseUntilUtc = null;

                if (eligibleJob.Attempts >= eligibleJob.MaxAttempts)
                {
                    eligibleJob.Status = JobStatus.DeadLetter;
                    _logger.LogError("Job {JobId} moved to DeadLetter queue after reaching max attempts.", eligibleJob.Id);
                }
                else
                {
                    eligibleJob.Status = JobStatus.Pending;
                    var backoffSeconds = Math.Pow(2, eligibleJob.Attempts) * 5;
                    eligibleJob.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(backoffSeconds);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private Task SimulateDownstreamWorkflowAsync(BackgroundJob job)
        {
            return Task.Delay(100);
        }
    }
}
