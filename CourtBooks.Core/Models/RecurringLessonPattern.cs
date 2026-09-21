namespace CourtBooks.Core.Models;

public sealed class RecurringLessonPattern
{
    public DateTime FirstStart { get; init; }
    public int DurationMinutes { get; init; } = 60;
    public decimal HourlyRate { get; init; }
    public int Occurrences { get; init; } = 1;
    public int IntervalDays { get; init; } = 7;
    public string Location { get; init; } = "";
    public string Notes { get; init; } = "";
}
