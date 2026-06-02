using MyMiniLMS.Data;
using MyMiniLMS.Models;
using Microsoft.EntityFrameworkCore;

namespace MyMiniLMS.Services;

public class AssignmentService
{
    private readonly ApplicationDbContext _context;

    public AssignmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Assignment assignment)
    {
        assignment.CreatedAt = DateTime.UtcNow;

        if (assignment.Deadline.HasValue)
        {
            assignment.Deadline = DateTime.SpecifyKind(
                assignment.Deadline.Value,
                DateTimeKind.Utc
            );
        }

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        var assignedStudents = await _context.StudentCourses
            .Where(sc => sc.CourseId == assignment.CourseId)
            .Select(sc => sc.StudentId)
            .ToListAsync();

        foreach (var studentId in assignedStudents)
        {
            var exists = await _context.StudentAssignments
                .AnyAsync(sa =>
                    sa.StudentId == studentId &&
                    sa.AssignmentId == assignment.Id);

            if (!exists)
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

    public async Task<Assignment?> GetByIdAsync(int id)
    {
        return await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task UpdateAsync(Assignment assignment)
    {
        assignment.CreatedAt = DateTime.SpecifyKind(assignment.CreatedAt, DateTimeKind.Utc);

        if (assignment.Deadline.HasValue)
        {
            assignment.Deadline = DateTime.SpecifyKind(
                assignment.Deadline.Value,
                DateTimeKind.Utc
            );
        }

        _context.Assignments.Update(assignment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var assignment = await _context.Assignments.FindAsync(id);

        if (assignment == null)
        {
            return;
        }

        _context.Assignments.Remove(assignment);
        await _context.SaveChangesAsync();
    }
}