
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

[Authorize]
public class ProductSupportController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public ProductSupportController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Проверка: заказ завершён, а товар принадлежит этому заказу.
    private async Task<OrderItem?> GetAvailableOrderItem(
        int orderItemId,
        string userId)
    {
        return await _db.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i =>
                i.Id == orderItemId &&
                i.Order != null &&
                i.Order.UserId == userId &&
                i.Order.Status == "Завершён" &&
                i.Order.CompletedAt != null);
    }

    // ПОКУПАТЕЛЬ: список своих обращений

    [HttpGet]
    public async Task<IActionResult> MyRequests()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var requests = await _db.ProductSupportRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

        return View(requests);
    }

    // ПОКУПАТЕЛЬ: создание обращения

    [HttpGet]
    public async Task<IActionResult> Create(int orderItemId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var item = await GetAvailableOrderItem(orderItemId, userId);

        if (item == null)
            return NotFound();

        ViewBag.OrderItem = item;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int orderItemId,
        string subject,
        string question)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var item = await GetAvailableOrderItem(orderItemId, userId);

        if (item == null)
            return NotFound();

        subject = subject?.Trim() ?? "";
        question = question?.Trim() ?? "";

        ViewBag.OrderItem = item;

        if (subject.Length < 3 || subject.Length > 150)
        {
            ModelState.AddModelError(
                "subject",
                "Тема должна содержать от 3 до 150 символов.");
        }

        if (question.Length < 5 || question.Length > 2000)
        {
            ModelState.AddModelError(
                "question",
                "Описание должно содержать от 5 до 2000 символов.");
        }

        if (!ModelState.IsValid)
            return View();

        var now = DateTime.UtcNow;

        var request = new ProductSupportRequest
        {
            OrderId = item.OrderId,
            OrderItemId = item.Id,
            UserId = userId,
            UserName = User.Identity?.Name ?? "Покупатель",
            Subject = subject,
            Question = question,
            CreatedAt = now,
            UpdatedAt = now,
            Status = "Новое"
        };

        // Сначала сохраняем обращение, чтобы получить его Id.
        _db.ProductSupportRequests.Add(request);
        await _db.SaveChangesAsync();

        // Формируем уникальный номер обращения.
        request.RequestNumber =
            $"PROD-{now:yyyyMMdd}-{request.Id:D6}";

        // Первое сообщение становится началом переписки.
        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            ProductSupportRequestId = request.Id,
            SenderId = userId,
            SenderName = User.Identity?.Name ?? "Покупатель",
            Text = question,
            IsStaff = false,
            CreatedAt = now
        });

        await _db.SaveChangesAsync();

        TempData["ProductSupportSuccess"] =
            $"Обращение №{request.RequestNumber} успешно создано.";

        return RedirectToAction(
            nameof(RequestDetails),
            new { id = request.Id });
    }

    // ПОКУПАТЕЛЬ И СОТРУДНИК: просмотр обращения

    [HttpGet]
    public async Task<IActionResult> RequestDetails(int id)
    {
        var request = await _db.ProductSupportRequests
            .Include(r => r.OrderItem)
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound();

        var userId = _userManager.GetUserId(User);
        bool isStaff =
            User.IsInRole("Admin") ||
            User.IsInRole("Moderator");

        if (!isStaff && request.UserId != userId)
            return NotFound();

        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ProductSupportRequestId == id)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync();

        ViewBag.SupportRequest = request;
        ViewBag.IsStaff = isStaff;

        return View(messages);
    }

    // ПОКУПАТЕЛЬ: ответ на обращение

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(
        int requestId,
        string text)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var request = await _db.ProductSupportRequests
            .FirstOrDefaultAsync(r =>
                r.Id == requestId &&
                r.UserId == userId);

        if (request == null)
            return NotFound();

        if (request.Status == "Закрыто")
        {
            TempData["ProductSupportError"] =
                "Это обращение закрыто. Отправить новое сообщение нельзя.";

            return RedirectToAction(
                nameof(RequestDetails),
                new { id = requestId });
        }

        text = text?.Trim() ?? "";

        if (text.Length < 1 || text.Length > 2000)
        {
            TempData["ProductSupportError"] =
                "Сообщение должно содержать от 1 до 2000 символов.";

            return RedirectToAction(
                nameof(RequestDetails),
                new { id = requestId });
        }

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            ProductSupportRequestId = requestId,
            SenderId = userId,
            SenderName = User.Identity?.Name ?? "Покупатель",
            Text = text,
            IsStaff = false,
            CreatedAt = DateTime.UtcNow
        });

        request.Status = "Ожидает ответа магазина";
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(RequestDetails),
            new { id = requestId });
    }

    // СОТРУДНИК: список всех обращений

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        var requests = await _db.ProductSupportRequests
            .AsNoTracking()
            .Include(r => r.OrderItem)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

        return View(requests);
    }

    // СОТРУДНИК: ответ на обращение

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(
        int requestId,
        string text)
    {
        var request = await _db.ProductSupportRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return NotFound();

        if (request.Status == "Закрыто")
        {
            TempData["ProductSupportError"] =
                "Это обращение закрыто.";

            return RedirectToAction(
                nameof(RequestDetails),
                new { id = requestId });
        }

        text = text?.Trim() ?? "";

        if (text.Length < 1 || text.Length > 2000)
        {
            TempData["ProductSupportError"] =
                "Сообщение должно содержать от 1 до 2000 символов.";

            return RedirectToAction(
                nameof(RequestDetails),
                new { id = requestId });
        }

        var staffId = _userManager.GetUserId(User);

        if (staffId == null)
            return Challenge();

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = request.UserId,
            ProductSupportRequestId = requestId,
            SenderId = staffId,
            SenderName = User.Identity?.Name ?? "Сотрудник",
            Text = text,
            IsStaff = true,
            CreatedAt = DateTime.UtcNow
        });

        request.Status = "Ожидает ответа покупателя";
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(RequestDetails),
            new { id = requestId });
    }

    // СОТРУДНИК: закрытие обращения

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var request = await _db.ProductSupportRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound();

        request.Status = "Закрыто";
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["ProductSupportSuccess"] =
            "Обращение закрыто.";

        return RedirectToAction(
            nameof(RequestDetails),
            new { id });
    }
}
