using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyMiniLMS.Models;
using MyMiniLMS.ViewModels;

namespace MyMiniLMS.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();

        var model = new List<AdminUserVm>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            model.Add(new AdminUserVm
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles.ToList()
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeTeacher(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToAction(nameof(Users));
        }

        if (!await _userManager.IsInRoleAsync(user, "Teacher"))
        {
            await _userManager.AddToRoleAsync(user, "Teacher");
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeacher(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToAction(nameof(Users));
        }

        if (await _userManager.IsInRoleAsync(user, "Teacher"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Teacher");
        }

        if (!await _userManager.IsInRoleAsync(user, "Student"))
        {
            await _userManager.AddToRoleAsync(user, "Student");
        }

        return RedirectToAction(nameof(Users));
    }
}
