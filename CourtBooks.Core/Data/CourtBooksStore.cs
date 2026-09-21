using System.Globalization;
using Microsoft.Data.Sqlite;
using CourtBooks.Core.Models;

namespace CourtBooks.Core.Data;

public sealed class CourtBooksStore
{
    private int _studentId;
    private int _lessonId;
    private int _invoiceId;
    private string? _connectionString;

    public List<Student> Students { get; } = [];
    public List<Lesson> Lessons { get; } = [];
    public List<CurriculumStep> Curriculum { get; } = [];
    public List<StudentProgress> Progress { get; } = [];
    public List<Invoice> Invoices { get; } = [];
    public List<InvoicePayment> Payments { get; } = [];

    public void ConfigureDatabase(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchema();
        Load();
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        foreach (var invoice in Invoices)
        {
            var recorded = Payments.Where(x => x.InvoiceId == invoice.Id).Sum(x => x.Amount);
            if (invoice.AmountPaid > recorded)
            {
                Payments.Add(new InvoicePayment
                {
                    Id = Payments.Count == 0 ? 1 : Payments.Max(x => x.Id) + 1,
                    InvoiceId = invoice.Id,
                    Amount = invoice.AmountPaid - recorded,
                    PaidOn = DateTime.Now
                });
            }
        }



        Execute(connection, transaction, "DELETE FROM StudentProgress;");
        Execute(connection, transaction, "DELETE FROM Lessons;");
        Execute(connection, transaction, "DELETE FROM InvoicePayments;");
        Execute(connection, transaction, "DELETE FROM Invoices;");
        Execute(connection, transaction, "DELETE FROM Curriculum;");
        Execute(connection, transaction, "DELETE FROM Students;");

        foreach (var s in Students)
            Execute(connection, transaction,
                "INSERT INTO Students (Id, FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active) VALUES ($id,$first,$last,$phone,$email,$dob,$notes,$active);",
                ("$id", s.Id), ("$first", s.FirstName), ("$last", s.LastName), ("$phone", s.Phone),
                ("$email", s.Email), ("$dob", s.DateOfBirth.ToString("yyyy-MM-dd")), ("$notes", s.Notes), ("$active", s.Active ? 1 : 0));

        foreach (var c in Curriculum)
            Execute(connection, transaction,
                "INSERT INTO Curriculum (Id, Name, Category, SortOrder) VALUES ($id,$name,$category,$sort);",
                ("$id", c.Id), ("$name", c.Name), ("$category", c.Category), ("$sort", c.Order));

        foreach (var l in Lessons)
            Execute(connection, transaction,
                "INSERT INTO Lessons (Id, StudentId, StartUtc, DurationMinutes, HourlyRate, Location, Notes, Status) VALUES ($id,$student,$start,$duration,$rate,$location,$notes,$status);",
                ("$id", l.Id), ("$student", l.StudentId), ("$start", l.Start.ToString("O")), ("$duration", l.DurationMinutes),
                ("$rate", l.HourlyRate), ("$location", l.Location), ("$notes", l.Notes), ("$status", (int)l.Status));

        foreach (var p in Progress)
            Execute(connection, transaction,
                "INSERT INTO StudentProgress (StudentId, CurriculumStepId, Status, CompletedOn, Notes) VALUES ($student,$step,$status,$completed,$notes);",
                ("$student", p.StudentId), ("$step", p.CurriculumStepId), ("$status", (int)p.Status),
                ("$completed", p.CompletedOn?.ToString("O")), ("$notes", p.Notes));

        foreach (var i in Invoices)
        {
            Execute(connection, transaction,
                "INSERT INTO Invoices (Id, StudentId, InvoiceNumber, IssueDate, DueDate, Amount, Status) VALUES ($id,$student,$number,$issue,$due,$amount,$status);",
                ("$id", i.Id), ("$student", i.StudentId), ("$number", i.InvoiceNumber),
                ("$issue", i.IssueDate.ToString("yyyy-MM-dd")), ("$due", i.DueDate.ToString("yyyy-MM-dd")),
                ("$amount", i.Amount), ("$status", (int)i.Status));

        }

        foreach (var payment in Payments)
            Execute(connection, transaction,
                "INSERT INTO InvoicePayments (Id, InvoiceId, Amount, PaidOn, Method, Reference) VALUES ($id,$invoice,$amount,$paid,$method,$reference);",
                ("$id", payment.Id), ("$invoice", payment.InvoiceId), ("$amount", payment.Amount), ("$paid", payment.PaidOn.ToString("O")), ("$method", (int)payment.Method), ("$reference", payment.Reference));

        transaction.Commit();
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Execute(connection, null, @"CREATE TABLE IF NOT EXISTS Students (
            Id INTEGER PRIMARY KEY, FirstName TEXT NOT NULL, LastName TEXT NOT NULL,
            Phone TEXT NOT NULL, Email TEXT NOT NULL, DateOfBirth TEXT NOT NULL,
            Notes TEXT NOT NULL, Active INTEGER NOT NULL);");
        Execute(connection, null, @"CREATE TABLE IF NOT EXISTS Curriculum (
            Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Category TEXT NOT NULL, SortOrder INTEGER NOT NULL);");
        Execute(connection, null, @"CREATE TABLE IF NOT EXISTS Lessons (
            Id INTEGER PRIMARY KEY, StudentId INTEGER NOT NULL, StartUtc TEXT NOT NULL,
            DurationMinutes INTEGER NOT NULL, HourlyRate NUMERIC NOT NULL,
            Location TEXT NOT NULL, Notes TEXT NOT NULL, Status INTEGER NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES Students(Id));");
        Execute(connection, null, @"CREATE TABLE IF NOT EXISTS StudentProgress (
            StudentId INTEGER NOT NULL, CurriculumStepId INTEGER NOT NULL, Status INTEGER NOT NULL,
            CompletedOn TEXT NULL, Notes TEXT NOT NULL,
            PRIMARY KEY(StudentId, CurriculumStepId),
            FOREIGN KEY(StudentId) REFERENCES Students(Id),
            FOREIGN KEY(CurriculumStepId) REFERENCES Curriculum(Id));");
        Execute(connection, null, @"CREATE TABLE IF NOT EXISTS Invoices (
            Id INTEGER PRIMARY KEY, StudentId INTEGER NOT NULL, InvoiceNumber TEXT NOT NULL UNIQUE,
            IssueDate TEXT NOT NULL, DueDate TEXT NOT NULL, Amount NUMERIC NOT NULL, Status INTEGER NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES Students(Id));");
        Execute(connection, null, @"CREATE TABLE IF NOT EXISTS InvoicePayments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, InvoiceId INTEGER NOT NULL, Amount NUMERIC NOT NULL,
            PaidOn TEXT NOT NULL, Method INTEGER NOT NULL DEFAULT 4, Reference TEXT NOT NULL DEFAULT '',
            FOREIGN KEY(InvoiceId) REFERENCES Invoices(Id));");

        if (!HasColumn(connection, "InvoicePayments", "Method"))
            Execute(connection, null, "ALTER TABLE InvoicePayments ADD COLUMN Method INTEGER NOT NULL DEFAULT 4;");
        if (!HasColumn(connection, "InvoicePayments", "Reference"))
            Execute(connection, null, "ALTER TABLE InvoicePayments ADD COLUMN Reference TEXT NOT NULL DEFAULT '';");
    }

    private static bool HasColumn(SqliteConnection connection, string table, string column)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private void Load()
    {
        Students.Clear(); Lessons.Clear(); Curriculum.Clear(); Progress.Clear(); Invoices.Clear(); Payments.Clear();
        _studentId = _lessonId = _invoiceId = 0;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id,FirstName,LastName,Phone,Email,DateOfBirth,Notes,Active FROM Students ORDER BY Id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Students.Add(new Student {
                    Id = r.GetInt32(0), FirstName = r.GetString(1), LastName = r.GetString(2),
                    Phone = r.GetString(3), Email = r.GetString(4),
                    DateOfBirth = DateOnly.Parse(r.GetString(5)), Notes = r.GetString(6), Active = r.GetInt32(7) == 1 });
            }
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id,Name,Category,SortOrder FROM Curriculum ORDER BY SortOrder";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                Curriculum.Add(new CurriculumStep { Id = r.GetInt32(0), Name = r.GetString(1), Category = r.GetString(2), Order = r.GetInt32(3) });
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id,StudentId,StartUtc,DurationMinutes,HourlyRate,Location,Notes,Status FROM Lessons ORDER BY StartUtc";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                Lessons.Add(new Lesson { Id = r.GetInt32(0), StudentId = r.GetInt32(1), Start = DateTime.Parse(r.GetString(2), null, DateTimeStyles.RoundtripKind),
                    DurationMinutes = r.GetInt32(3), HourlyRate = r.GetDecimal(4), Location = r.GetString(5), Notes = r.GetString(6), Status = (LessonStatus)r.GetInt32(7) });
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT StudentId,CurriculumStepId,Status,CompletedOn,Notes FROM StudentProgress";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                Progress.Add(new StudentProgress { StudentId = r.GetInt32(0), CurriculumStepId = r.GetInt32(1),
                    Status = (ProgressStatus)r.GetInt32(2), CompletedOn = r.IsDBNull(3) ? null : DateTime.Parse(r.GetString(3), null, DateTimeStyles.RoundtripKind), Notes = r.GetString(4) });
        }

        var overdueInvoiceIds = new HashSet<int>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id,StudentId,InvoiceNumber,IssueDate,DueDate,Amount,Status FROM Invoices ORDER BY Id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var invoice = new Invoice { Id = r.GetInt32(0), StudentId = r.GetInt32(1), InvoiceNumber = r.GetString(2),
                    IssueDate = DateOnly.Parse(r.GetString(3)), DueDate = DateOnly.Parse(r.GetString(4)), Amount = r.GetDecimal(5) };
                var status = (InvoiceStatus)r.GetInt32(6);
                if (status == InvoiceStatus.Overdue) overdueInvoiceIds.Add(invoice.Id);
                Invoices.Add(invoice);
            }
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id,InvoiceId,Amount,PaidOn,Method,Reference FROM InvoicePayments ORDER BY Id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var payment = new InvoicePayment
                {
                    Id = r.GetInt32(0),
                    InvoiceId = r.GetInt32(1),
                    Amount = Convert.ToDecimal(r.GetValue(2), CultureInfo.InvariantCulture),
                    PaidOn = DateTime.Parse(r.GetString(3), null, DateTimeStyles.RoundtripKind),
                    Method = (PaymentMethod)r.GetInt32(4),
                    Reference = r.GetString(5)
                };
                Payments.Add(payment);
                var invoice = Invoices.SingleOrDefault(x => x.Id == payment.InvoiceId);
                invoice?.RecordPayment(payment.Amount);
            }
        }

        foreach (var invoiceId in overdueInvoiceIds)
        {
            var invoice = Invoices.SingleOrDefault(x => x.Id == invoiceId);
            invoice?.MarkOverdue(invoice.DueDate.AddDays(1));
        }

        _studentId = Students.Count == 0 ? 0 : Students.Max(x => x.Id);
        _lessonId = Lessons.Count == 0 ? 0 : Lessons.Max(x => x.Id);
        _invoiceId = Invoices.Count == 0 ? 0 : Invoices.Max(x => x.Id);
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public Student AddStudent(Student student)
    {
        if (string.IsNullOrWhiteSpace(student.FirstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(student.LastName)) throw new ArgumentException("Last name is required.");
        ValidateDateOfBirth(student.DateOfBirth);
        if (!string.IsNullOrWhiteSpace(student.Email) && !student.Email.Contains('@')) throw new ArgumentException("Email address is invalid.");
        EnsureStudentIsUnique(student.Email, student.Phone, null);

        var saved = new Student { Id = ++_studentId, FirstName = student.FirstName.Trim(), LastName = student.LastName.Trim(),
            Phone = student.Phone.Trim(), Email = student.Email.Trim(), DateOfBirth = student.DateOfBirth, Notes = student.Notes.Trim(), Active = true };
        Students.Add(saved);
        return saved;
    }

    public Student UpdateStudent(int studentId, string firstName, string lastName, string phone, string email, DateOnly dateOfBirth, string notes)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.");
        ValidateDateOfBirth(dateOfBirth);
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) throw new ArgumentException("Email address is invalid.");
        EnsureStudentIsUnique(email, phone, studentId);

        var student = Students.SingleOrDefault(x => x.Id == studentId) ?? throw new KeyNotFoundException("Student not found.");
        student.FirstName = firstName.Trim();
        student.LastName = lastName.Trim();
        student.Phone = phone.Trim();
        student.Email = email.Trim();
        student.DateOfBirth = dateOfBirth;
        student.Notes = notes.Trim();
        return student;
    }

    public void SetStudentActive(int studentId, bool active)
    {
        var student = Students.SingleOrDefault(x => x.Id == studentId) ?? throw new KeyNotFoundException("Student not found.");
        student.Active = active;
    }

    public Lesson AddLesson(Lesson lesson)
    {
        var student = Students.SingleOrDefault(s => s.Id == lesson.StudentId) ?? throw new ArgumentException("Student does not exist.");
        if (!student.Active) throw new InvalidOperationException("Inactive students cannot be scheduled for new lessons.");
        if (lesson.Start <= DateTime.Now) throw new ArgumentException("New lessons must be scheduled in the future.");
        if (lesson.DurationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(lesson.DurationMinutes));
        if (lesson.HourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(lesson.HourlyRate));
        if (Lessons.Any(x => x.Status == LessonStatus.Scheduled && x.Start < lesson.Start.AddMinutes(lesson.DurationMinutes) && x.End > lesson.Start))
            throw new InvalidOperationException("The coach already has a lesson during this time.");

        var saved = new Lesson { Id = ++_lessonId, StudentId = lesson.StudentId, Start = lesson.Start,
            DurationMinutes = lesson.DurationMinutes, HourlyRate = lesson.HourlyRate, Location = lesson.Location.Trim(), Notes = lesson.Notes.Trim(), Status = LessonStatus.Scheduled };
        Lessons.Add(saved);
        return saved;
    }

    private void ValidateDateOfBirth(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dateOfBirth > today) throw new ArgumentException("Date of birth cannot be in the future.");
        if (dateOfBirth < today.AddYears(-100)) throw new ArgumentException("Date of birth is outside the supported age range.");
    }

    private void EnsureStudentIsUnique(string email, string phone, int? excludeId)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedPhone = NormalizePhone(phone);
        if (!string.IsNullOrEmpty(normalizedEmail) &&
            Students.Any(s => s.Id != excludeId && !string.IsNullOrWhiteSpace(s.Email) &&
                              s.Email.Trim().Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Another student already uses this email address.");
        if (!string.IsNullOrEmpty(normalizedPhone) &&
            Students.Any(s => s.Id != excludeId && NormalizePhone(s.Phone) == normalizedPhone))
            throw new InvalidOperationException("Another student already uses this phone number.");
    }

    private static string NormalizePhone(string phone)
        => new string(phone.Where(char.IsDigit).ToArray());

    public Invoice CreateInvoice(int studentId, decimal amount, DateOnly issueDate, int paymentTermsDays = 30)
    {
        var student = Students.SingleOrDefault(s => s.Id == studentId) ?? throw new ArgumentException("Student does not exist.");
        if (!student.Active) throw new InvalidOperationException("Inactive students cannot receive new invoices.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays));

        var number = $"CB-{issueDate:yyyyMMdd}-{_invoiceId + 1:0000}";
        var invoice = new Invoice { Id = ++_invoiceId, StudentId = studentId, InvoiceNumber = number,
            IssueDate = issueDate, DueDate = issueDate.AddDays(paymentTermsDays), Amount = decimal.Round(amount, 2) };
        Invoices.Add(invoice);
        return invoice;
    }
}