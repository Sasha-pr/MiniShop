using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

public class LikesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public LikesController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        var productExists = await _db.Products
            .AnyAsync(p => p.Id == productId);

        if (!productExists)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        var like = await _db.ProductLikes.FirstOrDefaultAsync(
            x => x.ProductId == productId && x.UserId == user.Id);

        bool liked;

        if (like == null)
        {
            _db.ProductLikes.Add(new ProductLike
            {
                ProductId = productId,
                UserId = user.Id
            });

            liked = true;
        }
        else
        {
            _db.ProductLikes.Remove(like);
            liked = false;
        }

        await _db.SaveChangesAsync();

        var count = await _db.ProductLikes.CountAsync(
            x => x.ProductId == productId);

        return Json(new { success = true, liked, count });
    }
}
