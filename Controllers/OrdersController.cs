
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;

namespace MiniShop.Controllers;

[Authorize(Roles = "Moderator,Admin")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;

    public OrdersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return View(order);
    }

    private async Task RestoreStock(int orderId)
    {
        var items = await _db.OrderItems
            .Where(i => i.OrderId == orderId)
            .ToListAsync();

        foreach (var item in items)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product != null)
            {
                product.StockQuantity += item.Quantity;
            }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id, string status)
    {
        string[] allowedStatuses =
        {
            "Новый",
            "В обработке",
            "Отправлен",
            "Завершён",
            "Отменён"
        };

        if (!allowedStatuses.Contains(status))
        {
            TempData["OrderError"] =
                "Недопустимый статус заказа.";

            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        if (order.Status == "Отменён" && status != "Отменён")
        {
            TempData["OrderError"] =
                "Отменённый заказ нельзя возобновить.";

            return RedirectToAction(nameof(Details), new { id });
        }

        if (status == "Отменён" && order.Status != "Отменён")
        {
            await RestoreStock(id);
        }

        order.Status = status;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["OrderSuccess"] =
            "Статус заказа обновлён.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        if (order.Status != "Отменён")
        {
            await RestoreStock(id);
        }

        _db.OrderItems.RemoveRange(order.Items);
        _db.Orders.Remove(order);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["OrderSuccess"] =
            $"Заказ №{id} удалён. Остатки обработаны.";

        return RedirectToAction(nameof(Index));
    }
}
