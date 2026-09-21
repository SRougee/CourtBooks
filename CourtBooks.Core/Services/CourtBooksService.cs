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

    public IReadOnlyList<Lesson> ScheduleRecurringLessons(int studentId, RecurringLessonPattern pattern)
    {
        if (pattern.Occurrences is < 1 or > 52) throw new ArgumentOutOfRangeException(nameof(pattern.Occurrences), "Occurrences must be between 1 and 52.");
        if (pattern.IntervalDays <= 0) throw new ArgumentOutOfRangeException(nameof(pattern.IntervalDays));
        if (pattern.DurationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(pattern.DurationMinutes));
        if (pattern.HourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(pattern.HourlyRate));

        var starts = Enumerable.Range(0, pattern.Occurrences)
            .Select(i => pattern.FirstStart.AddDays((double)(i * pattern.IntervalDays)))
            .ToList();

        var existing = new List<Lesson>();
        foreach (var start in starts)
        {
            if (start <= DateTime.Now) throw new ArgumentException("All recurring lessons must be in the future.");
            if (existing.Any(x => x.Start < start.AddMinutes(pattern.DurationMinutes) && x.End > start))
                throw new InvalidOperationException("The recurring lessons would overlap each other.");
            if (_store.Lessons.Any(x => x.Status == LessonStatus.Scheduled && x.Start < start.AddMinutes(pattern.DurationMinutes) && x.End > start))
                throw new InvalidOperationException($"The coach already has a lesson during {start:g}.");
            existing.Add(new Lesson { StudentId = studentId, Start = start, DurationMinutes = pattern.DurationMinutes, HourlyRate = pattern.HourlyRate, Location = pattern.Location, Notes = pattern.Notes });
        }

        var result = new List<Lesson>();
        foreach (var lesson in existing)
            result.Add(_store.AddLesson(lesson));
        return result;
    }

    public InvoicePayment RecordPayment(int invoiceId, decimal amount, DateTime? paidOn = null, PaymentMethod method = PaymentMethod.Other, string reference = "")
    {
        var invoice = _store.Invoices.SingleOrDefault(x => x.Id == invoiceId) ?? throw new KeyNotFoundException("Invoice not found.");
        var paymentDate = paidOn ?? DateTime.Now;
        var today = DateTime.Now;
        if (paymentDate > today) throw new ArgumentException("Payment date cannot be in the future.");
        if (DateOnly.FromDateTime(paymentDate) < invoice.IssueDate) throw new ArgumentException("Payment date cannot be before the invoice issue date.");
        invoice.RecordPayment(amount);
        var payment = new InvoicePayment
        {
            Id = _store.Payments.Count == 0 ? 1 : _store.Payments.Max(x => x.Id) + 1,
            InvoiceId = invoiceId,
            Amount = amount,
            PaidOn = paymentDate,
            Method = method,
            Reference = reference.Trim()
        };
        _store.Payments.Add(payment);
        return payment;
    }

    public void UpdateLessonStatus(int lessonId, LessonStatus status)
    {
        var lesson = _store.Lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        if (lesson.Status != LessonStatus.Scheduled && status == LessonStatus.Scheduled)
            throw new InvalidOperationException("Completed, cancelled or no-show lessons cannot be reopened as scheduled.");
        lesson.Status = status;
    }

    public Lesson RescheduleLesson(int lessonId, DateTime newStart, int? durationMinutes = null, string? location = null)
    {
        var lesson = _store.Lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        if (lesson.Status != LessonStatus.Scheduled) throw new InvalidOperationException("Only scheduled lessons can be rescheduled.");
        if (newStart <= DateTime.Now) throw new ArgumentException("Rescheduled lessons must be in the future.");
        var duration = durationMinutes ?? lesson.DurationMinutes;
        if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        var newEnd = newStart.AddMinutes(duration);
        if (_store.Lessons.Any(x => x.Id != lesson.Id && x.Status == LessonStatus.Scheduled && x.Start < newEnd && x.End > newStart))
            throw new InvalidOperationException("The coach already has a lesson during this time.");
        lesson.Start = newStart;
        lesson.DurationMinutes = duration;
        if (location is not null) lesson.Location = location.Trim();
        return lesson;
    }

    public void CancelInvoice(int invoiceId)
    {
        var invoice = _store.Invoices.SingleOrDefault(x => x.Id == invoiceId) ?? throw new KeyNotFoundException("Invoice not found.");
        invoice.Cancel();
    }

    public Invoice InvoiceStudent(int studentId, decimal amount, DateOnly issueDate, int paymentTermsDays = 30)
        => _store.CreateInvoice(studentId, amount, issueDate, paymentTermsDays);

    public IEnumerable<Lesson> GetLessons(DateTime from, DateTime to)
    {
        if (to <= from) throw new ArgumentException("The end date must be after the start date.");
        return _store.Lessons.Where(x => x.Start >= from && x.Start < to).OrderBy(x => x.Start);
    }

    public decimal GetOutstandingBalance(int? studentId = null)
        => _store.Invoices.Where(x => x.Status != InvoiceStatus.Cancelled && (studentId is null || x.StudentId == studentId)).Sum(x => x.Balance);

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