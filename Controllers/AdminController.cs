using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MiniShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public IActionResult Index()
    {
        return View(_userManager.Users.ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string role)
    {
        if (role != "Admin" && role != "Moderator")
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var managedRoles = currentRoles
            .Where(r => r == "Admin" || r == "Moderator")
            .ToList();

        if (managedRoles.Contains(role))
            await _userManager.RemoveFromRoleAsync(user, role);
        else
            await _userManager.AddToRoleAsync(user, role);

        return RedirectToAction(nameof(Index));
    }
}
