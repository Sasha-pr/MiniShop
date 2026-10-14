using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public ReviewsController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string text)
    {
        if (!User.Identity!.IsAuthenticated)
            return Challenge();

        if (string.IsNullOrWhiteSpace(text) ||
            text.Trim().Length < 3 ||
            text.Trim().Length > 1000)
        {
            TempData["ReviewError"] =
                "Отзыв должен содержать от 3 до 1000 символов.";

            return RedirectToAction(
                "Details", "Products", new { id = productId });
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = productId,
            UserId = user.Id,
            UserName = user.Email ?? user.UserName ?? "Пользователь",
            Text = text.Trim()
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(
            "Details", "Products", new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Reply(int reviewId, string reply)
    {
        if (string.IsNullOrWhiteSpace(reply) ||
            reply.Trim().Length > 1000)
        {
            return BadRequest(
                "Ответ должен содержать от 1 до 1000 символов.");
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var review = await _db.ProductReviews.FindAsync(reviewId);

        if (review == null)
            return NotFound();

        review.ModeratorReply = reply.Trim();
        review.ModeratorName = user.Email ?? "Модератор";
        review.RepliedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return RedirectToAction(
            "Details", "Products", new { id = review.ProductId });
    }
}