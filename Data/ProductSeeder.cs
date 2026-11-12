
using Microsoft.EntityFrameworkCore;
using MiniShop.Models;

namespace MiniShop.Data;

public static class ProductSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var products = new List<Product>
        {
            new Product
            {
                Name = "Беспроводная мышь",
                Description = "Удобная беспроводная мышь для работы и учёбы.",
                Price = 1490m,
                ImageUrl = "https://images.unsplash.com/photo-1527814050087-3793815479db?w=600",
                Category = "Электроника",
                StockQuantity = 12
            },
            new Product
            {
                Name = "Клавиатура",
                Description = "Компактная клавиатура для компьютера.",
                Price = 2290m,
                ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=600",
                Category = "Электроника",
                StockQuantity = 8
            },
            new Product
            {
                Name = "Портативная колонка",
                Description = "Небольшая колонка для прослушивания музыки.",
                Price = 2790m,
                ImageUrl = "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=600",
                Category = "Электроника",
                StockQuantity = 6
            },
            new Product
            {
                Name = "Солнцезащитные очки",
                Description = "Стильные очки для повседневного использования.",
                Price = 1790m,
                ImageUrl = "https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=600",
                Category = "Аксессуары",
                StockQuantity = 9
            },
            new Product
            {
                Name = "Наручные часы Sport",
                Description = "Практичные часы для повседневной жизни.",
                Price = 3590m,
                ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600",
                Category = "Аксессуары",
                StockQuantity = 4
            },
            new Product
            {
                Name = "Настольная лампа",
                Description = "Лампа для комфортной работы и чтения.",
                Price = 1990m,
                ImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=600",
                Category = "Дом",
                StockQuantity = 7
            },
            new Product
            {
                Name = "Блокнот",
                Description = "Блокнот для записей, планов и заметок.",
                Price = 390m,
                ImageUrl = "https://images.unsplash.com/photo-1531346878377-a5be20888e57?w=600",
                Category = "Канцелярия",
                StockQuantity = 20
            },
            new Product
            {
                Name = "Настольный органайзер",
                Description = "Органайзер для хранения канцелярских принадлежностей.",
                Price = 890m,
                ImageUrl = "https://images.unsplash.com/photo-1494438639946-1ebd1d20bf85?w=600",
                Category = "Канцелярия",
                StockQuantity = 10
            }
        };

        foreach (var product in products)
        {
            bool exists = await db.Products
                .AnyAsync(p => p.Name == product.Name);

            if (!exists)
            {
                db.Products.Add(product);
            }
        }

        await db.SaveChangesAsync();
    }
}
