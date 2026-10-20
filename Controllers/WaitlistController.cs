
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

[Authorize]
public class WaitlistController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public WaitlistController(
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

        var waitlist = await (
            from w in _db.ProductWaitlists
            join p in _db.Products
                on w.ProductId equals p.Id
            where w.UserId == userId
            orderby w.CreatedAt descending
            select new WaitlistItemViewModel
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                StockQuantity = p.StockQuantity
            }
        ).ToListAsync();

        return View(waitlist);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(int productId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return NotFound();

        if (product.StockQuantity > 0)
        {
            TempData["WaitlistMessage"] =
                "Товар уже есть в наличии.";

            return RedirectToAction(
                "Details", "Products",
                new { id = productId });
        }

        var exists = await _db.ProductWaitlists
            .AnyAsync(w =>
                w.UserId == userId &&
                w.ProductId == productId);

        if (!exists)
        {
            _db.ProductWaitlists.Add(new ProductWaitlist
            {
                UserId = userId,
                ProductId = productId
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Уникальный индекс не допускает
                // повторную подписку.
                _db.ChangeTracker.Clear();

                if (!await _db.ProductWaitlists.AnyAsync(w =>
                    w.UserId == userId &&
                    w.ProductId == productId))
                {
                    throw;
                }
            }
        }

        TempData["WaitlistMessage"] =
            "Товар добавлен в лист ожидания.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unsubscribe(int productId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var item = await _db.ProductWaitlists
            .FirstOrDefaultAsync(w =>
                w.UserId == userId &&
                w.ProductId == productId);

        if (item != null)
        {
            _db.ProductWaitlists.Remove(item);
            await _db.SaveChangesAsync();
        }

        TempData["WaitlistMessage"] =
            "Товар удалён из листа ожидания.";

        return RedirectToAction(nameof(Index));
    }
}
