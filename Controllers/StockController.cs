
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

[Authorize(Roles = "Admin,Moderator")]
public class StockController : Controller
{
    private readonly ApplicationDbContext _db;

    public StockController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products
            .OrderBy(p => p.Name)
            .ToListAsync();

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int productId, int quantity)
    {
        if (quantity < 0 || quantity > 100000)
        {
            TempData["StockError"] =
                "Количество должно быть от 0 до 100000.";

            return RedirectToAction(nameof(Index));
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return NotFound();

        int oldQuantity = product.StockQuantity;

        product.StockQuantity = quantity;

        // Уведомляем только при переходе
        // от отсутствия товара к наличию.
        if (oldQuantity <= 0 && quantity > 0)
        {
            var subscribers = await _db.ProductWaitlists
                .Where(w => w.ProductId == productId)
                .Select(w => w.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in subscribers)
            {
                _db.UserNotifications.Add(new UserNotification
                {
                    UserId = userId,
                    ProductId = product.Id,
                    Message =
                        $"Товар «{product.Name}», который вы ожидали, " +
                        "снова появился в наличии! " +
                        "Вы можете перейти к товару и оформить заказ.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        if (oldQuantity <= 0 && quantity > 0)
        {
            TempData["StockSuccess"] =
                $"Товар «{product.Name}» поступил на склад. " +
                "Подписанным покупателям созданы уведомления.";
        }
        else
        {
            TempData["StockSuccess"] =
                $"Количество товара «{product.Name}» обновлено.";
        }

        return RedirectToAction(nameof(Index));
    }
}
