
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

    // СТАРЫЙ ЧАТ

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var messages = await _db.ChatMessages
            .Where(m => m.UserId == userId && m.SupportRequestId == null)
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

        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > 2000)
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

    // ОБРАЩЕНИЯ ПОКУПАТЕЛЯ

    [HttpGet]
    public async Task<IActionResult> MyRequests()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var requests = await _db.SupportRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpGet]
    public IActionResult CreateRequest()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(string question)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        if (string.IsNullOrWhiteSpace(question) ||
            question.Trim().Length < 3 ||
            question.Trim().Length > 2000)
        {
            TempData["RequestError"] =
                "Вопрос должен содержать от 3 до 2000 символов.";

            return View();
        }

        var request = new SupportRequest
        {
            UserId = userId,
            UserName = User.Identity?.Name ?? "Покупатель",
            Question = question.Trim(),
            CreatedAt = DateTime.UtcNow,
            Status = "Новое"
        };

        _db.SupportRequests.Add(request);
        await _db.SaveChangesAsync();

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            SupportRequestId = request.Id,
            SenderId = userId,
            SenderName = User.Identity?.Name ?? "Покупатель",
            Text = question.Trim(),
            IsStaff = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(RequestDetails),
            new { id = request.Id });
    }

    [HttpGet]
    public async Task<IActionResult> RequestDetails(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var request = await _db.SupportRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (request == null)
            return NotFound();

        var messages = await _db.ChatMessages
            .Where(m => m.SupportRequestId == id)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync();

        ViewBag.SupportRequest = request;

        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRequest(int requestId, string text)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var request = await _db.SupportRequests
            .FirstOrDefaultAsync(r =>
                r.Id == requestId && r.UserId == userId);

        if (request == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > 2000)
        {
            TempData["ChatError"] =
                "Введите сообщение длиной до 2000 символов.";

            return RedirectToAction(
                nameof(RequestDetails),
                new { id = requestId });
        }

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            SupportRequestId = requestId,
            SenderId = userId,
            SenderName = User.Identity?.Name ?? "Покупатель",
            Text = text.Trim(),
            IsStaff = false,
            CreatedAt = DateTime.UtcNow
        });

        request.Status = "В обработке";

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(RequestDetails),
            new { id = requestId });
    }

    // ОБРАЩЕНИЯ АДМИНИСТРАТОРА И МОДЕРАТОРА

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> StaffRequests()
    {
        var requests = await _db.SupportRequests
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> StaffRequest(int id)
    {
        var request = await _db.SupportRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound();

        var messages = await _db.ChatMessages
            .Where(m => m.SupportRequestId == id)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync();

        ViewBag.SupportRequest = request;

        return View(messages);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyRequest(int requestId, string text)
    {
        var request = await _db.SupportRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > 2000)
        {
            TempData["ChatError"] =
                "Введите сообщение длиной до 2000 символов.";

            return RedirectToAction(
                nameof(StaffRequest),
                new { id = requestId });
        }

        var staffId = _userManager.GetUserId(User);

        if (staffId == null)
            return Challenge();

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = request.UserId,
            SupportRequestId = requestId,
            SenderId = staffId,
            SenderName = User.Identity?.Name ?? "Сотрудник",
            Text = text.Trim(),
            IsStaff = true,
            CreatedAt = DateTime.UtcNow
        });

        request.Status = "В обработке";

        await _db.SaveChangesAsync();

        return RedirectToAction(
            nameof(StaffRequest),
            new { id = requestId });
    }

    // ПОЛУЧЕНИЕ НОВЫХ СООБЩЕНИЙ БЕЗ ПЕРЕЗАГРУЗКИ

    [HttpGet]
    public async Task<IActionResult> RequestMessages(
        int requestId, int afterId = 0)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var ownsRequest = await _db.SupportRequests.AnyAsync(
            r => r.Id == requestId && r.UserId == userId);

        if (!ownsRequest)
            return NotFound();

        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(m =>
                m.SupportRequestId == requestId &&
                m.Id > afterId)
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

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> StaffRequestMessages(
        int requestId, int afterId = 0)
    {
        var exists = await _db.SupportRequests.AnyAsync(
            r => r.Id == requestId);

        if (!exists)
            return NotFound();

        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(m =>
                m.SupportRequestId == requestId &&
                m.Id > afterId)
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

    // СТАРЫЙ СПИСОК ПЕРЕПИСОК

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Conversations()
    {
        var conversations = await _db.ChatMessages
            .Where(m => m.SupportRequestId == null)
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
            .Where(m => m.UserId == userId && m.SupportRequestId == null)
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
    public async Task<IActionResult> Reply(string userId, string text)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        var customer = await _userManager.FindByIdAsync(userId);

        if (customer == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > 2000)
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

    [HttpGet]
    public async Task<IActionResult> MyMessages(int afterId = 0)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(m =>
                m.UserId == userId &&
                m.SupportRequestId == null &&
                m.Id > afterId)
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
            .Where(m =>
                m.UserId == userId &&
                m.SupportRequestId == null &&
                m.Id > afterId)
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

    // СЧЁТЧИК НЕОТВЕЧЕННЫХ ОБРАЩЕНИЙ

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> PendingCount()
    {
        var lastRequestMessages = await _db.ChatMessages
            .Where(m => m.SupportRequestId != null)
            .GroupBy(m => m.SupportRequestId)
            .Select(g => g.OrderByDescending(m => m.Id)
                .Select(m => new { m.IsStaff })
                .First())
            .ToListAsync();

        var count = lastRequestMessages.Count(m => !m.IsStaff);

        return Json(new { count });
    }
}

