
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MiniShop.Models;

[Index(nameof(ProductId), nameof(UserId), IsUnique = true)]
public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5 звёзд")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Напиши текст отзыва")]
    [StringLength(
        1000,
        MinimumLength = 3,
        ErrorMessage = "Отзыв должен содержать от 3 до 1000 символов")]
    public string Text { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ModeratorReply { get; set; }

    public string? ModeratorName { get; set; }

    public DateTime? RepliedAt { get; set; }
}
