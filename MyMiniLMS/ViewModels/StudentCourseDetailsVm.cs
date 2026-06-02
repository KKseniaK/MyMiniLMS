using MyMiniLMS.Models;

namespace MyMiniLMS.ViewModels;

public class StudentCourseDetailsVm
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AssignmentStatus? StatusFilter { get; set; }

    public string Sort { get; set; } = string.Empty;

    public List<StudentAssignmentVm> Assignments { get; set; } = new();
}
