using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Models;
using MiniShop.Services;

namespace MiniShop.Controllers;

public class CartController : Controller
{
    private const string CartKey = "MiniShopCart";

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartKey);
        return string.IsNullOrEmpty(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart) =>
        HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));

    [HttpGet]
    public IActionResult Index()
    {
        var cart = GetCart();
        ViewBag.CartCount = cart.Sum(x => x.Quantity);
        return View(cart);
    }

    [HttpGet]
    public IActionResult Count()
    {
        return Json(new { count = GetCart().Sum(x => x.Quantity) });
    }

    [HttpPost]
    public IActionResult Add(int id)
    {
        var product = ProductStore.GetById(id);
        if (product is null)
            return NotFound(new { message = "Товар не найден." });

        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.ProductId == id);

        if (item is null)
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                Quantity = 1
            });
        }
        else
        {
            item.Quantity++;
        }

        SaveCart(cart);
        return Json(new
        {
            success = true,
            count = cart.Sum(x => x.Quantity),
            message = "Товар добавлен в корзину."
        });
    }

    [HttpPost]
    public IActionResult Remove(int id)
    {
        var cart = GetCart();
        cart.RemoveAll(x => x.ProductId == id);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Clear()
    {
        SaveCart(new List<CartItem>());
        return RedirectToAction(nameof(Index));
    }
}
