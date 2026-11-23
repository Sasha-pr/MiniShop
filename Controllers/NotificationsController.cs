using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;

namespace MiniShop.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificationsController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var notifications = await _db.UserNotifications
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    // Возвращает количество непрочитанных уведомлений
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Unauthorized();

        var count = await _db.UserNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Json(new { count });
    }

    // Возвращает уведомления для автоматического обновления страницы
    [HttpGet]
    public async Task<IActionResult> Latest()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Unauthorized();

        var notifications = await _db.UserNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new
            {
                n.Id,
                n.Message,
                n.IsRead,
                n.ProductId,
                n.ReturnRequestId,
                n.CreatedAt
            })
            .ToListAsync();

        return Json(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var notification = await _db.UserNotifications
            .FirstOrDefaultAsync(n =>
                n.Id == id &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var notifications = await _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var notification = await _db.UserNotifications
            .FirstOrDefaultAsync(n =>
                n.Id == id &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        _db.UserNotifications.Remove(notification);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}