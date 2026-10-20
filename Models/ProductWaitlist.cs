using Microsoft.EntityFrameworkCore;
namespace MiniShop.Models;
[Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
public class ProductWaitlist
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public int ProductId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
