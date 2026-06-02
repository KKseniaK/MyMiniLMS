using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMiniLMS.Models;
using MyMiniLMS.Services;
using System.Security.Claims;

namespace MyMiniLMS.Controllers;

[Authorize]
public class CourseController : Controller
{
    private readonly CourseService _courseService;

    public CourseController(CourseService courseService)
    {
        _courseService = courseService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (User.IsInRole("Admin"))
        {
            var courses = await _courseService.GetAllAsync();
            return View(courses);
        }

        if (User.IsInRole("Teacher") && userId != null)
        {
            var courses = await _courseService.GetByTeacherAsync(userId);
            return View(courses);
        }

        if (User.IsInRole("Student") && userId != null)
        {
            var courses = await _courseService.GetByStudentAsync(userId);
            return View(courses);
        }

        return View(new List<Course>());
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> Teaching()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        ViewBag.PageTitleKey = "Teaching";

        var courses = await _courseService.GetByTeacherAsync(userId);
        return View(nameof(Index), courses);
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course)
    {
        if (!ModelState.IsValid)
        {
            return View(course);
        }

        course.TeacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _courseService.CreateAsync(course);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null ||
            !await _courseService.CanManageCourseAsync(id, userId, User.IsInRole("Admin")))
        {
            return Forbid();
        }

        var course = await _courseService.GetByIdAsync(id);

        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Course course)
    {
        if (!ModelState.IsValid)
        {
            return View(course);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null ||
            !await _courseService.CanManageCourseAsync(course.Id, userId, User.IsInRole("Admin")))
        {
            return Forbid();
        }

        await _courseService.UpdateAsync(course);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null ||
            !await _courseService.CanManageCourseAsync(id, userId, User.IsInRole("Admin")))
        {
            return Forbid();
        }

        var course = await _courseService.GetByIdAsync(id);

        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null ||
            !await _courseService.CanManageCourseAsync(id, userId, User.IsInRole("Admin")))
        {
            return Forbid();
        }

        await _courseService.DeleteAsync(id);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        if (User.IsInRole("Student") && !User.IsInRole("Teacher") && !User.IsInRole("Admin"))
        {
            return RedirectToAction("Course", "Student", new { id });
        }

        if (!await _courseService.CanViewCourseAsync(
                id,
                userId,
                User.IsInRole("Admin"),
                User.IsInRole("Teacher"),
                User.IsInRole("Student")))
        {
            return Forbid();
        }

        var model = await _courseService.GetTeacherDetailsAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }
}
