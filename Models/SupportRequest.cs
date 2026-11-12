
using System.ComponentModel.DataAnnotations;

namespace MiniShop.Models;

public class SupportRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string UserName { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string Question { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Новое";

    public List<ChatMessage> Messages { get; set; } = new();
}
