namespace CourtBooks.Core.Models;

public sealed class CurriculumStep
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public int Order { get; init; }
}

public sealed class StudentProgress
{
    public int StudentId { get; init; }
    public int CurriculumStepId { get; init; }
    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;
    public DateTime? CompletedOn { get; set; }
    public string Notes { get; set; } = "";
}

public enum ProgressStatus
{
    NotStarted,
    InProgress,
    Completed
}