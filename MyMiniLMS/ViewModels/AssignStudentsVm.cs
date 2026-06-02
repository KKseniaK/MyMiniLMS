namespace MyMiniLMS.ViewModels;

public class AssignStudentsVm
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public List<StudentForAssignVm> Students { get; set; } = new();
}