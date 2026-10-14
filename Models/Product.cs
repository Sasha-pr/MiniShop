
namespace MiniShop.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = "";

    public string Category { get; set; } = "";

    public int StockQuantity { get; set; }

    public bool HasLowStock => StockQuantity < 3;

    public decimal DiscountedPrice =>
        HasLowStock ? Math.Round(Price * 0.7m, 2) : Price;
}
