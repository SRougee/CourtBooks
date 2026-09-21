namespace CourtBooks.Core.Models;

public sealed class Invoice
{
    public int Id { get; init; }
    public int StudentId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Issued;
    public decimal Balance => Amount - AmountPaid;

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Payment must be greater than zero.");
        if (amount > Balance) throw new InvalidOperationException("Payment exceeds invoice balance.");
        AmountPaid += amount;
        Status = Balance == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }

    public void MarkOverdue(DateOnly today)
    {
        if (Balance > 0 && today > DueDate && Status != InvoiceStatus.Cancelled)
            Status = InvoiceStatus.Overdue;
    }
}

public enum InvoiceStatus
{
    Issued,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}