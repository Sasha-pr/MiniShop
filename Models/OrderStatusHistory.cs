using System.ComponentModel.DataAnnotations;

namespace MiniShop.Models;

public class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string OldStatus { get; set; } = "";

    [Required]
    public string NewStatus { get; set; } = "";

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string ChangedByUserId { get; set; } = "";

    public Order? Order { get; set; }
}