
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

[Authorize]
public class BookmarksController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public BookmarksController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var productIds = await _db.ProductBookmarks
            .Where(b => b.UserId == userId)
            .Select(b => b.ProductId)
            .ToListAsync();

        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var productExists = await _db.Products
            .AnyAsync(p => p.Id == productId);

        if (!productExists)
            return NotFound();

        var bookmark = await _db.ProductBookmarks
            .FirstOrDefaultAsync(b =>
                b.UserId == userId &&
                b.ProductId == productId);

        if (bookmark == null)
        {
            _db.ProductBookmarks.Add(new ProductBookmark
            {
                UserId = userId,
                ProductId = productId
            });

            TempData["BookmarkMessage"] =
                "Товар добавлен в закладки.";
        }
        else
        {
            _db.ProductBookmarks.Remove(bookmark);

            TempData["BookmarkMessage"] =
                "Товар удалён из закладок.";
        }

        await _db.SaveChangesAsync();

        var previousPage = Request.Headers.Referer.ToString();

        if (Uri.TryCreate(previousPage, UriKind.Absolute, out var uri))
        {
            var localUrl = uri.PathAndQuery + uri.Fragment;

            if (string.Equals(uri.Host, Request.Host.Host,
                    StringComparison.OrdinalIgnoreCase)
                && uri.Port == (Request.Host.Port ??
                    (Request.IsHttps ? 443 : 80))
                && Url.IsLocalUrl(localUrl))
            {
                return LocalRedirect(localUrl);
            }
        }

        return RedirectToAction("Index", "Products");
    }
}
