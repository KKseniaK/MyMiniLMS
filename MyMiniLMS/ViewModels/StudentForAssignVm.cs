namespace MyMiniLMS.ViewModels;

public class StudentForAssignVm
{
    public string StudentId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool IsAssigned { get; set; }
}