using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Book_Haven.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Book_Haven.Services
{
    public class DiscountCleanupService : BackgroundService, IDiscountCleanupService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiscountCleanupService> _logger;
        private readonly TimeSpan _checkInterval;

        public DiscountCleanupService(
            IServiceProvider serviceProvider,
            ILogger<DiscountCleanupService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Read interval from configuration, default to 1 hour
            _checkInterval = configuration.GetValue<TimeSpan>("DiscountCleanup:CheckInterval", TimeSpan.FromHours(1));
        }

        // Explicit interface implementation for IDiscountCleanupService
        async Task IDiscountCleanupService.ExecuteAsync(CancellationToken stoppingToken)
        {
            await ExecuteAsyncInternal(stoppingToken);
        }

        // Protected method for BackgroundService
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ExecuteAsyncInternal(stoppingToken);
        }

        private async Task ExecuteAsyncInternal(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiscountCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var expiredBooks = await context.Books
                            .Where(b => b.IsOnSale && b.SaleEndDate < DateTime.UtcNow)
                            .Take(100) // Process in batches to avoid memory issues
                            .ToListAsync(stoppingToken);

                        if (expiredBooks.Any())
                        {
                            foreach (var book in expiredBooks)
                            {
                                book.IsOnSale = false;
                                book.DiscountPercentage = 0;
                                book.SaleStartDate = null;
                                book.SaleEndDate = null;
                            }

                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Cleared discounts for {Count} books.", expiredBooks.Count);
                        }
                        else
                        {
                            _logger.LogDebug("No expired discounts found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up discounts.");
                    // Optionally, delay before retrying to avoid rapid failure loops
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("DiscountCleanupService stopped.");
        }
    }
}


