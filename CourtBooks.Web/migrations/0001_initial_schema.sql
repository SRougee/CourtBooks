PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Students (
    Id INTEGER PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Phone TEXT NOT NULL,
    Email TEXT NOT NULL,
    DateOfBirth TEXT NOT NULL,
    Notes TEXT NOT NULL DEFAULT '',
    Active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Curriculum (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL,
    SortOrder INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Lessons (
    Id INTEGER PRIMARY KEY,
    StudentId INTEGER NOT NULL,
    StartUtc TEXT NOT NULL,
    DurationMinutes INTEGER NOT NULL,
    HourlyRate NUMERIC NOT NULL,
    Location TEXT NOT NULL,
    Notes TEXT NOT NULL DEFAULT '',
    Status INTEGER NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id)
);

CREATE TABLE IF NOT EXISTS StudentProgress (
    StudentId INTEGER NOT NULL,
    CurriculumStepId INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    CompletedOn TEXT NULL,
    Notes TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (StudentId, CurriculumStepId),
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (CurriculumStepId) REFERENCES Curriculum(Id)
);

CREATE TABLE IF NOT EXISTS Invoices (
    Id INTEGER PRIMARY KEY,
    StudentId INTEGER NOT NULL,
    InvoiceNumber TEXT NOT NULL UNIQUE,
    IssueDate TEXT NOT NULL,
    DueDate TEXT NOT NULL,
    Amount NUMERIC NOT NULL,
    Status INTEGER NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id)
);

CREATE TABLE IF NOT EXISTS InvoicePayments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceId INTEGER NOT NULL,
    Amount NUMERIC NOT NULL,
    PaidOn TEXT NOT NULL,
    Method INTEGER NOT NULL DEFAULT 4,
    Reference TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
);

INSERT OR IGNORE INTO Curriculum (Id, Name, Category, SortOrder) VALUES
(1, 'Ready Position', 'Foundation', 1),
(2, 'Forehand', 'Groundstrokes', 2),
(3, 'Backhand', 'Groundstrokes', 3),
(4, 'Serve', 'Serve', 4),
(5, 'Volley', 'Net Play', 5),
(6, 'Movement', 'Footwork', 6),
(7, 'Match Play', 'Tactics', 7);

INSERT OR IGNORE INTO Students (Id, FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active) VALUES
(1, 'Alex', 'Naidoo', '082 555 0101', 'alex@example.com', '2014-04-12', '', 1),
(2, 'Mia', 'Jacobs', '082 555 0102', 'mia@example.com', '2012-09-04', '', 1);
