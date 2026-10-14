using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;
using MiniShop.Services;

namespace MiniShop.Controllers;

public class CartController : Controller
{
    private const string CartKey = "MiniShopCart";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public CartController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private bool IsLoggedIn =>
        User.Identity?.IsAuthenticated == true;

    private string UserId =>
        _userManager.GetUserId(User)!;

    private List<CartItem> GetSessionCart()
    {
        var json = HttpContext.Session.GetString(CartKey);

        return string.IsNullOrEmpty(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json)
                ?? new List<CartItem>();
    }

    private void SaveSessionCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(
            CartKey,
            JsonSerializer.Serialize(cart));
    }

    private async Task<List<CartItem>> GetCartAsync()
    {
        if (!IsLoggedIn)
            return GetSessionCart();

        return await _db.UserCartItems
            .Where(x => x.UserId == UserId)
            .Select(x => new CartItem
            {
                ProductId = x.ProductId,
                Name = x.Name,
                Price = x.Price,
                ImageUrl = x.ImageUrl,
                Quantity = x.Quantity
            })
            .ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await GetCartAsync();

        foreach (var item in cart)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product != null)
            {
                item.Name = product.Name;
                item.ImageUrl = product.ImageUrl;
                item.Price = product.DiscountedPrice;
            }
        }

        if (IsLoggedIn)
        {
            var dbItems = await _db.UserCartItems
                .Where(x => x.UserId == UserId)
                .ToListAsync();

            foreach (var dbItem in dbItems)
            {
                var product = await _db.Products
                    .FirstOrDefaultAsync(p => p.Id == dbItem.ProductId);

                if (product != null)
                {
                    dbItem.Name = product.Name;
                    dbItem.ImageUrl = product.ImageUrl;
                    dbItem.Price = product.DiscountedPrice;
                }
            }

            await _db.SaveChangesAsync();
        }
        else
        {
            SaveSessionCart(cart);
        }

        ViewBag.CartCount = cart.Sum(x => x.Quantity);
        return View(cart);
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var cart = await GetCartAsync();
        return Json(new { count = cart.Sum(x => x.Quantity) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound(new { message = "Товар не найден." });

        var cart = await GetCartAsync();
        var cartItem = cart.FirstOrDefault(x => x.ProductId == id);
        var quantityInCart = cartItem?.Quantity ?? 0;

        if (product.StockQuantity <= 0)
        {
            return BadRequest(new
            {
                message = "Товар закончился."
            });
        }

        if (quantityInCart >= product.StockQuantity)
        {
            return BadRequest(new
            {
                message = $"Доступно только {product.StockQuantity} шт."
            });
        }

        var price = product.DiscountedPrice;

        if (IsLoggedIn)
        {
            var item = await _db.UserCartItems.FirstOrDefaultAsync(
                x => x.UserId == UserId && x.ProductId == id);

            if (item == null)
            {
                _db.UserCartItems.Add(new UserCartItem
                {
                    UserId = UserId,
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            }
            else
            {
                item.Quantity++;
                item.Price = price;
                item.Name = product.Name;
                item.ImageUrl = product.ImageUrl;
            }

            await _db.SaveChangesAsync();

            var count = await _db.UserCartItems
                .Where(x => x.UserId == UserId)
                .SumAsync(x => (int?)x.Quantity) ?? 0;

            return Json(new
            {
                success = true,
                count,
                message = "Товар добавлен в корзину."
            });
        }

        var sessionCart = GetSessionCart();
        var sessionItem = sessionCart.FirstOrDefault(x => x.ProductId == id);

        if (sessionItem == null)
        {
            sessionCart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = price,
                ImageUrl = product.ImageUrl,
                Quantity = 1
            });
        }
        else
        {
            sessionItem.Quantity++;
            sessionItem.Price = price;
            sessionItem.Name = product.Name;
            sessionItem.ImageUrl = product.ImageUrl;
        }

        SaveSessionCart(sessionCart);

        return Json(new
        {
            success = true,
            count = sessionCart.Sum(x => x.Quantity),
            message = "Товар добавлен в корзину."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        [Bind("CustomerName,Phone,Address,PaymentMethod,Comment")]
        Order order)
    {
        if (!IsLoggedIn)
        {
            return RedirectToAction(
                "Login", "Account", new { returnUrl = "/Cart" });
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToAction(
                "Login", "Account", new { returnUrl = "/Cart" });
        }

        var cart = await GetCartAsync();

        if (cart.Count == 0)
        {
            TempData["CheckoutError"] = "Корзина пуста.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            TempData["CheckoutError"] =
                "Проверьте заполнение данных заказа.";
            return RedirectToAction(nameof(Index));
        }

        // Проверяем наличие и получаем актуальные цены из базы.
        foreach (var item in cart)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product == null ||
                product.StockQuantity < item.Quantity)
            {
                TempData["CheckoutError"] =
                    $"Недостаточно товара «{item.Name}» на складе.";
                return RedirectToAction(nameof(Index));
            }

            item.Name = product.Name;
            item.ImageUrl = product.ImageUrl;
            item.Price = product.DiscountedPrice;
        }

        order.Id = 0;
        order.UserId = user.Id;
        order.CustomerEmail = user.Email ?? user.UserName ?? "Не указан";
        order.CreatedAt = DateTime.UtcNow;
        order.Status = "Новый";
        order.Total = cart.Sum(x => x.Price * x.Quantity);

        order.Items = cart.Select(x => new OrderItem
        {
            ProductId = x.ProductId,
            ProductName = x.Name,
            Price = x.Price,
            Quantity = x.Quantity
        }).ToList();

        // Уменьшаем остатки в SQLite.
        foreach (var item in cart)
        {
            var product = await _db.Products
                .FirstAsync(p => p.Id == item.ProductId);

            product.StockQuantity -= item.Quantity;
        }

        _db.Orders.Add(order);

        var cartItems = await _db.UserCartItems
            .Where(x => x.UserId == user.Id)
            .ToListAsync();

        _db.UserCartItems.RemoveRange(cartItems);
        SaveSessionCart(new List<CartItem>());

        await _db.SaveChangesAsync();

        TempData["CheckoutSuccess"] =
            $"Заказ №{order.Id} успешно оформлен!";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        if (IsLoggedIn)
        {
            var items = await _db.UserCartItems
                .Where(x => x.UserId == UserId && x.ProductId == id)
                .ToListAsync();

            _db.UserCartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
        else
        {
            var cart = GetSessionCart();
            cart.RemoveAll(x => x.ProductId == id);
            SaveSessionCart(cart);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        if (IsLoggedIn)
        {
            var items = await _db.UserCartItems
                .Where(x => x.UserId == UserId)
                .ToListAsync();

            _db.UserCartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
        else
        {
            SaveSessionCart(new List<CartItem>());
        }

        return RedirectToAction(nameof(Index));
    }
}
