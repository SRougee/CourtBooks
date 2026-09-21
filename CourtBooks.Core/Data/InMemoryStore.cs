using CourtBooks.Core.Models;

namespace CourtBooks.Core.Data;

public sealed class InMemoryStore
{
    private int _studentId;
    private int _lessonId;
    private int _invoiceId;

    public List<Student> Students { get; } = [];
    public List<Lesson> Lessons { get; } = [];
    public List<CurriculumStep> Curriculum { get; } = [];
    public List<StudentProgress> Progress { get; } = [];
    public List<Invoice> Invoices { get; } = [];

    public Student AddStudent(Student student)
    {
        if (string.IsNullOrWhiteSpace(student.FirstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(student.LastName)) throw new ArgumentException("Last name is required.");
        if (!string.IsNullOrWhiteSpace(student.Email) && !student.Email.Contains('@')) throw new ArgumentException("Email address is invalid.");

        var saved = new Student
        {
            Id = ++_studentId,
            FirstName = student.FirstName.Trim(), LastName = student.LastName.Trim(),
            Phone = student.Phone.Trim(), Email = student.Email.Trim(), DateOfBirth = student.DateOfBirth,
            Notes = student.Notes.Trim(), Active = true
        };
        Students.Add(saved);
        return saved;
    }

    public Lesson AddLesson(Lesson lesson)
    {
        if (!Students.Any(s => s.Id == lesson.StudentId)) throw new ArgumentException("Student does not exist.");
        if (lesson.DurationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(lesson.DurationMinutes));
        if (lesson.HourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(lesson.HourlyRate));
        if (Lessons.Any(x => x.Status == LessonStatus.Scheduled && x.Start < lesson.Start.AddMinutes(lesson.DurationMinutes) && x.End > lesson.Start))
            throw new InvalidOperationException("The coach already has a lesson during this time.");

        var saved = new Lesson
        {
            Id = ++_lessonId, StudentId = lesson.StudentId, Start = lesson.Start,
            DurationMinutes = lesson.DurationMinutes, HourlyRate = lesson.HourlyRate,
            Location = lesson.Location.Trim(), Notes = lesson.Notes.Trim(), Status = LessonStatus.Scheduled
        };
        Lessons.Add(saved);
        return saved;
    }

    public Invoice CreateInvoice(int studentId, decimal amount, DateOnly issueDate, int paymentTermsDays = 30)
    {
        if (!Students.Any(s => s.Id == studentId)) throw new ArgumentException("Student does not exist.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays));

        var number = $"CB-{issueDate:yyyyMMdd}-{_invoiceId + 1:0000}";
        var invoice = new Invoice { Id = ++_invoiceId, StudentId = studentId, InvoiceNumber = number,
            IssueDate = issueDate, DueDate = issueDate.AddDays(paymentTermsDays), Amount = decimal.Round(amount, 2) };
        Invoices.Add(invoice);
        return invoice;
    }
}