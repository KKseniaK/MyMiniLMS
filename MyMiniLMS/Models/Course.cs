using System.ComponentModel.DataAnnotations;

namespace MyMiniLMS.Models;

public class Course
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Course name is required.")]
    [StringLength(100, ErrorMessage = "Course name must be 100 characters or less.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Course description must be 1000 characters or less.")]
    public string? Description { get; set; }

    public string? TeacherId { get; set; }

    public ApplicationUser? Teacher { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Assignment> Assignments { get; set; } = new();

    public List<StudentCourse> StudentCourses { get; set; } = new();
}
