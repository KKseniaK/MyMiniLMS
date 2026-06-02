using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMiniLMS.Services;

namespace MyMiniLMS.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly CourseService _courseService;
    private readonly ReportService _reportService;

    public ReportsController(
        CourseService courseService,
        ReportService reportService)
    {
        _courseService = courseService;
        _reportService = reportService;
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StudentCourseXlsx(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (studentId == null)
        {
            return Unauthorized();
        }

        var report = await _reportService.BuildStudentCourseXlsxAsync(studentId, courseId);

        if (report == null)
        {
            return Forbid();
        }

        return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "student-course-report.xlsx");
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StudentCourseDocx(int courseId)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (studentId == null)
        {
            return Unauthorized();
        }

        var report = await _reportService.BuildStudentCourseDocxAsync(studentId, courseId);

        if (report == null)
        {
            return Forbid();
        }

        return File(report, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "student-course-report.docx");
    }

    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> OverdueXlsx(int? courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null || !await CanUseOverdueReportAsync(courseId, userId))
        {
            return Forbid();
        }

        var report = await _reportService.BuildOverdueXlsxAsync(courseId, userId, User.IsInRole("Admin"));

        return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "overdue-report.xlsx");
    }

    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> OverdueDocx(int? courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null || !await CanUseOverdueReportAsync(courseId, userId))
        {
            return Forbid();
        }

        var report = await _reportService.BuildOverdueDocxAsync(courseId, userId, User.IsInRole("Admin"));

        return File(report, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "overdue-report.docx");
    }

    private async Task<bool> CanUseOverdueReportAsync(int? courseId, string userId)
    {
        if (!courseId.HasValue)
        {
            return User.IsInRole("Admin") || User.IsInRole("Teacher");
        }

        return await _courseService.CanManageCourseAsync(courseId.Value, userId, User.IsInRole("Admin"));
    }
}
