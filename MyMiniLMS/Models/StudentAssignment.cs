namespace MyMiniLMS.Models;

public class StudentAssignment
{
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser Student { get; set; } = null!;

    public int AssignmentId { get; set; }

    public Assignment Assignment { get; set; } = null!;

    public AssignmentStatus Status { get; set; } = AssignmentStatus.NotStarted;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}