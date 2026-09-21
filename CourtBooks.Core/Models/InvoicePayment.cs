namespace CourtBooks.Core.Models;

public sealed class InvoicePayment
{
    public int Id { get; init; }
    public int InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public DateTime PaidOn { get; init; }
    public PaymentMethod Method { get; init; } = PaymentMethod.Other;
    public string Reference { get; init; } = "";
}
