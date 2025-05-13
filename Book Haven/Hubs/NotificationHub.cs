using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Book_Haven.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Use ClaimTypes.NameIdentifier instead of "sub" to match ASP.NET Core Identity
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"User {userId} connected and added to group {userId}, ConnectionId: {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine("No userId found in claims for SignalR connection.");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"User {userId} disconnected from group {userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"Connection {Context.ConnectionId} subscribed to group {groupName}");
    }

    // Method to send a notification to a specific user
    public async Task SendNotification(string message, string recipientId)
    {
        await Clients.Group(recipientId).SendAsync("ReceiveNotification", new
        {
            Message = message,
            Timestamp = DateTime.UtcNow,
            IsRead = false
        });
        Console.WriteLine($"Notification sent to group {recipientId}: {message}");
    }
}