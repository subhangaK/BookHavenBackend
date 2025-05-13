namespace Book_Haven.Dtos;

public class NotificationDto
{
    public long Id { get; set; }
    public string RecipientId { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}