using MyMiniLMS.Models;

namespace MyMiniLMS.ViewModels;

public class TeacherCourseDetailsVm
{
    public Course Course { get; set; } = null!;

    public List<ApplicationUser> Students { get; set; } = new();

    public List<AssignmentProgressVm> Progress { get; set; } = new();
}
