using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public ProductsController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? category)
    {
        var query = _db.Products.AsQueryable();

        var categories = await _db.Products
            .Where(p => p.Category != null && p.Category != "")
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var products = await query
            .OrderBy(p => p.Id)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category;

        return View(products);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        ViewBag.Reviews = await _db.ProductReviews
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.LikeCount = await _db.ProductLikes
            .CountAsync(x => x.ProductId == id);

        ViewBag.HasLiked = false;

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                ViewBag.HasLiked = await _db.ProductLikes.AnyAsync(
                    x => x.ProductId == id && x.UserId == user.Id);
            }
        }

        return View(product);
    }
}
