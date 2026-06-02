using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMiniLMS.Data;
using MyMiniLMS.Models;
using MyMiniLMS.ViewModels;

namespace MyMiniLMS.Services;

public class StudentCourseService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentCourseService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<AssignStudentsVm?> GetAssignStudentsVmAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);

        if (course == null)
        {
            return null;
        }

        var students = await _userManager.GetUsersInRoleAsync("Student");

        var assignedStudentIds = await _context.StudentCourses
            .Where(sc => sc.CourseId == courseId)
            .Select(sc => sc.StudentId)
            .ToListAsync();

        return new AssignStudentsVm
        {
            CourseId = course.Id,
            CourseName = course.Name,
            Students = students.Select(student => new StudentForAssignVm
            {
                StudentId = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                IsAssigned = assignedStudentIds.Contains(student.Id)
            })
            .Where(student => student.StudentId != course.TeacherId)
            .ToList()
        };
    }

    public async Task AssignAsync(int courseId, string studentId)
    {
        var courseTeacherId = await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => c.TeacherId)
            .FirstOrDefaultAsync();

        if (courseTeacherId == null || courseTeacherId == studentId)
        {
            return;
        }

        var exists = await _context.StudentCourses
            .AnyAsync(sc => sc.CourseId == courseId && sc.StudentId == studentId);

        if (exists)
        {
            return;
        }

        _context.StudentCourses.Add(new StudentCourse
        {
            CourseId = courseId,
            StudentId = studentId,
            AssignedAt = DateTime.UtcNow
        });

        var assignments = await _context.Assignments
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            var studentAssignmentExists = await _context.StudentAssignments
                .AnyAsync(sa =>
                    sa.StudentId == studentId &&
                    sa.AssignmentId == assignment.Id);

            if (!studentAssignmentExists)
            {
                _context.StudentAssignments.Add(new StudentAssignment
                {
                    StudentId = studentId,
                    AssignmentId = assignment.Id,
                    Status = AssignmentStatus.NotStarted
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UnassignAsync(int courseId, string studentId)
    {
        var studentCourse = await _context.StudentCourses
            .FirstOrDefaultAsync(sc => sc.CourseId == courseId && sc.StudentId == studentId);

        if (studentCourse == null)
        {
            return;
        }

        var assignmentIds = await _context.Assignments
            .Where(a => a.CourseId == courseId)
            .Select(a => a.Id)
            .ToListAsync();

        var studentAssignments = await _context.StudentAssignments
            .Where(sa =>
                sa.StudentId == studentId &&
                assignmentIds.Contains(sa.AssignmentId))
            .ToListAsync();

        _context.StudentAssignments.RemoveRange(studentAssignments);
        _context.StudentCourses.Remove(studentCourse);

        await _context.SaveChangesAsync();
    }
}
