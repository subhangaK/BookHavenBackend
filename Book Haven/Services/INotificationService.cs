namespace Book_Haven.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string recipientId, string message);
}