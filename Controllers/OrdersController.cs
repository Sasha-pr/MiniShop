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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
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
            TempData["OrderError"] = "Недопустимый статус заказа.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var order = await _db.Orders.FindAsync(id);

        if (order == null)
            return NotFound();

        order.Status = status;

        await _db.SaveChangesAsync();

        TempData["OrderSuccess"] = "Статус заказа обновлён.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        TempData["OrderSuccess"] = $"Заказ №{id} удалён.";

        return RedirectToAction(nameof(Index));
    }
}
