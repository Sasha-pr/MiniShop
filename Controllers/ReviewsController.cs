
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> Add(
        int productId,
        string? text,
        int rating)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var productExists = await _db.Products
            .AnyAsync(p => p.Id == productId);

        if (!productExists)
            return NotFound();

        if (rating < 1 || rating > 5)
        {
            TempData["ReviewError"] =
                "Выбери оценку от 1 до 5 звёзд.";

            return RedirectToAction(
                "Details", "Products", new { id = productId });
        }

        if (string.IsNullOrWhiteSpace(text) ||
            text.Trim().Length < 3 ||
            text.Trim().Length > 1000)
        {
            TempData["ReviewError"] =
                "Отзыв должен содержать от 3 до 1000 символов.";

            return RedirectToAction(
                "Details", "Products", new { id = productId });
        }

        // Проверяем, что пользователь приобрёл товар
        // и заказ имеет статус завершённого.
        var hasPurchased = await _db.Orders
    .AnyAsync(o =>
        o.UserId == user.Id &&
        o.Status == "Завершён" &&
        o.CompletedAt != null &&
        o.Items.Any(i => i.ProductId == productId));

        if (!hasPurchased)
        {
            TempData["ReviewError"] =
                "Оставить отзыв можно только после получения товара.";

            return RedirectToAction(
                "Details", "Products", new { id = productId });
        }

        // Проверяем, не оставлял ли пользователь отзыв ранее.
        var alreadyReviewed = await _db.ProductReviews
            .AnyAsync(r =>
                r.ProductId == productId &&
                r.UserId == user.Id);

        if (alreadyReviewed)
        {
            TempData["ReviewError"] =
                "Ты уже оставил отзыв на этот товар.";

            return RedirectToAction(
                "Details", "Products", new { id = productId });
        }

        var review = new ProductReview
        {
            ProductId = productId,
            UserId = user.Id,
            UserName = user.Email ?? user.UserName ?? "Покупатель",
            Rating = rating,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.ProductReviews.Add(review);

        await _db.SaveChangesAsync();

        TempData["ReviewSuccess"] = "Спасибо за отзыв!";

        return RedirectToAction(
            "Details", "Products", new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Reply(
        int reviewId,
        string? reply)
    {
        var review = await _db.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(reply) ||
            reply.Trim().Length > 1000)
        {
            TempData["ReviewError"] =
                "Ответ должен содержать от 1 до 1000 символов.";

            return RedirectToAction(
                "Details", "Products",
                new { id = review.ProductId });
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        review.ModeratorReply = reply.Trim();
        review.ModeratorName = user.Email ?? "Модератор";
        review.RepliedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["ReviewSuccess"] = "Ответ магазина сохранён.";

        return RedirectToAction(
            "Details", "Products",
            new { id = review.ProductId });
    }
}
