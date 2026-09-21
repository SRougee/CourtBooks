using CourtBooks.Core.Configuration;
using CourtBooks.Core.Data;
using CourtBooks.Core.Models;
using CourtBooks.Core.Services;

var settings = CourtBooksSettings.Load(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
var store = new CourtBooksStore();
store.ConfigureDatabase(settings.DefaultConnection);

if (store.Students.Count == 0)
{
    Seed(store);
    store.Save();
}

var app = new CourtBooksService(store);
app.MarkOverdueInvoices(DateOnly.FromDateTime(DateTime.Today));
store.Save();

while (true)
{
    try
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
        Console.WriteLine("8. Reports");
        Console.WriteLine("0. Exit");
        Console.WriteLine("\nTip: type M at any input prompt to return here.");
        Console.Write("\nChoose: ");

        switch (Console.ReadLine()?.Trim().ToLowerInvariant())
        {
            case "1": Dashboard(app); break;
            case "2": Students(app); break;
            case "3": Schedule(app); break;
            case "4": ScheduleView(app); break;
            case "5": Progress(app, store); break;
            case "6": Invoice(app); break;
            case "7": Payment(app, store); break;
            case "8": Reports(app, store); break;
            case "0": store.Save(); return;
            default: Pause("Invalid option."); break;
        }

        store.Save();
    }
    catch (ReturnToMenuException)
    {
        store.Save();
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
    Console.WriteLine("---------");
    Console.WriteLine($"Active students:   {app.Students.Count(s => s.Active)}");
    Console.WriteLine($"Today's lessons:   {app.GetLessons(today, today.AddDays(1)).Count()}");
    Console.WriteLine($"Next 7 days:       {app.GetLessons(DateTime.Now, DateTime.Now.AddDays(7)).Count()}");
    Console.WriteLine($"Outstanding:       {app.GetOutstandingBalance():C2}");
    Console.WriteLine($"Invoices:          {app.Invoices.Count}");
    Pause();
}

static void Students(CourtBooksService app)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("STUDENTS");
        Console.WriteLine("--------");
        foreach (var s in app.Students.OrderBy(x => x.Id))
            Console.WriteLine($"{s.Id,2} | {s.FullName,-24} | {s.Phone,-14} | {s.Email,-28} | {(s.Active ? "Active" : "Inactive")}");

        Console.WriteLine("\nA. Add student");
        Console.WriteLine("E. Edit student");
        Console.WriteLine("T. Activate / deactivate student");
        Console.WriteLine("M. Main menu");
        Console.Write("\nChoose: ");

        switch (Console.ReadLine()?.Trim().ToLowerInvariant())
        {
            case "a":
                AddStudent(app);
                break;
            case "e":
                EditStudent(app);
                break;
            case "t":
                ToggleStudent(app);
                break;
            case "m":
                return;
            default:
                Pause("Invalid option.");
                break;
        }
    }
}

static void AddStudent(CourtBooksService app)
{
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

static void EditStudent(CourtBooksService app)
{
    var id = ReadInt("Student ID");
    var student = app.Students.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Student not found.");

    var updated = app.UpdateStudent(
        id,
        Read("First name", student.FirstName),
        Read("Last name", student.LastName),
        Read("Phone", student.Phone),
        Read("Email", student.Email),
        ReadDate("Date of birth (yyyy-mm-dd)", student.DateOfBirth),
        Read("Notes", student.Notes));

    Console.WriteLine($"Updated student #{updated.Id}: {updated.FullName}");
    Pause();
}

static void ToggleStudent(CourtBooksService app)
{
    var id = ReadInt("Student ID");
    var student = app.Students.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Student not found.");
    app.SetStudentActive(id, !student.Active);
    Console.WriteLine($"{student.FullName} is now {(student.Active ? "active" : "inactive")}.");
    Pause();
}

static void Schedule(CourtBooksService app)
{
    Console.Clear();
    Console.WriteLine("SCHEDULE LESSON");
    Console.WriteLine("----------------");
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
    Console.WriteLine("--------");
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

static void Progress(CourtBooksService app, CourtBooksStore store)
{
    Console.Clear();
    Console.WriteLine("CURRICULUM PROGRESS");
    Console.WriteLine("-------------------");
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
    Console.WriteLine("CREATE INVOICE");
    Console.WriteLine("--------------");
    StudentsMini(app);
    var studentId = ReadInt("Student ID");
    var amount = ReadDecimal("Invoice amount");
    var terms = ReadInt("Payment terms (days)", 30);
    var invoice = app.InvoiceStudent(studentId, amount, DateOnly.FromDateTime(DateTime.Today), terms);
    Console.WriteLine($"Created {invoice.InvoiceNumber} for {invoice.Amount:C2}, due {invoice.DueDate:yyyy-MM-dd}.");
    Pause();
}

static void Payment(CourtBooksService app, CourtBooksStore store)
{
    Console.Clear();
    Console.WriteLine("RECORD PAYMENT");
    Console.WriteLine("--------------");
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

static void Reports(CourtBooksService app, CourtBooksStore store)
{
    Console.Clear();
    Console.WriteLine("REPORTS");
    Console.WriteLine("-------");

    var today = DateTime.Today;
    var upcoming = app.GetLessons(today, today.AddDays(7)).ToList();
    var completedLessons = app.Lessons.Count(x => x.Status == LessonStatus.Completed);
    var scheduledLessons = app.Lessons.Count(x => x.Status == LessonStatus.Scheduled);
    var revenue = app.Invoices.Sum(x => x.AmountPaid);
    var invoiced = app.Invoices.Sum(x => x.Amount);
    var outstanding = app.GetOutstandingBalance();

    Console.WriteLine($"Active students:       {app.Students.Count(x => x.Active)}");
    Console.WriteLine($"Inactive students:     {app.Students.Count(x => !x.Active)}");
    Console.WriteLine($"Scheduled lessons:     {scheduledLessons}");
    Console.WriteLine($"Completed lessons:     {completedLessons}");
    Console.WriteLine($"Upcoming 7-day lessons:{upcoming.Count}");
    Console.WriteLine($"Total invoiced:         {invoiced:C2}");
    Console.WriteLine($"Payments received:     {revenue:C2}");
    Console.WriteLine($"Outstanding balance:   {outstanding:C2}");
    Console.WriteLine($"Overdue invoices:      {app.Invoices.Count(x => x.Status == InvoiceStatus.Overdue)}");

    Console.WriteLine("\nSTUDENT PROGRESS");
    foreach (var student in app.Students.OrderBy(x => x.LastName))
        Console.WriteLine($"{student.FullName,-28} {app.GetProgressPercent(student.Id),3}%");

    Console.WriteLine("\nUPCOMING LESSONS");
    foreach (var lesson in upcoming)
    {
        var student = app.Students.First(x => x.Id == lesson.StudentId);
        Console.WriteLine($"{lesson.Start:ddd dd MMM HH:mm} | {student.FullName,-24} | {lesson.Status}");
    }

    Pause();
}

static void StudentsMini(CourtBooksService app)
{
    foreach (var s in app.Students.Where(x => x.Active))
        Console.WriteLine($"{s.Id}. {s.FullName}");
}

static string Read(string label, string? defaultValue = null)
{
    Console.Write($"{label}{(defaultValue is null ? "" : $" [{defaultValue}")}]: ");
    var value = Console.ReadLine()?.Trim();
    if (string.Equals(value, "m", StringComparison.OrdinalIgnoreCase))
        throw new ReturnToMenuException();
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

static void Seed(CourtBooksStore store)
{
    store.AddStudent(new Student { FirstName = "Alex", LastName = "Naidoo", Phone = "082 555 0101", Email = "alex@example.com", DateOfBirth = new DateOnly(2014, 4, 12) });
    store.AddStudent(new Student { FirstName = "Mia", LastName = "Jacobs", Phone = "082 555 0102", Email = "mia@example.com", DateOfBirth = new DateOnly(2012, 9, 4) });
    SeedCurriculum(store);
}

static void SeedCurriculum(CourtBooksStore store)
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

sealed class ReturnToMenuException : Exception
{
}
