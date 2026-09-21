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
- `CourtBooks.Core` — domain models, validation, in-memory data store and application services
- `CourtBooks.Console` — interactive command-line application
- `CourtBooks.Tests` — xUnit unit tests

## Getting Started
Install the .NET 10 SDK, then run:

```bash
dotnet build CourtBooks.slnx
dotnet test CourtBooks.slnx
dotnet run --project CourtBooks.Console
```

The MVP uses an in-memory store, so restarting the console resets the sample data.

## Core workflows
1. Add and maintain students.
2. Schedule lessons against students.
3. Review lessons by date range and update lesson status.
4. Track each student's curriculum progress.
5. Issue invoices and record payments.
6. Review outstanding and overdue invoices from the application.

## Next production steps
Persistent database storage, authentication and role-based access, a web/mobile UI, invoice PDF/email delivery, recurring lesson support, reporting, backups, and deployment configuration.
