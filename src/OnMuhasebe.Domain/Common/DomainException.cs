namespace OnMuhasebe.Domain.Common;

/// <summary>
/// İş kuralı ihlali. API katmanında 400/409 cevabına çevrilir.
/// </summary>
public class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string message) : DomainException(message);
