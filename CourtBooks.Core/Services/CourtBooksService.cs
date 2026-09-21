using CourtBooks.Core.Data;
using CourtBooks.Core.Models;

namespace CourtBooks.Core.Services;

public sealed class CourtBooksService
{
    private readonly CourtBooksStore _store;
    public CourtBooksService(CourtBooksStore store) => _store = store;

    public IReadOnlyList<Student> Students => _store.Students;
    public IReadOnlyList<Lesson> Lessons => _store.Lessons;
    public IReadOnlyList<Invoice> Invoices => _store.Invoices;

    public Student AddStudent(string firstName, string lastName, string phone, string email, DateOnly dob, string notes = "")
        => _store.AddStudent(new Student { FirstName = firstName, LastName = lastName, Phone = phone, Email = email, DateOfBirth = dob, Notes = notes });

    public Student UpdateStudent(int studentId, string firstName, string lastName, string phone, string email, DateOnly dateOfBirth, string notes = "")
        => _store.UpdateStudent(studentId, firstName, lastName, phone, email, dateOfBirth, notes);

    public void SetStudentActive(int studentId, bool active)
        => _store.SetStudentActive(studentId, active);

    public Lesson ScheduleLesson(int studentId, DateTime start, int durationMinutes, decimal hourlyRate, string location = "", string notes = "")
        => _store.AddLesson(new Lesson { StudentId = studentId, Start = start, DurationMinutes = durationMinutes, HourlyRate = hourlyRate, Location = location, Notes = notes });

    public InvoicePayment RecordPayment(int invoiceId, decimal amount, DateTime? paidOn = null)
    {
        var invoice = _store.Invoices.SingleOrDefault(x => x.Id == invoiceId) ?? throw new KeyNotFoundException("Invoice not found.");
        invoice.RecordPayment(amount);
        var payment = new InvoicePayment
        {
            Id = _store.Payments.Count == 0 ? 1 : _store.Payments.Max(x => x.Id) + 1,
            InvoiceId = invoiceId,
            Amount = amount,
            PaidOn = paidOn ?? DateTime.Now
        };
        _store.Payments.Add(payment);
        return payment;
    }

    public void UpdateLessonStatus(int lessonId, LessonStatus status)
    {
        var lesson = _store.Lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        lesson.Status = status;
    }

    public Invoice InvoiceStudent(int studentId, decimal amount, DateOnly issueDate, int paymentTermsDays = 30)
        => _store.CreateInvoice(studentId, amount, issueDate, paymentTermsDays);

    public IEnumerable<Lesson> GetLessons(DateTime from, DateTime to)
    {
        if (to <= from) throw new ArgumentException("The end date must be after the start date.");
        return _store.Lessons.Where(x => x.Start >= from && x.Start < to).OrderBy(x => x.Start);
    }

    public decimal GetOutstandingBalance(int? studentId = null)
        => _store.Invoices.Where(x => studentId is null || x.StudentId == studentId).Sum(x => x.Balance);

    public int GetProgressPercent(int studentId)
    {
        var total = _store.Curriculum.Count;
        if (total == 0) return 0;
        var completed = _store.Progress.Count(x => x.StudentId == studentId && x.Status == ProgressStatus.Completed);
        return (int)Math.Round(completed * 100d / total);
    }

    public StudentProgress SetProgress(int studentId, int curriculumStepId, ProgressStatus status, string notes = "")
    {
        if (!_store.Students.Any(s => s.Id == studentId)) throw new ArgumentException("Student does not exist.");
        if (!_store.Curriculum.Any(c => c.Id == curriculumStepId)) throw new ArgumentException("Curriculum step does not exist.");

        var existing = _store.Progress.SingleOrDefault(x => x.StudentId == studentId && x.CurriculumStepId == curriculumStepId);
        if (existing is null)
        {
            existing = new StudentProgress { StudentId = studentId, CurriculumStepId = curriculumStepId };
            _store.Progress.Add(existing);
        }
        existing.Status = status;
        existing.Notes = notes;
        existing.CompletedOn = status == ProgressStatus.Completed ? DateTime.Now : null;
        return existing;
    }

    public void MarkOverdueInvoices(DateOnly today)
    {
        foreach (var invoice in _store.Invoices) invoice.MarkOverdue(today);
    }
}