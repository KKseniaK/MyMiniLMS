using MyMiniLMS.Models;

namespace MyMiniLMS.ViewModels;

public class StudentDashboardVm
{
    public List<StudentCourseVm> Courses { get; set; } = new();
}