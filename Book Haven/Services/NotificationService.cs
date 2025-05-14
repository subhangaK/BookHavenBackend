using Book_Haven;
using Book_Haven.Entities;
using Book_Haven.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Book_Haven.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ApplicationDbContext dbContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendNotificationAsync(string recipientId, string message)
    {
        try
        {
            // Validate recipientId
            if (string.IsNullOrEmpty(recipientId))
            {
                _logger.LogError("RecipientId is null or empty.");
                throw new ArgumentException("RecipientId cannot be null or empty.");
            }

            if (!long.TryParse(recipientId, out var userId))
            {
                _logger.LogError("Invalid RecipientId format: {RecipientId}", recipientId);
                throw new ArgumentException("Invalid RecipientId format.");
            }

            // Create and save the notification
            _logger.LogInformation("Saving notification for RecipientId: {RecipientId}, Message: {Message}", recipientId, message);
            var notification = new Notification
            {
                RecipientId = recipientId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                UserId = userId
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Notification saved with Id: {NotificationId} for RecipientId: {RecipientId}", notification.Id, recipientId);

            // Send real-time notification via SignalR
            _logger.LogInformation("Sending SignalR notification to group: {RecipientId}", recipientId);
            await _hubContext.Clients.Group(recipientId)
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Message,
                    notification.Timestamp,
                    notification.IsRead
                });
            _logger.LogInformation("SignalR notification sent to group: {RecipientId}", recipientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to RecipientId: {RecipientId}, Message: {Message}", recipientId, message);
            throw new Exception($"Failed to send notification to {recipientId}", ex);
        }
    }
}

