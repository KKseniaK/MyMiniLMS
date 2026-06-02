namespace MyMiniLMS.ViewModels;

public class AssignmentProgressVm
{
    public string Title { get; set; } = string.Empty;

    public int CompletedCount { get; set; }

    public int InProgressCount { get; set; }

    public int NotStartedCount { get; set; }
}
