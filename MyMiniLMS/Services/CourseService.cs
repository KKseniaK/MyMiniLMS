using Microsoft.EntityFrameworkCore;
using MyMiniLMS.Data;
using MyMiniLMS.Models;
using MyMiniLMS.ViewModels;

namespace MyMiniLMS.Services;

public class CourseService
{
    private readonly ApplicationDbContext _context;

    public CourseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> GetAllAsync()
    {
        return await _context.Courses
            .Include(c => c.Teacher)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task CreateAsync(Course course)
    {
        course.CreatedAt = DateTime.UtcNow;

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        return await _context.Courses.FindAsync(id);
    }

    public async Task UpdateAsync(Course course)
    {
        var existingCourse = await _context.Courses.FindAsync(course.Id);

        if (existingCourse == null)
        {
            return;
        }

        existingCourse.Name = course.Name;
        existingCourse.Description = course.Description;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return;
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
    }

    public async Task<Course?> GetDetailsByIdAsync(int id)
    {
        return await _context.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Course>> GetByTeacherAsync(string teacherId)
    {
        return await _context.Courses
            .Include(c => c.Teacher)
            .Where(c => c.TeacherId == teacherId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Course>> GetByStudentAsync(string studentId)
    {
        return await _context.StudentCourses
            .Where(sc => sc.StudentId == studentId)
            .Include(sc => sc.Course)
                .ThenInclude(c => c.Teacher)
            .Select(sc => sc.Course)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
    public async Task<bool> CanManageCourseAsync(int courseId, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        return await _context.Courses
            .AnyAsync(c => c.Id == courseId && c.TeacherId == userId);
    }

    public async Task<bool> CanViewCourseAsync(
        int courseId,
        string userId,
        bool isAdmin,
        bool isTeacher,
        bool isStudent)
    {
        if (isAdmin)
        {
            return true;
        }

        if (isTeacher && await CanManageCourseAsync(courseId, userId, false))
        {
            return true;
        }

        if (isStudent)
        {
            return await _context.StudentCourses
                .AnyAsync(sc => sc.CourseId == courseId && sc.StudentId == userId);
        }

        return false;
    }

    public async Task<TeacherCourseDetailsVm?> GetTeacherDetailsAsync(int courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Assignments)
                .ThenInclude(a => a.StudentAssignments)
            .Include(c => c.StudentCourses)
                .ThenInclude(sc => sc.Student)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
        {
            return null;
        }

        return new TeacherCourseDetailsVm
        {
            Course = course,
            Students = course.StudentCourses
                .Select(sc => sc.Student)
                .OrderBy(s => s.FullName)
                .ToList(),
            Progress = course.Assignments
                .OrderBy(a => a.Deadline)
                .Select(a => new AssignmentProgressVm
                {
                    Title = a.Title,
                    CompletedCount = a.StudentAssignments.Count(sa => sa.Status == AssignmentStatus.Completed),
                    InProgressCount = a.StudentAssignments.Count(sa => sa.Status == AssignmentStatus.InProgress),
                    NotStartedCount = a.StudentAssignments.Count(sa => sa.Status == AssignmentStatus.NotStarted)
                })
                .ToList()
        };
    }
}
