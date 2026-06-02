using Microsoft.EntityFrameworkCore;
using MyMiniLMS.Data;
using MyMiniLMS.ViewModels;

namespace MyMiniLMS.Services;

public class CalendarService
{
    private readonly ApplicationDbContext _context;

    public CalendarService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CalendarVm> GetCalendarAsync(
        string userId,
        bool isAdmin,
        bool isTeacher,
        bool isStudent,
        int? year,
        int? month)
    {
        var selectedMonth = NormalizeMonth(year, month);
        var firstDay = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddTicks(-1);

        var query = _context.Assignments
            .Where(a => a.Deadline.HasValue &&
                a.Deadline.Value >= firstDay.ToUniversalTime() &&
                a.Deadline.Value <= lastDay.ToUniversalTime());

        if (!isAdmin)
        {
            if (isTeacher)
            {
                query = query.Where(a => a.Course!.TeacherId == userId);
            }
            else if (isStudent)
            {
                query = query.Where(a =>
                    a.Course!.StudentCourses.Any(sc => sc.StudentId == userId));
            }
            else
            {
                query = query.Where(a => false);
            }
        }

        var assignments = await query
            .Include(a => a.Course)
            .OrderBy(a => a.Deadline)
            .Select(a => new CalendarAssignmentVm
            {
                AssignmentId = a.Id,
                CourseId = a.CourseId,
                CourseName = a.Course!.Name,
                AssignmentTitle = a.Title,
                Deadline = a.Deadline!.Value
            })
            .ToListAsync();

        return BuildCalendar(firstDay, assignments);
    }

    private static DateTime NormalizeMonth(int? year, int? month)
    {
        var today = DateTime.Today;

        if (!year.HasValue || !month.HasValue || month < 1 || month > 12)
        {
            return new DateTime(today.Year, today.Month, 1);
        }

        return new DateTime(year.Value, month.Value, 1);
    }

    private static CalendarVm BuildCalendar(DateTime firstDay, List<CalendarAssignmentVm> assignments)
    {
        var startOffset = ((int)firstDay.DayOfWeek + 6) % 7;
        var gridStart = firstDay.AddDays(-startOffset);
        var assignmentLookup = assignments
            .GroupBy(a => a.Deadline.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var days = new List<CalendarDayVm>();

        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            assignmentLookup.TryGetValue(date.Date, out var dayAssignments);

            days.Add(new CalendarDayVm
            {
                Date = date,
                IsCurrentMonth = date.Month == firstDay.Month,
                IsToday = date.Date == DateTime.Today,
                Assignments = dayAssignments ?? new List<CalendarAssignmentVm>()
            });
        }

        return new CalendarVm
        {
            Year = firstDay.Year,
            Month = firstDay.Month,
            PreviousMonth = firstDay.AddMonths(-1),
            NextMonth = firstDay.AddMonths(1),
            Days = days
        };
    }
}
