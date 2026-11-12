using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

[Authorize]
public class ReturnRequestsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    private const int ReturnPeriodDays = 14;

    public ReturnRequestsController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Заявки текущего покупателя
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var requests = await _db.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.OrderItem)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    // Подробная информация о конкретной заявке
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _db.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound();

        var isStaff =
            User.IsInRole("Admin") ||
            User.IsInRole("Moderator");

        var userId = _userManager.GetUserId(User);

        if (!isStaff && request.UserId != userId)
            return Forbid();

        return View(request);
    }

    // Создание заявки
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int orderItemId, string reason)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Trim().Length < 5 ||
            reason.Trim().Length > 1000)
        {
            TempData["ReturnError"] =
                "Укажите причину возврата от 5 до 1000 символов.";

            return RedirectToAction("Index", "MyOrders");
        }

        var item = await _db.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == orderItemId);

        if (item == null || item.Order == null ||
            item.Order.UserId != userId)
        {
            return NotFound();
        }

        var order = item.Order;

        if (order.Status != "Завершён" || order.CompletedAt == null)
        {
            TempData["ReturnError"] =
                "Возврат можно запросить только для завершённого заказа.";

            return RedirectToAction(
                "Details", "MyOrders", new { id = order.Id });
        }

        var deadline = order.CompletedAt.Value.AddDays(ReturnPeriodDays);

        if (DateTime.UtcNow > deadline)
        {
            TempData["ReturnError"] =
                "Срок подачи заявки на возврат истёк.";

            return RedirectToAction(
                "Details", "MyOrders", new { id = order.Id });
        }

        var hasActiveRequest = await _db.ReturnRequests
            .AnyAsync(r =>
                r.OrderItemId == orderItemId &&
                r.UserId == userId &&
                r.Status != "Отклонена");

        if (hasActiveRequest)
        {
            TempData["ReturnError"] =
                "По этому товару уже существует активная заявка.";

            return RedirectToAction(
                "Details", "MyOrders", new { id = order.Id });
        }

        var request = new ReturnRequest
        {
            RequestNumber = "TEMP",
            OrderId = order.Id,
            OrderItemId = item.Id,
            UserId = userId!,
            Reason = reason.Trim(),
            CreatedAt = DateTime.UtcNow,
            Status = "Новая"
        };

        _db.ReturnRequests.Add(request);
        await _db.SaveChangesAsync();

        request.RequestNumber =
            $"RET-{DateTime.UtcNow:yyyyMMdd}-{request.Id:D5}";

        await _db.SaveChangesAsync();

        TempData["ReturnSuccess"] =
            $"Заявка {request.RequestNumber} создана.";

        return RedirectToAction("Index", "ReturnRequests");
    }

    // Управление заявками для сотрудников
    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        var requests = await _db.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.OrderItem)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    // Количество новых заявок для счётчика в меню
    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> PendingCount()
    {
        var count = await _db.ReturnRequests
            .CountAsync(r => r.Status == "Новая");

        return Json(new { count });
    }

    // Обработка заявки сотрудником
    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(
        int id,
        string status,
        string? adminComment)
    {
        if (status != "Одобрена" && status != "Отклонена")
            return BadRequest();

        var request = await _db.ReturnRequests
            .Include(r => r.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound();

        if (request.Status == "Одобрена" ||
            request.Status == "Отклонена")
        {
            TempData["ReturnError"] =
                "Эта заявка уже обработана.";

            return RedirectToAction(nameof(Manage));
        }

        request.Status = status;

        request.AdminComment =
            string.IsNullOrWhiteSpace(adminComment)
                ? null
                : adminComment.Trim();

        request.ReviewedAt = DateTime.UtcNow;

        var productId = request.OrderItem?.ProductId ?? 0;

        var message = status == "Одобрена"
            ? $"Ваша заявка {request.RequestNumber} на возврат одобрена."
            : $"Ваша заявка {request.RequestNumber} на возврат отклонена.";

        if (!string.IsNullOrWhiteSpace(request.AdminComment))
        {
            message += $" Комментарий сотрудника: {request.AdminComment}";
        }

        var notification = new UserNotification
        {
            UserId = request.UserId,
            ProductId = productId,
            ReturnRequestId = request.Id,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserNotifications.Add(notification);

        await _db.SaveChangesAsync();

        TempData["ReturnSuccess"] =
            $"Заявка {request.RequestNumber} обработана.";

        return RedirectToAction(nameof(Manage));
    }
}
