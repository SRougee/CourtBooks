using CourtBooks.Core.Data;
using CourtBooks.Core.Models;
using CourtBooks.Core.Services;

namespace CourtBooks.Tests;

public sealed class CourtBooksServiceTests
{
    [Fact]
    public void AddStudent_AssignsIdAndStoresStudent()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "010", "sam@example.com", new DateOnly(2010, 1, 1));
        Assert.Equal(1, student.Id);
        Assert.Single(service.Students);
        Assert.Equal("Sam Lee", student.FullName);
    }

    [Fact]
    public void AddStudent_RejectsInvalidEmail()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        Assert.Throws<ArgumentException>(() =>
            service.AddStudent("Sam", "Lee", "", "not-an-email", new DateOnly(2010, 1, 1)));
    }

    [Fact]
    public void UpdateStudent_ChangesStudentDetails()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "010", "sam@example.com", new DateOnly(2010, 1, 1));

        var updated = service.UpdateStudent(student.Id, "Samuel", "Lee", "011", "samuel@example.com", new DateOnly(2010, 2, 2), "Updated");

        Assert.Equal("Samuel Lee", updated.FullName);
        Assert.Equal("011", updated.Phone);
        Assert.Equal("samuel@example.com", updated.Email);
        Assert.Equal("Updated", updated.Notes);
    }

    [Fact]
    public void SetStudentActive_TogglesStudentStatus()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        service.SetStudentActive(student.Id, false);

        Assert.False(student.Active);
    }

    [Fact]
    public void ScheduleLesson_RejectsUnknownStudent()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        Assert.Throws<ArgumentException>(() =>
            service.ScheduleLesson(99, DateTime.Now, 60, 300));
    }

    [Fact]
    public void ScheduleLesson_RejectsOverlappingScheduledLesson()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        service.ScheduleLesson(student.Id, new DateTime(2026, 9, 22, 10, 0, 0), 60, 300);

        Assert.Throws<InvalidOperationException>(() =>
            service.ScheduleLesson(student.Id, new DateTime(2026, 9, 22, 10, 30, 0), 30, 300));
    }

    [Fact]
    public void InvoicePayment_TracksOutstandingBalance()
    {
        var service = new CourtBooksService(new CourtBooksStore());
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
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        Assert.Throws<InvalidOperationException>(() => invoice.RecordPayment(1201));
    }

    [Fact]
    public void CompletedProgress_RecordsCompletionDateAndPercent()
    {
        var store = new CourtBooksStore();
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
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 500, new DateOnly(2026, 9, 1), 7);

        service.MarkOverdueInvoices(new DateOnly(2026, 9, 10));

        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public void RecordPayment_CreatesPaymentHistory()
    {
        var store = new CourtBooksStore();
        var service = new CourtBooksService(store);
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        service.RecordPayment(invoice.Id, 500, new DateTime(2026, 9, 21, 10, 0, 0));

        Assert.Single(store.Payments);
        Assert.Equal(500, store.Payments[0].Amount);
        Assert.Equal(invoice.Id, store.Payments[0].InvoiceId);
    }

    [Fact]
    public void SqliteStore_PersistsStudentsAcrossStoreInstances()
    {
        var path = Path.Combine(Path.GetTempPath(), $"courtbooks-{Guid.NewGuid():N}.db");
        try
        {
            var first = new CourtBooksStore();
            first.ConfigureDatabase($"Data Source={path};Foreign Keys=True");
            first.AddStudent(new Student { FirstName = "Persistent", LastName = "Student", Phone = "010", Email = "persistent@example.com", DateOfBirth = new DateOnly(2010, 1, 1) });
            first.Save();

            var second = new CourtBooksStore();
            second.ConfigureDatabase($"Data Source={path};Foreign Keys=True");

            Assert.Single(second.Students);
            Assert.Equal("Persistent Student", second.Students[0].FullName);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}