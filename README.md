# CourtBooks

Practice management app for solo tennis coaches — student roster, scheduling, hourglass-curriculum progress tracking, and invoicing. Built in C# / .NET 10.

## Implemented MVP
- Student roster management with validation and contact details
- Lesson scheduling with overlap protection, status updates and date-range viewing
- Curriculum / progress tracking using an ordered hourglass-style skills list
- Invoicing with payment terms, payment recording, overdue status and outstanding-balance reporting
- Console dashboard and menu-driven workflow
- Unit tests covering key business rules
- GitHub Actions build-and-test workflow

## Project structure
- `CourtBooks.Core` — domain models, validation, SQLite persistence and application services
- `CourtBooks.Console` — interactive command-line application
- `CourtBooks.Tests` — xUnit unit tests

## Getting Started
Install the .NET 10 SDK, then run:

```bash
dotnet build CourtBooks.slnx
dotnet test CourtBooks.slnx
dotnet run --project CourtBooks.Console
```

## Core workflows
1. Add and maintain students.
2. Schedule lessons against students.
3. Review lessons by date range and update lesson status.
4. Track each student's curriculum progress.
5. Issue invoices and record payments.
6. Review outstanding and overdue invoices from the application.

## Next production steps
Authentication and role-based access, a web/mobile UI, invoice PDF/email delivery, recurring lesson support, richer payment history, backups, and deployment configuration.


## Database setup
CourtBooks now uses SQLite for local persistence. The connection string is stored in `appsettings.json` and defaults to `Data Source=courtbooks.db;Foreign Keys=True`.

On first run the application:
1. Creates the SQLite schema automatically.
2. Seeds the seven curriculum steps and two sample students if the database is empty.
3. Loads existing students, lessons, progress and invoices on later runs.
4. Saves changes back to SQLite after each menu operation.

The generated `courtbooks.db` file is deliberately ignored by Git. The repository contains the reproducible schema and seed script at `Database/setup.sql` instead.

For a fresh manual database, open `Database/setup.sql` with SQLite. For normal use, simply run the console application and it will create the database automatically.

## Console navigation
The console supports a simple return-to-menu workflow. Inside the Students screen use **M. Main menu**, and at any data-entry prompt type **M** to immediately return to the main menu without completing the current operation. This prevents invalid input loops from trapping the user inside a workflow.

## Reports
Option 8 provides a summary of active/inactive students, lesson counts, upcoming lessons, invoiced amounts, payments received, outstanding balances, overdue invoices, and curriculum progress.
