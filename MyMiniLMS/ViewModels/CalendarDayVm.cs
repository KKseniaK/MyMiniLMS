namespace MyMiniLMS.ViewModels;

public class CalendarDayVm
{
    public DateTime Date { get; set; }

    public bool IsCurrentMonth { get; set; }

    public bool IsToday { get; set; }

    public List<CalendarAssignmentVm> Assignments { get; set; } = new();
}
