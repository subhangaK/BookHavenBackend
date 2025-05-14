using System.Threading;
using System.Threading.Tasks;

namespace Book_Haven.Services
{
    public interface IDiscountCleanupService
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}