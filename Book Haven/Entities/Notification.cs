using Book_Haven.Entities;

namespace Book_Haven.Entities;

public class Notification
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty; // Maps to User.Id (as string for SignalR groups)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    public long UserId { get; set; } // Changed from int to long to match User.Id
    public User User { get; set; } // Navigation property
}