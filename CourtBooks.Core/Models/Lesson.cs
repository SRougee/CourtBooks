namespace CourtBooks.Core.Models;

public sealed class Lesson
{
    public int Id { get; init; }
    public int StudentId { get; init; }
    public DateTime Start { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public decimal HourlyRate { get; set; }
    public string Location { get; set; } = "";
    public string Notes { get; set; } = "";
    public LessonStatus Status { get; set; } = LessonStatus.Scheduled;
    public DateTime End => Start.AddMinutes(DurationMinutes);
}

public enum LessonStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow
}