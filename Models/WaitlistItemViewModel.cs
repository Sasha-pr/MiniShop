
namespace MiniShop.Models;

public class WaitlistItemViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
}
