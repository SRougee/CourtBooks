using CourtBooks.Core.Data;
using CourtBooks.Core.Models;
using CourtBooks.Core.Services;

namespace CourtBooks.Tests;

public sealed class CourtBooksServiceTests
{
    [Fact]
    public void AddStudent_AssignsIdAndStoresStudent()
    {
        var service = new CourtBooksService(new InMemoryStore());
        var student = service.AddStudent("Sam", "Lee", "010", "sam@example.com", new DateOnly(2010, 1, 1));
        Assert.Equal(1, student.Id);
        Assert.Single(service.Students);
        Assert.Equal("Sam Lee", student.FullName);
    }

    [Fact]
    public void AddStudent_RejectsInvalidEmail()
    {
        var service = new CourtBooksService(new InMemoryStore());
        Assert.Throws<ArgumentException>(() =>
            service.AddStudent("Sam", "Lee", "", "not-an-email", new DateOnly(2010, 1, 1)));
    }

    [Fact]
    public void ScheduleLesson_RejectsUnknownStudent()
    {
        var service = new CourtBooksService(new InMemoryStore());
        Assert.Throws<ArgumentException>(() =>
            service.ScheduleLesson(99, DateTime.Now, 60, 300));
    }

    [Fact]
    public void ScheduleLesson_RejectsOverlappingScheduledLesson()
    {
        var service = new CourtBooksService(new InMemoryStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        service.ScheduleLesson(student.Id, new DateTime(2026, 9, 22, 10, 0, 0), 60, 300);

        Assert.Throws<InvalidOperationException>(() =>
            service.ScheduleLesson(student.Id, new DateTime(2026, 9, 22, 10, 30, 0), 30, 300));
    }

    [Fact]
    public void InvoicePayment_TracksOutstandingBalance()
    {
        var service = new CourtBooksService(new InMemoryStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        invoice.RecordPayment(500);

        Assert.Equal(700, invoice.Balance);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
        Assert.Equal(700, service.GetOutstandingBalance(student.Id));
    }

    [Fact]
    public void Invoice_RejectsOverpayment()
    {
        var service = new CourtBooksService(new InMemoryStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        Assert.Throws<InvalidOperationException>(() => invoice.RecordPayment(1201));
    }

    [Fact]
    public void CompletedProgress_RecordsCompletionDateAndPercent()
    {
        var store = new InMemoryStore();
        var service = new CourtBooksService(store);
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        store.Curriculum.Add(new CurriculumStep { Id = 1, Name = "Forehand", Category = "Groundstrokes", Order = 1 });
        store.Curriculum.Add(new CurriculumStep { Id = 2, Name = "Serve", Category = "Serve", Order = 2 });

        var progress = service.SetProgress(student.Id, 1, ProgressStatus.Completed);

        Assert.Equal(ProgressStatus.Completed, progress.Status);
        Assert.NotNull(progress.CompletedOn);
        Assert.Equal(50, service.GetProgressPercent(student.Id));
    }

    [Fact]
    public void MarkOverdueInvoices_ChangesStatusWhenDueDatePassed()
    {
        var service = new CourtBooksService(new InMemoryStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 500, new DateOnly(2026, 9, 1), 7);

        service.MarkOverdueInvoices(new DateOnly(2026, 9, 10));

        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }
}