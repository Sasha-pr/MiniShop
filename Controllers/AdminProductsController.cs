
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProductsController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminProductsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Список товаров
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return View(products);
    }

    // Страница добавления товара
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Product());
    }

    // Сохранение нового товара
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            ModelState.AddModelError(
                nameof(product.Name),
                "Введите название товара.");
        }

        if (product.Price <= 0)
        {
            ModelState.AddModelError(
                nameof(product.Price),
                "Цена должна быть больше нуля.");
        }

        if (product.StockQuantity < 0)
        {
            ModelState.AddModelError(
                nameof(product.StockQuantity),
                "Количество не может быть отрицательным.");
        }

        if (!ModelState.IsValid)
            return View(product);

        product.Id = 0;
        product.Name = product.Name.Trim();
        product.Description = product.Description?.Trim() ?? "";
        product.Category = product.Category?.Trim() ?? "";
        product.ImageUrl = product.ImageUrl?.Trim() ?? "";

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Товар успешно добавлен.";

        return RedirectToAction(nameof(Index));
    }

    // Страница редактирования товара
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    // Сохранение изменений товара
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            ModelState.AddModelError(
                nameof(product.Name),
                "Введите название товара.");
        }

        if (product.Price <= 0)
        {
            ModelState.AddModelError(
                nameof(product.Price),
                "Цена должна быть больше нуля.");
        }

        if (product.StockQuantity < 0)
        {
            ModelState.AddModelError(
                nameof(product.StockQuantity),
                "Количество не может быть отрицательным.");
        }

        if (!ModelState.IsValid)
            return View(product);

        var existingProduct = await _db.Products.FindAsync(id);

        if (existingProduct == null)
            return NotFound();

        existingProduct.Name = product.Name.Trim();
        existingProduct.Description =
            product.Description?.Trim() ?? "";

        existingProduct.Price = product.Price;

        existingProduct.Category =
            product.Category?.Trim() ?? "";

        existingProduct.ImageUrl =
            product.ImageUrl?.Trim() ?? "";

        existingProduct.StockQuantity =
            product.StockQuantity;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Товар успешно изменён.";

        return RedirectToAction(nameof(Index));
    }
    
    // Удаление товара
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);

        if (product == null)
        {
            TempData["Error"] = "Товар не найден.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Товар успешно удалён.";
        }
        catch (DbUpdateException)
        {
            _db.Entry(product).State =
                EntityState.Unchanged;

            TempData["Error"] =
                "Не удалось удалить товар. Возможно, он связан с заказами, отзывами или другими данными.";
        }

        return RedirectToAction(nameof(Index));
    }

}
