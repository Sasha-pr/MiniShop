
namespace MiniShop.Models;

public class UserCartItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public int ProductId { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = "";

    public int Quantity { get; set; }
}