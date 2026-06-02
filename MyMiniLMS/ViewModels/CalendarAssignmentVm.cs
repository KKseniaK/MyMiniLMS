namespace MyMiniLMS.ViewModels;

public class CalendarAssignmentVm
{
    public int AssignmentId { get; set; }

    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string AssignmentTitle { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }
}
