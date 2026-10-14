using Microsoft.EntityFrameworkCore;

namespace MiniShop.Models;

[Index(nameof(ProductId), nameof(UserId), IsUnique = true)]
public class ProductLike
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string UserId { get; set; } = "";
}
