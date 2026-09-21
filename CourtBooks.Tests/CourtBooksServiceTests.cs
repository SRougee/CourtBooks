using Xunit;
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
    public void AddStudent_AllowsBoundaryValues()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var today = DateOnly.FromDateTime(DateTime.Today);
        var exactly100YearsAgo = today.AddYears(-100);

        var todayStudent = service.AddStudent("Today", "Student", "", "", today);
        var oldStudent = service.AddStudent("Old", "Student", "011", "", exactly100YearsAgo);

        Assert.Equal(today, todayStudent.DateOfBirth);
        Assert.Equal(exactly100YearsAgo, oldStudent.DateOfBirth);
    }

    [Fact]
    public void AddStudent_RejectsDuplicatePhoneIgnoringFormatting()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        service.AddStudent("Sam", "Lee", "082 123 4567", "", new DateOnly(2010, 1, 1));

        Assert.Throws<InvalidOperationException>(() =>
            service.AddStudent("Other", "Person", "082-123-4567", "", new DateOnly(2011, 1, 1)));
    }

    [Fact]
    public void ScheduleLesson_RejectsZeroAndNegativeDuration()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.ScheduleLesson(student.Id, DateTime.Now.AddDays(1), 0, 300));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.ScheduleLesson(student.Id, DateTime.Now.AddDays(1), -1, 300));
    }

    [Fact]
    public void ScheduleLesson_AllowsZeroHourlyRateButRejectsNegativeRate()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var freeStudent = service.AddStudent("Free", "Student", "", "", new DateOnly(2010, 1, 1));
        var paidStudent = service.AddStudent("Paid", "Student", "011", "", new DateOnly(2010, 1, 1));

        var freeLesson = service.ScheduleLesson(freeStudent.Id, DateTime.Now.AddDays(1), 60, 0);
        Assert.Equal(0, freeLesson.HourlyRate);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.ScheduleLesson(paidStudent.Id, DateTime.Now.AddDays(1), 60, -0.01m));
    }

    [Fact]
    public void InvoicePayment_UsesExactBalanceAsPaidBoundary()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        invoice.RecordPayment(1200);

        Assert.Equal(0, invoice.Balance);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void Invoice_RejectsZeroAndNegativeAmounts()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.InvoiceStudent(student.Id, 0, DateOnly.FromDateTime(DateTime.Today)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.InvoiceStudent(student.Id, -0.01m, DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public void Invoice_AllowsZeroDayTermsButRejectsNegativeTerms()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        var invoice = service.InvoiceStudent(student.Id, 100, DateOnly.FromDateTime(DateTime.Today), 0);
        Assert.Equal(invoice.IssueDate, invoice.DueDate);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.InvoiceStudent(student.Id, 100, DateOnly.FromDateTime(DateTime.Today), -1));
    }

    [Fact]
    public void RecurringLessons_RejectsOccurrenceBoundaries()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        var one = service.ScheduleRecurringLessons(student.Id, new RecurringLessonPattern
        {
            FirstStart = DateTime.Now.AddDays(1),
            Occurrences = 1,
            IntervalDays = 7,
            DurationMinutes = 60,
            HourlyRate = 300
        });
        Assert.Single(one);

        var remaining = 51;
        var start = DateTime.Now.AddDays(2);
        var max = service.ScheduleRecurringLessons(student.Id, new RecurringLessonPattern
        {
            FirstStart = start,
            Occurrences = remaining,
            IntervalDays = 7,
            DurationMinutes = 30,
            HourlyRate = 0
        });
        Assert.Equal(remaining, max.Count);
    }

    [Fact]
    public void RecurringLessons_RejectsZeroAndOverMaximumOccurrences()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.ScheduleRecurringLessons(student.Id, new RecurringLessonPattern
            {
                FirstStart = DateTime.Now.AddDays(1),
                Occurrences = 0
            }));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.ScheduleRecurringLessons(student.Id, new RecurringLessonPattern
            {
                FirstStart = DateTime.Now.AddDays(1),
                Occurrences = 53
            }));
    }

    [Fact]
    public void Sqlite_Parameterization_PreventsInjectionThroughStudentFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"courtbooks-injection-{Guid.NewGuid():N}.db");
        const string malicious = "Robert'); DROP TABLE Students;--";
        try
        {
            var store = new CourtBooksStore();
            store.ConfigureDatabase($"Data Source={path};Foreign Keys=True;Pooling=False");
            var service = new CourtBooksService(store);

            var student = service.AddStudent(malicious, "Test", malicious, "safe@example.com", new DateOnly(2010, 1, 1), malicious);
            var invoice = service.InvoiceStudent(student.Id, 100, DateOnly.FromDateTime(DateTime.Today));
            service.RecordPayment(invoice.Id, 100, DateTime.Now, PaymentMethod.EFT, malicious);
            store.Save();

            var reloaded = new CourtBooksStore();
            reloaded.ConfigureDatabase($"Data Source={path};Foreign Keys=True;Pooling=False");

            Assert.Single(reloaded.Students);
            Assert.Equal(malicious, reloaded.Students[0].FirstName);
            Assert.Equal(malicious, reloaded.Students[0].Phone);
            Assert.Equal(malicious, reloaded.Students[0].Notes);
            Assert.Equal(malicious, reloaded.Payments[0].Reference);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
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
    public void AddStudent_RejectsFutureDateOfBirth()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        Assert.Throws<ArgumentException>(() =>
            service.AddStudent("Sam", "Lee", "", "", DateOnly.FromDateTime(DateTime.Today.AddDays(1))));
    }

    [Fact]
    public void AddStudent_RejectsDuplicateEmail()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        service.AddStudent("Sam", "Lee", "", "sam@example.com", new DateOnly(2010, 1, 1));

        Assert.Throws<InvalidOperationException>(() =>
            service.AddStudent("Other", "Person", "", "SAM@example.com", new DateOnly(2011, 1, 1)));
    }

    [Fact]
    public void ScheduleLesson_RejectsInactiveStudent()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        service.SetStudentActive(student.Id, false);

        Assert.Throws<InvalidOperationException>(() =>
            service.ScheduleLesson(student.Id, DateTime.Now.AddDays(1), 60, 300));
    }

    [Fact]
    public void ScheduleLesson_RejectsPastLesson()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));

        Assert.Throws<ArgumentException>(() =>
            service.ScheduleLesson(student.Id, DateTime.Now.AddMinutes(-5), 60, 300));
    }

    [Fact]
    public void RescheduleLesson_UpdatesFutureTime()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var lesson = service.ScheduleLesson(student.Id, DateTime.Now.AddDays(1), 60, 300);

        var updated = service.RescheduleLesson(lesson.Id, DateTime.Now.AddDays(2), 90);

        Assert.Equal(90, updated.DurationMinutes);
        Assert.True(updated.Start > DateTime.Now.AddDays(1));
    }

    [Fact]
    public void CancelInvoice_RemovesItFromOutstandingBalance()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        service.CancelInvoice(invoice.Id);

        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
        Assert.Equal(0, service.GetOutstandingBalance(student.Id));
    }

    [Fact]
    public void CancelInvoice_RejectsPartiallyPaidInvoice()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));
        invoice.RecordPayment(100);

        Assert.Throws<InvalidOperationException>(() => service.CancelInvoice(invoice.Id));
    }

    [Fact]
    public void RecordPayment_RejectsFuturePaymentDate()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        Assert.Throws<ArgumentException>(() =>
            service.RecordPayment(invoice.Id, 100, DateTime.Now.AddDays(1)));
    }

    [Fact]
    public void RecordPayment_StoresMethodAndReference()
    {
        var store = new CourtBooksStore();
        var service = new CourtBooksService(store);
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var invoice = service.InvoiceStudent(student.Id, 1200, DateOnly.FromDateTime(DateTime.Today));

        service.RecordPayment(invoice.Id, 500, DateTime.Now, PaymentMethod.EFT, "ABC123");

        Assert.Equal(PaymentMethod.EFT, store.Payments[0].Method);
        Assert.Equal("ABC123", store.Payments[0].Reference);
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
    public void ScheduleRecurringLessons_CreatesWeeklyLessons()
    {
        var service = new CourtBooksService(new CourtBooksStore());
        var student = service.AddStudent("Sam", "Lee", "", "", new DateOnly(2010, 1, 1));
        var first = DateTime.Now.AddDays(1);

        var lessons = service.ScheduleRecurringLessons(student.Id, new RecurringLessonPattern
        {
            FirstStart = first,
            DurationMinutes = 60,
            HourlyRate = 300,
            Occurrences = 3,
            IntervalDays = 7
        });

        Assert.Equal(3, lessons.Count);
        Assert.Equal(first.Date, lessons[0].Start.Date);
        Assert.Equal(first.AddDays(14).Date, lessons[2].Start.Date);
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
    public void SqliteStore_PersistsPaymentDetailsAndCancelledInvoice()
    {
        var path = Path.Combine(Path.GetTempPath(), $"courtbooks-{Guid.NewGuid():N}.db");
        try
        {
            var first = new CourtBooksStore();
            first.ConfigureDatabase($"Data Source={path};Foreign Keys=True;Pooling=False");
            var service = new CourtBooksService(first);
            var student = service.AddStudent("Persistent", "Student", "010", "persistent@example.com", new DateOnly(2010, 1, 1));
            var invoice = service.InvoiceStudent(student.Id, 500, DateOnly.FromDateTime(DateTime.Today));
            service.RecordPayment(invoice.Id, 200, DateTime.Now, PaymentMethod.EFT, "EFT-123");
            var cancelled = service.InvoiceStudent(student.Id, 300, DateOnly.FromDateTime(DateTime.Today));
            service.CancelInvoice(cancelled.Id);
            first.Save();

            var second = new CourtBooksStore();
            second.ConfigureDatabase($"Data Source={path};Foreign Keys=True;Pooling=False");

            Assert.Equal(PaymentMethod.EFT, second.Payments.Single().Method);
            Assert.Equal("EFT-123", second.Payments.Single().Reference);
            Assert.Equal(InvoiceStatus.Cancelled, second.Invoices.Single(x => x.Id == cancelled.Id).Status);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SqliteStore_PersistsStudentsAcrossStoreInstances()
    {
        var path = Path.Combine(Path.GetTempPath(), $"courtbooks-{Guid.NewGuid():N}.db");
        try
        {
            var first = new CourtBooksStore();
            first.ConfigureDatabase($"Data Source={path};Foreign Keys=True;Pooling=False");
            first.AddStudent(new Student { FirstName = "Persistent", LastName = "Student", Phone = "010", Email = "persistent@example.com", DateOfBirth = new DateOnly(2010, 1, 1) });
            first.Save();

            var second = new CourtBooksStore();
            second.ConfigureDatabase($"Data Source={path};Foreign Keys=True;Pooling=False");

            Assert.Single(second.Students);
            Assert.Equal("Persistent Student", second.Students[0].FullName);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}