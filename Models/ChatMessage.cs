
namespace MiniShop.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public string SenderId { get; set; } = "";

    public string SenderName { get; set; } = "";

    public string Text { get; set; } = "";

    public bool IsStaff { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Старое обращение в поддержку
    public int? SupportRequestId { get; set; }

    public SupportRequest? SupportRequest { get; set; }

    // Новое обращение по конкретному товару
    public int? ProductSupportRequestId { get; set; }

    public ProductSupportRequest? ProductSupportRequest { get; set; }
}
