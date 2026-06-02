using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMiniLMS.Models;
using MyMiniLMS.Services;

namespace MyMiniLMS.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class AssignmentController : Controller
{
    private readonly AssignmentService _assignmentService;
    private readonly CourseService _courseService;

    public AssignmentController(
        AssignmentService assignmentService,
        CourseService courseService)
    {
        _assignmentService = assignmentService;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int courseId)
    {
        var course = await _courseService.GetByIdAsync(courseId);

        if (course == null)
        {
            return NotFound();
        }

        if (!await CanManageCourseAsync(courseId))
        {
            return Forbid();
        }

        var assignment = new Assignment
        {
            CourseId = courseId
        };

        ViewBag.CourseName = course.Name;

        return View(assignment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Assignment assignment)
    {
        if (!ModelState.IsValid)
        {
            await SetCourseNameAsync(assignment.CourseId);
            return View(assignment);
        }

        if (!await CanManageCourseAsync(assignment.CourseId))
        {
            return Forbid();
        }

        await _assignmentService.CreateAsync(assignment);

        return RedirectToAction("Details", "Course", new { id = assignment.CourseId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var assignment = await _assignmentService.GetByIdAsync(id);

        if (assignment == null)
        {
            return NotFound();
        }

        if (!await CanManageCourseAsync(assignment.CourseId))
        {
            return Forbid();
        }

        return View(assignment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Assignment assignment)
    {
        if (!ModelState.IsValid)
        {
            await SetCourseNameAsync(assignment.CourseId);
            return View(assignment);
        }

        if (!await CanManageCourseAsync(assignment.CourseId))
        {
            return Forbid();
        }

        await _assignmentService.UpdateAsync(assignment);

        return RedirectToAction("Details", "Course", new { id = assignment.CourseId });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await _assignmentService.GetByIdAsync(id);

        if (assignment == null)
        {
            return NotFound();
        }

        if (!await CanManageCourseAsync(assignment.CourseId))
        {
            return Forbid();
        }

        return View(assignment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var assignment = await _assignmentService.GetByIdAsync(id);

        if (assignment == null)
        {
            return NotFound();
        }

        if (!await CanManageCourseAsync(assignment.CourseId))
        {
            return Forbid();
        }

        var courseId = assignment.CourseId;

        await _assignmentService.DeleteAsync(id);

        return RedirectToAction("Details", "Course", new { id = courseId });
    }

    private async Task<bool> CanManageCourseAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return userId != null &&
            await _courseService.CanManageCourseAsync(courseId, userId, User.IsInRole("Admin"));
    }

    private async Task SetCourseNameAsync(int courseId)
    {
        var course = await _courseService.GetByIdAsync(courseId);
        ViewBag.CourseName = course?.Name;
    }
}
