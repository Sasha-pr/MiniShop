
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public ChatController(
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

        var messages = await _db.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync();

        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string text)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        if (string.IsNullOrWhiteSpace(text) ||
            text.Trim().Length > 2000)
        {
            TempData["ChatError"] =
                "Введите сообщение длиной до 2000 символов.";

            return RedirectToAction(nameof(Index));
        }

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            SenderId = userId,
            SenderName = User.Identity?.Name ?? "Покупатель",
            Text = text.Trim(),
            IsStaff = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Conversations()
    {
        var conversations = await _db.ChatMessages
            .GroupBy(m => m.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                LastMessage = g.Max(m => m.CreatedAt),
                MessageCount = g.Count()
            })
            .OrderByDescending(c => c.LastMessage)
            .ToListAsync();

        var users = new List<ChatConversationViewModel>();

        foreach (var conversation in conversations)
        {
            var user = await _userManager.FindByIdAsync(
                conversation.UserId);

            users.Add(new ChatConversationViewModel
            {
                UserId = conversation.UserId,
                UserName = user?.UserName ?? "Покупатель",
                MessageCount = conversation.MessageCount,
                LastMessage = conversation.LastMessage
            });
        }

        return View(users);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Conversation(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        var customer = await _userManager.FindByIdAsync(userId);

        if (customer == null)
            return NotFound();

        var messages = await _db.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync();

        ViewBag.CustomerId = userId;
        ViewBag.CustomerName = customer.UserName;

        return View(messages);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(
        string userId, string text)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        var customer = await _userManager.FindByIdAsync(userId);

        if (customer == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(text) ||
            text.Trim().Length > 2000)
        {
            TempData["ChatError"] =
                "Введите сообщение длиной до 2000 символов.";

            return RedirectToAction(
                nameof(Conversation),
                new { userId });
        }

        var staffId = _userManager.GetUserId(User);

        if (staffId == null)
            return Challenge();

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            SenderId = staffId,
            SenderName = User.Identity?.Name ?? "Сотрудник",
            Text = text.Trim(),
            IsStaff = true,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(Conversation),
            new { userId });
    }

    // Получение новых сообщений покупателем
    [HttpGet]
    public async Task<IActionResult> MyMessages(int afterId = 0)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Id > afterId)
            .OrderBy(m => m.Id)
            .Select(m => new
            {
                m.Id,
                m.Text,
                m.IsStaff,
                m.CreatedAt
            })
            .ToListAsync();

        return Json(messages);
    }

    // Получение новых сообщений сотрудником
    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> CustomerMessages(
        string userId, int afterId = 0)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        var customer = await _userManager.FindByIdAsync(userId);

        if (customer == null)
            return NotFound();

        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Id > afterId)
            .OrderBy(m => m.Id)
            .Select(m => new
            {
                m.Id,
                m.Text,
                m.IsStaff,
                m.CreatedAt
            })
            .ToListAsync();

        return Json(messages);
    }
}
