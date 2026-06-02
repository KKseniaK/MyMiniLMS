using MyMiniLMS.Models;

namespace MyMiniLMS.ViewModels;

public class StudentAssignmentVm
{
    public int AssignmentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? Deadline { get; set; }

    public AssignmentStatus Status { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
