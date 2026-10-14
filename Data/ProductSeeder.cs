using Microsoft.EntityFrameworkCore;
using MiniShop.Models;

namespace MiniShop.Data;

public static class ProductSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Products.AnyAsync())
            return;

        db.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Наушники",
                Description = "Удобные наушники для музыки и учёбы.",
                Price = 2490m,
                ImageUrl = "https://caseguru.ru/upload/resize_cache/iblock/276/y1tzvictz34ciujo17mmrbvai6z0n6ad/1200_1200_1/luchshie_naushniki_apple_top_5_modeley_v_2025_godu.jpg",
                Category = "Электроника",
                StockQuantity = 10
            },
            new Product
            {
                Id = 2,
                Name = "Рюкзак",
                Description = "Минималистичный рюкзак на каждый день.",
                Price = 3190m,
                ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSsSo_zNmX79K9i8Gnm_rUjgc4ToTFyhCpoVXy0TSDy-p2W-MIddhC76mL8&s=10",
                Category = "Аксессуары",
                StockQuantity = 2
            },
            new Product
            {
                Id = 3,
                Name = "Часы",
                Description = "Лаконичные часы с классическим дизайном.",
                Price = 4590m,
                ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTR2xeDMcNJN-DNpjaTJj311gtkT5DZB7KMfaofR8naPn2N55OaaPZiL2yv&s=10",
                Category = "Аксессуары",
                StockQuantity = 5
            },
            new Product
            {
                Id = 4,
                Name = "Кружка",
                Description = "Керамическая кружка для любимых напитков.",
                Price = 690m,
                ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTwHSo4DOn46-OPBXIQoS-EVxOWJ5S96p7dYgMFMH4-Tg&s=10",
                Category = "Дом",
                StockQuantity = 1
            }
        );

        await db.SaveChangesAsync();
    }
}
