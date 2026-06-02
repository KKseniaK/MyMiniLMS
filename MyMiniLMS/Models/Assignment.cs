using System.ComponentModel.DataAnnotations;

namespace MyMiniLMS.Models;

public class Assignment : IValidatableObject
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Course is required.")]
    public int CourseId { get; set; }

    public Course? Course { get; set; }

    [Required(ErrorMessage = "Assignment title is required.")]
    [StringLength(150, ErrorMessage = "Assignment title must be 150 characters or less.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Assignment description must be 2000 characters or less.")]
    public string? Description { get; set; }

    public DateTime? Deadline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<StudentAssignment> StudentAssignments { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Deadline.HasValue && Deadline.Value < DateTime.UtcNow.AddMinutes(-1))
        {
            yield return new ValidationResult(
                "Deadline cannot be in the past.",
                new[] { nameof(Deadline) });
        }
    }
}
