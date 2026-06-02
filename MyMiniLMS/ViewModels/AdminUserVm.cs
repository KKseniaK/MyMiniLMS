namespace MyMiniLMS.ViewModels;

public class AdminUserVm
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public List<string> Roles { get; set; } = new();
}