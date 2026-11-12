
using System.ComponentModel.DataAnnotations;

namespace MiniShop.Models;

public class ReturnRequest
{
    public int Id { get; set; }

    [Required]
    public string RequestNumber { get; set; } = "";

    public int OrderId { get; set; }

    public int OrderItemId { get; set; }

    public string UserId { get; set; } = "";

    [Required(ErrorMessage = "Укажите причину возврата")]
    [StringLength(1000, MinimumLength = 5,
        ErrorMessage = "Причина должна содержать от 5 до 1000 символов")]
    public string Reason { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Новая";

    [StringLength(1000)]
    public string? AdminComment { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Order? Order { get; set; }

    public OrderItem? OrderItem { get; set; }
}
