
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

    // Список товаров, поиск, фильтры и сортировка
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? category,
        string? stock,
        string? sort)
    {
        var allProducts = _db.Products.AsNoTracking();

        var categories = await allProducts
            .Where(p => p.Category != "")
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        switch (stock)
        {
            case "available":
                query = query.Where(p => p.StockQuantity > 0);
                break;

            case "low":
                query = query.Where(p =>
                    p.StockQuantity > 0 &&
                    p.StockQuantity < 3);
                break;

            case "empty":
                query = query.Where(p => p.StockQuantity == 0);
                break;
        }

        query = sort switch
        {
            "name" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "price" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "stock" => query.OrderBy(p => p.StockQuantity),
            "stock_desc" => query.OrderByDescending(p => p.StockQuantity),
            _ => query.OrderByDescending(p => p.Id)
        };

        var products = await query.ToListAsync();

        // Общая статистика по магазину
        ViewBag.TotalProducts = await allProducts.CountAsync();
        ViewBag.AvailableProducts = await allProducts
            .CountAsync(p => p.StockQuantity > 0);
        ViewBag.LowStockProducts = await allProducts
            .CountAsync(p => p.StockQuantity > 0 && p.StockQuantity < 3);
        ViewBag.EmptyProducts = await allProducts
            .CountAsync(p => p.StockQuantity == 0);

        ViewBag.Categories = categories;
        ViewBag.Search = search;
        ViewBag.SelectedCategory = category;
        ViewBag.SelectedStock = stock;
        ViewBag.SelectedSort = sort;

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
        ValidateProduct(product);

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

        ValidateProduct(product);

        if (!ModelState.IsValid)
            return View(product);

        var existingProduct = await _db.Products.FindAsync(id);

        if (existingProduct == null)
            return NotFound();

        existingProduct.Name = product.Name.Trim();
        existingProduct.Description = product.Description?.Trim() ?? "";
        existingProduct.Price = product.Price;
        existingProduct.Category = product.Category?.Trim() ?? "";
        existingProduct.ImageUrl = product.ImageUrl?.Trim() ?? "";
        existingProduct.StockQuantity = product.StockQuantity;

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
            _db.Entry(product).State = EntityState.Unchanged;

            TempData["Error"] =
                "Не удалось удалить товар. Возможно, он связан с заказами, отзывами или другими данными.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Общая проверка данных товара
    private void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            ModelState.AddModelError(
                nameof(product.Name),
                "Введите название товара.");
        }
        else if (product.Name.Trim().Length > 150)
        {
            ModelState.AddModelError(
                nameof(product.Name),
                "Название не должно превышать 150 символов.");
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

        if (product.Description?.Length > 3000)
        {
            ModelState.AddModelError(
                nameof(product.Description),
                "Описание не должно превышать 3000 символов.");
        }

        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            bool validUrl = Uri.TryCreate(
                product.ImageUrl.Trim(),
                UriKind.Absolute,
                out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps);

            if (!validUrl)
            {
                ModelState.AddModelError(
                    nameof(product.ImageUrl),
                    "Введите корректную ссылку на изображение, начинающуюся с http:// или https://.");
            }
        }
    }
}
