using CourtBooks.Core.Data;
using CourtBooks.Core.Models;
using CourtBooks.Core.Services;

var store = new InMemoryStore();
Seed(store);
var app = new CourtBooksService(store);
app.MarkOverdueInvoices(DateOnly.FromDateTime(DateTime.Today));

while (true)
{
    Console.Clear();
    Console.WriteLine("=================================");
    Console.WriteLine("           COURTBOOKS");
    Console.WriteLine(" Tennis Coach Practice Management");
    Console.WriteLine("=================================");
    Console.WriteLine("1. Dashboard");
    Console.WriteLine("2. Students");
    Console.WriteLine("3. Schedule lesson");
    Console.WriteLine("4. View / update schedule");
    Console.WriteLine("5. Curriculum progress");
    Console.WriteLine("6. Create invoice");
    Console.WriteLine("7. Record payment");
    Console.WriteLine("0. Exit");
    Console.Write("\nChoose: ");

    try
    {
        switch (Console.ReadLine())
        {
            case "1": Dashboard(app); break;
            case "2": Students(app); break;
            case "3": Schedule(app); break;
            case "4": ScheduleView(app); break;
            case "5": Progress(app, store); break;
            case "6": Invoice(app); break;
            case "7": Payment(app, store); break;
            case "0": return;
            default: Pause("Invalid option."); break;
        }
    }
    catch (Exception ex)
    {
        Pause($"Error: {ex.Message}");
    }
}

static void Dashboard(CourtBooksService app)
{
    Console.Clear();
    var today = DateTime.Today;
    Console.WriteLine("DASHBOARD");
    Console.WriteLine($"Students:          {app.Students.Count(s => s.Active)}");
    Console.WriteLine($"Today's lessons:   {app.GetLessons(today, today.AddDays(1)).Count()}");
    Console.WriteLine($"Next 7 days:       {app.GetLessons(DateTime.Now, DateTime.Now.AddDays(7)).Count()}");
    Console.WriteLine($"Outstanding:       {app.GetOutstandingBalance():C2}");
    Console.WriteLine($"Invoices:           {app.Invoices.Count}");
    Pause();
}

static void Students(CourtBooksService app)
{
    Console.Clear();
    Console.WriteLine("STUDENTS");
    foreach (var s in app.Students)
        Console.WriteLine($"{s.Id,2} | {s.FullName,-24} | {s.Phone,-14} | {s.Email,-28} | {(s.Active ? "Active" : "Inactive")}");

    Console.Write("\nAdd student? (y/n): ");
    if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y") { Pause(); return; }

    var student = app.AddStudent(
        ReadRequired("First name"),
        ReadRequired("Last name"),
        Read("Phone"),
        Read("Email"),
        ReadDate("Date of birth (yyyy-mm-dd)", DateOnly.MinValue),
        Read("Notes"));

    Console.WriteLine($"Added student #{student.Id}: {student.FullName}");
    Pause();
}

static void Schedule(CourtBooksService app)
{
    Console.Clear();
    StudentsMini(app);
    var id = ReadInt("Student ID");
    var start = ReadDateTime("Start (yyyy-MM-dd HH:mm)");
    var duration = ReadInt("Duration minutes", 60);
    var rate = ReadDecimal("Hourly rate", 300);
    var location = Read("Location");
    var lesson = app.ScheduleLesson(id, start, duration, rate, location, Read("Notes"));
    Console.WriteLine($"Lesson #{lesson.Id} scheduled from {lesson.Start:g} to {lesson.End:t}.");
    Pause();
}

static void ScheduleView(CourtBooksService app)
{
    Console.Clear();
    var from = ReadDate("From date (yyyy-mm-dd)", DateOnly.FromDateTime(DateTime.Today));
    var to = ReadDate("To date (yyyy-mm-dd)", from);
    var lessons = app.GetLessons(from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue)).ToList();

    Console.WriteLine("SCHEDULE");
    if (lessons.Count == 0) Console.WriteLine("No lessons found.");
    foreach (var l in lessons)
    {
        var student = app.Students.First(s => s.Id == l.StudentId);
        Console.WriteLine($"{l.Id,3} | {l.Start:ddd dd MMM HH:mm} | {student.FullName,-24} | {l.DurationMinutes,3} min | {l.HourlyRate:C2} | {l.Status}");
    }

    if (lessons.Count > 0)
    {
        Console.Write("\nUpdate lesson status? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
        {
            var lessonId = ReadInt("Lesson ID");
            Console.WriteLine("1=Scheduled 2=Completed 3=Cancelled 4=NoShow");
            var choice = ReadInt("Status");
            if (choice is < 1 or > 4) throw new ArgumentOutOfRangeException("status");
            app.UpdateLessonStatus(lessonId, (LessonStatus)(choice - 1));
            Console.WriteLine("Lesson status updated.");
        }
    }
    Pause();
}

static void Progress(CourtBooksService app, InMemoryStore store)
{
    Console.Clear();
    StudentsMini(app);
    var studentId = ReadInt("Student ID");
    if (!store.Students.Any(x => x.Id == studentId)) throw new ArgumentException("Student does not exist.");

    if (store.Curriculum.Count == 0) SeedCurriculum(store);
    Console.WriteLine($"Current progress: {app.GetProgressPercent(studentId)}%\n");

    foreach (var step in store.Curriculum.OrderBy(x => x.Order))
    {
        var p = store.Progress.SingleOrDefault(x => x.StudentId == studentId && x.CurriculumStepId == step.Id);
        Console.WriteLine($"{step.Id}. {step.Category,-14} {step.Name,-28} [{p?.Status.ToString() ?? "NotStarted"}]");
    }

    var stepId = ReadInt("Curriculum step ID");
    Console.WriteLine("1=NotStarted 2=InProgress 3=Completed");
    var choice = ReadInt("Status");
    if (choice is < 1 or > 3) throw new ArgumentOutOfRangeException("status");
    app.SetProgress(studentId, stepId, (ProgressStatus)(choice - 1), Read("Notes"));
    Console.WriteLine($"Progress saved. Student progress: {app.GetProgressPercent(studentId)}%.");
    Pause();
}

static void Invoice(CourtBooksService app)
{
    Console.Clear();
    StudentsMini(app);
    var studentId = ReadInt("Student ID");
    var amount = ReadDecimal("Invoice amount");
    var terms = ReadInt("Payment terms (days)", 30);
    var invoice = app.InvoiceStudent(studentId, amount, DateOnly.FromDateTime(DateTime.Today), terms);
    Console.WriteLine($"Created {invoice.InvoiceNumber} for {invoice.Amount:C2}, due {invoice.DueDate:yyyy-MM-dd}.");
    Pause();
}

static void Payment(CourtBooksService app, InMemoryStore store)
{
    Console.Clear();
    app.MarkOverdueInvoices(DateOnly.FromDateTime(DateTime.Today));
    foreach (var i in app.Invoices)
    {
        var student = app.Students.First(s => s.Id == i.StudentId);
        Console.WriteLine($"{i.InvoiceNumber} | {student.FullName,-24} | {i.Amount,10:C2} | Paid {i.AmountPaid,10:C2} | Balance {i.Balance,10:C2} | {i.Status}");
    }

    if (app.Invoices.Count == 0) { Pause("No invoices."); return; }

    var number = ReadRequired("Invoice number");
    var invoice = store.Invoices.SingleOrDefault(x => x.InvoiceNumber.Equals(number, StringComparison.OrdinalIgnoreCase));
    if (invoice is null) { Pause("Invoice not found."); return; }

    invoice.RecordPayment(ReadDecimal("Payment amount"));
    Console.WriteLine($"Payment recorded. Balance: {invoice.Balance:C2}. Status: {invoice.Status}");
    Pause();
}

static void StudentsMini(CourtBooksService app)
{
    foreach (var s in app.Students.Where(x => x.Active))
        Console.WriteLine($"{s.Id}. {s.FullName}");
}

static string Read(string label, string? defaultValue = null)
{
    Console.Write($"{label}{(defaultValue is null ? "" : $" [{defaultValue}]")}: ");
    var value = Console.ReadLine()?.Trim();
    return string.IsNullOrWhiteSpace(value) ? defaultValue ?? "" : value;
}

static string ReadRequired(string label)
{
    while (true)
    {
        var value = Read(label);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        Console.WriteLine("A value is required.");
    }
}

static int ReadInt(string label, int? defaultValue = null)
{
    while (true)
    {
        var s = Read(label, defaultValue?.ToString());
        if (int.TryParse(s, out var value) && value >= 0) return value;
        Console.WriteLine("Enter a valid whole number.");
    }
}

static decimal ReadDecimal(string label, decimal? defaultValue = null)
{
    while (true)
    {
        var s = Read(label, defaultValue?.ToString("0.00"));
        if (decimal.TryParse(s, out var value) && value >= 0) return value;
        Console.WriteLine("Enter a valid amount.");
    }
}

static DateOnly ReadDate(string label, DateOnly defaultValue)
{
    while (true)
    {
        var s = Read(label, defaultValue == DateOnly.MinValue ? null : defaultValue.ToString("yyyy-MM-dd"));
        if (DateOnly.TryParse(s, out var value)) return value;
        Console.WriteLine("Use yyyy-mm-dd.");
    }
}

static DateTime ReadDateTime(string label)
{
    while (true)
    {
        var s = Read(label);
        if (DateTime.TryParse(s, out var value)) return value;
        Console.WriteLine("Enter a valid date/time, e.g. 2026-09-22 14:30.");
    }
}

static void Pause(string message = "Press Enter to continue...")
{
    Console.WriteLine(message);
    Console.ReadLine();
}

static void Seed(InMemoryStore store)
{
    store.AddStudent(new Student { FirstName = "Alex", LastName = "Naidoo", Phone = "082 555 0101", Email = "alex@example.com", DateOfBirth = new DateOnly(2014, 4, 12) });
    store.AddStudent(new Student { FirstName = "Mia", LastName = "Jacobs", Phone = "082 555 0102", Email = "mia@example.com", DateOfBirth = new DateOnly(2012, 9, 4) });
    SeedCurriculum(store);
}

static void SeedCurriculum(InMemoryStore store)
{
    if (store.Curriculum.Count > 0) return;
    var names = new[]
    {
        ("Ready Position", "Foundation"),
        ("Forehand", "Groundstrokes"),
        ("Backhand", "Groundstrokes"),
        ("Serve", "Serve"),
        ("Volley", "Net Play"),
        ("Movement", "Footwork"),
        ("Match Play", "Tactics")
    };
    var id = 0;
    foreach (var (name, category) in names)
        store.Curriculum.Add(new CurriculumStep { Id = ++id, Name = name, Category = category, Order = id });
}