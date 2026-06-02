using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMiniLMS.Models;
using MyMiniLMS.Services;

namespace MyMiniLMS.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly StudentService _studentService;

    public StudentController(StudentService studentService)
    {
        _studentService = studentService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (studentId == null)
        {
            return Unauthorized();
        }

        var model = await _studentService.GetDashboardAsync(studentId);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Course(int id, AssignmentStatus? status, string? sort)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (studentId == null)
        {
            return Unauthorized();
        }

        var model = await _studentService.GetCourseAsync(studentId, id, status, sort);

        if (model == null)
        {
            return Forbid();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int assignmentId, AssignmentStatus status)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (studentId == null)
        {
            return Unauthorized();
        }

        var courseId = await _studentService.GetCourseIdByAssignmentAsync(studentId, assignmentId);

        if (courseId == null)
        {
            return Forbid();
        }

        var updated = await _studentService.UpdateAssignmentStatusAsync(studentId, assignmentId, status);

        if (!updated)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Course), new { id = courseId.Value });
    }
}
