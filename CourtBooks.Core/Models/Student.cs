namespace CourtBooks.Core.Models;

public sealed class Student
{
    public int Id { get; init; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Notes { get; set; } = "";
    public bool Active { get; set; } = true;
}