using Microsoft.AspNetCore.Mvc;
using MiniShop.Services;

namespace MiniShop.Controllers;

public class ProductsController : Controller
{
    public IActionResult Index() => View(ProductStore.Products);

    public IActionResult Details(int id)
    {
        var product = ProductStore.GetById(id);
        return product is null ? NotFound() : View(product);
    }
}
