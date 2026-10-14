using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;
using MiniShop.Services;

namespace MiniShop.Controllers;

public class AccountController : Controller
{
    private const string CartKey = "MiniShopCart";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            await MergeGuestCartAsync(user.Id);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Products");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        ModelState.Remove(nameof(model.Password));
        ModelState.Remove(nameof(model.ConfirmPassword));

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            ModelState.Remove(nameof(model.Password));
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
                await MergeGuestCartAsync(user.Id);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Products");
        }

        ModelState.AddModelError("", "Неверный email или пароль");
        ModelState.Remove(nameof(model.Password));

        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ViewBag.Message = "Введите email.";
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            ViewBag.Message =
                "Если аккаунт существует, можно продолжить восстановление пароля.";

            return View();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { userId = user.Id, token },
            Request.Scheme);

        ViewBag.Message = "Ссылка для сброса пароля создана.";
        ViewBag.ResetLink = resetLink;

        return View();
    }

    // Открывает форму нового пароля
    [HttpGet]
    public async Task<IActionResult> ResetPassword(
        string? userId,
        string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(
                "Ссылка не содержит данных для сброса пароля.");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return Content("Пользователь не найден.");
        }

        ViewBag.UserId = userId;
        ViewBag.Token = token;

        return View("~/Views/Account/ResetPassword.cshtml");
    }
    // Сохраняет новый пароль
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        string userId,
        string token,
        string password,
        string confirmPassword)
    {
        ViewBag.UserId = userId;
        ViewBag.Token = token;

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Введи новый пароль.");
            return View("~/Views/Account/ResetPassword.cshtml");
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "Пароли не совпадают.");
            return View("~/Views/Account/ResetPassword.cshtml");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return BadRequest("Пользователь не найден.");

        var result = await _userManager.ResetPasswordAsync(
            user, token, password);

        if (result.Succeeded)
        {
            TempData["PasswordResetMessage"] =
                "Пароль успешно изменён. Теперь можно войти.";

            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View("~/Views/Account/ResetPassword.cshtml");
    }

    private async Task MergeGuestCartAsync(string userId)
    {
        var json = HttpContext.Session.GetString(CartKey);

        if (string.IsNullOrEmpty(json))
            return;

        var guestCart = JsonSerializer.Deserialize<List<CartItem>>(json)
                        ?? new List<CartItem>();

        if (guestCart.Count == 0)
            return;

        foreach (var guestItem in guestCart)
        {
            var existingItem = await _db.UserCartItems.FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     x.ProductId == guestItem.ProductId);

            if (existingItem == null)
            {
                _db.UserCartItems.Add(new UserCartItem
                {
                    UserId = userId,
                    ProductId = guestItem.ProductId,
                    Name = guestItem.Name,
                    Price = guestItem.Price,
                    ImageUrl = guestItem.ImageUrl,
                    Quantity = guestItem.Quantity
                });
            }
            else
            {
                existingItem.Quantity += guestItem.Quantity;
            }
        }

        await _db.SaveChangesAsync();
        HttpContext.Session.Remove(CartKey);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Products");
    }
}
