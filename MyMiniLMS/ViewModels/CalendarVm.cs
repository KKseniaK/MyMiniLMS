namespace MyMiniLMS.ViewModels;

public class CalendarVm
{
    public int Year { get; set; }

    public int Month { get; set; }

    public DateTime PreviousMonth { get; set; }

    public DateTime NextMonth { get; set; }

    public List<CalendarDayVm> Days { get; set; } = new();
}
