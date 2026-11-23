
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IActionResult> Index(string? search)
    {
        var usersQuery = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            usersQuery = usersQuery.Where(u =>
                u.Email != null && u.Email.Contains(search));
        }

        var users = await usersQuery
            .OrderBy(u => u.Email)
            .ToListAsync();

        ViewBag.Search = search;

        return View(users);
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

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId && role == "Admin")
        {
            TempData["AdminError"] =
                "Нельзя изменить собственную роль администратора.";

            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        IdentityResult result;

        if (currentRoles.Contains(role))
        {
            result = await _userManager.RemoveFromRoleAsync(user, role);
        }
        else
        {
            result = await _userManager.AddToRoleAsync(user, role);
        }

        if (result.Succeeded)
        {
            TempData["AdminSuccess"] = "Роли пользователя обновлены.";
        }
        else
        {
            TempData["AdminError"] = string.Join(
                " ",
                result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["AdminError"] =
                "Нельзя заблокировать собственный аккаунт.";

            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.SetLockoutEnabledAsync(user, true);

        if (result.Succeeded)
        {
            result = await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.MaxValue);
        }

        if (result.Succeeded)
        {
            TempData["AdminSuccess"] = "Пользователь заблокирован.";
        }
        else
        {
            TempData["AdminError"] = string.Join(
                " ",
                result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        var result = await _userManager.SetLockoutEndDateAsync(
            user,
            null);

        if (result.Succeeded)
        {
            TempData["AdminSuccess"] = "Пользователь разблокирован.";
        }
        else
        {
            TempData["AdminError"] = string.Join(
                " ",
                result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }
}
