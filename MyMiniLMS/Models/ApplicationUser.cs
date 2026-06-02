using Microsoft.AspNetCore.Identity;

namespace MyMiniLMS.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public List<StudentCourse> StudentCourses { get; set; } = new();

    public List<StudentAssignment> StudentAssignments { get; set; } = new();
}