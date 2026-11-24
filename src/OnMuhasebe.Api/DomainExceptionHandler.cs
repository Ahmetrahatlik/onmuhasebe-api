using Microsoft.AspNetCore.Diagnostics;
using OnMuhasebe.Domain.Common;

namespace OnMuhasebe.Api;

/// <summary>
/// İş kuralı hatalarını RFC 7807 ProblemDetails cevabına çevirir.
/// Bunlar beklenen hatalar olduğu için uyarı seviyesinde loglanır;
/// diğer hatalar varsayılan işleyiciye bırakılır (500, detay gizli).
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, baslik) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Kayıt bulunamadı"),
            DomainException => (StatusCodes.Status400BadRequest, "İş kuralı hatası"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
            _ => (0, ""),
        };
        if (status == 0)
            return false;

        logger.LogWarning("{Baslik}: {Mesaj}", baslik, exception.Message);
        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = baslik, Detail = exception.Message },
        });
    }
}
