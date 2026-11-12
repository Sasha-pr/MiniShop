namespace MiniShop.Models;

public class UserNotification
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public int ProductId { get; set; }

    // Заявка на возврат, к которой относится уведомление
    public int? ReturnRequestId { get; set; }

    public string Message { get; set; } = "";

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ReturnRequest? ReturnRequest { get; set; }
}
