namespace MyMiniLMS.ViewModels;

public class StudentCourseVm
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string TeacherName { get; set; } = string.Empty;

    public int AssignmentsCount { get; set; }

    public int CompletedCount { get; set; }
}