using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMiniLMS.Services;

namespace MyMiniLMS.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly CalendarService _calendarService;

    public CalendarController(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var model = await _calendarService.GetCalendarAsync(
            userId,
            User.IsInRole("Admin"),
            User.IsInRole("Teacher"),
            User.IsInRole("Student"),
            year,
            month);

        return View(model);
    }
}
