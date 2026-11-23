
using System.ComponentModel.DataAnnotations;

namespace MiniShop.Models;

public class ProductSupportRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string RequestNumber { get; set; } = "";

    public int OrderId { get; set; }

    public int OrderItemId { get; set; }

    public string UserId { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Укажите тему обращения")]
    [StringLength(150, MinimumLength = 3)]
    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "Опишите проблему")]
    [StringLength(2000, MinimumLength = 5)]
    public string Question { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Новое";

    public Order? Order { get; set; }

    public OrderItem? OrderItem { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();
}
