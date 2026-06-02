using Microsoft.EntityFrameworkCore;
using MyMiniLMS.Data;
using MyMiniLMS.Models;
using MyMiniLMS.ViewModels;

namespace MyMiniLMS.Services;

public class StudentService
{
    private readonly ApplicationDbContext _context;

    public StudentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentDashboardVm> GetDashboardAsync(string studentId)
    {
        var courses = await _context.StudentCourses
            .Where(sc => sc.StudentId == studentId)
            .Include(sc => sc.Course)
                .ThenInclude(c => c.Teacher)
            .Include(sc => sc.Course)
                .ThenInclude(c => c.Assignments)
            .ToListAsync();

        var courseVms = new List<StudentCourseVm>();

        foreach (var studentCourse in courses)
        {
            var assignmentIds = studentCourse.Course.Assignments
                .Select(a => a.Id)
                .ToList();

            var completedCount = await _context.StudentAssignments
                .CountAsync(sa =>
                    sa.StudentId == studentId &&
                    assignmentIds.Contains(sa.AssignmentId) &&
                    sa.Status == AssignmentStatus.Completed);

            courseVms.Add(new StudentCourseVm
            {
                CourseId = studentCourse.Course.Id,
                CourseName = studentCourse.Course.Name,
                Description = studentCourse.Course.Description,
                TeacherName = studentCourse.Course.Teacher?.FullName ?? string.Empty,
                AssignmentsCount = studentCourse.Course.Assignments.Count,
                CompletedCount = completedCount
            });
        }

        return new StudentDashboardVm
        {
            Courses = courseVms
        };
    }

    public async Task<StudentCourseDetailsVm?> GetCourseAsync(
        string studentId,
        int courseId,
        AssignmentStatus? statusFilter,
        string? sort)
    {
        var studentCourse = await _context.StudentCourses
            .Include(sc => sc.Course)
            .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        if (studentCourse == null)
        {
            return null;
        }

        var query = _context.StudentAssignments
            .Where(sa =>
                sa.StudentId == studentId &&
                sa.Assignment.CourseId == courseId)
            .Include(sa => sa.Assignment)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(sa => sa.Status == statusFilter.Value);
        }

        query = sort switch
        {
            "deadline_asc" => query.OrderBy(sa => sa.Assignment.Deadline == null)
                .ThenBy(sa => sa.Assignment.Deadline),
            "deadline_desc" => query.OrderBy(sa => sa.Assignment.Deadline == null)
                .ThenByDescending(sa => sa.Assignment.Deadline),
            _ => query.OrderBy(sa => sa.Assignment.CreatedAt)
        };

        var assignments = await query
            .Select(sa => new StudentAssignmentVm
            {
                AssignmentId = sa.AssignmentId,
                Title = sa.Assignment.Title,
                Description = sa.Assignment.Description,
                Deadline = sa.Assignment.Deadline,
                Status = sa.Status,
                StartedAt = sa.StartedAt,
                CompletedAt = sa.CompletedAt
            })
            .ToListAsync();

        return new StudentCourseDetailsVm
        {
            CourseId = studentCourse.Course.Id,
            CourseName = studentCourse.Course.Name,
            Description = studentCourse.Course.Description,
            StatusFilter = statusFilter,
            Sort = sort ?? string.Empty,
            Assignments = assignments
        };
    }

    public async Task<bool> UpdateAssignmentStatusAsync(
        string studentId,
        int assignmentId,
        AssignmentStatus status)
    {
        var studentAssignment = await _context.StudentAssignments
            .Include(sa => sa.Assignment)
            .FirstOrDefaultAsync(sa =>
                sa.StudentId == studentId &&
                sa.AssignmentId == assignmentId);

        if (studentAssignment == null)
        {
            return false;
        }

        studentAssignment.Status = status;

        if (status == AssignmentStatus.NotStarted)
        {
            studentAssignment.StartedAt = null;
            studentAssignment.CompletedAt = null;
        }
        else if (status == AssignmentStatus.InProgress)
        {
            studentAssignment.StartedAt = DateTime.UtcNow;
            studentAssignment.CompletedAt = null;
        }
        else if (status == AssignmentStatus.Completed)
        {
            studentAssignment.CompletedAt = DateTime.UtcNow;
            studentAssignment.StartedAt ??= studentAssignment.CompletedAt;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int?> GetCourseIdByAssignmentAsync(string studentId, int assignmentId)
    {
        return await _context.StudentAssignments
            .Where(sa => sa.StudentId == studentId && sa.AssignmentId == assignmentId)
            .Select(sa => (int?)sa.Assignment.CourseId)
            .FirstOrDefaultAsync();
    }
}
