using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMiniLMS.Services;

namespace MyMiniLMS.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class CourseStudentsController : Controller
{
    private readonly StudentCourseService _studentCourseService;
    private readonly CourseService _courseService;

    public CourseStudentsController(
        StudentCourseService studentCourseService,
        CourseService courseService)
    {
        _studentCourseService = studentCourseService;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> Manage(int courseId)
    {
        if (!await CanManageCourseAsync(courseId))
        {
            return Forbid();
        }

        var model = await _studentCourseService.GetAssignStudentsVmAsync(courseId);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int courseId, string studentId)
    {
        if (!await CanManageCourseAsync(courseId))
        {
            return Forbid();
        }

        await _studentCourseService.AssignAsync(courseId, studentId);

        return RedirectToAction(nameof(Manage), new { courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int courseId, string studentId)
    {
        if (!await CanManageCourseAsync(courseId))
        {
            return Forbid();
        }

        await _studentCourseService.UnassignAsync(courseId, studentId);

        return RedirectToAction(nameof(Manage), new { courseId });
    }

    private async Task<bool> CanManageCourseAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return userId != null &&
            await _courseService.CanManageCourseAsync(courseId, userId, User.IsInRole("Admin"));
    }
}
