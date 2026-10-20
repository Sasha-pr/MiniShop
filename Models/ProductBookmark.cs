namespace MiniShop.Models;

public class ProductBookmark
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public int ProductId { get; set; }
}