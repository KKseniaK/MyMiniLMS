namespace MyMiniLMS.Models;

public class StudentCourse
{
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser Student { get; set; } = null!;

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}