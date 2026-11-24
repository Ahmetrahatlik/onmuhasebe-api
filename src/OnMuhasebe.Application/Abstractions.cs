using OnMuhasebe.Domain.Cariler;
using OnMuhasebe.Domain.Faturalar;
using OnMuhasebe.Domain.Tahsilatlar;

namespace OnMuhasebe.Application;

// Uygulama katmanı veri erişimini arayüzler üzerinden kullanır.
// Altyapı (Infrastructure) katmanı bunları gerçekler; böylece in-memory
// depodan EF Core / SQL Server'a geçmek sadece altyapı değişikliğidir.

public interface ICariRepository
{
    Task<Cari?> GetirAsync(Guid id, CancellationToken ct = default);
    Task<Cari?> VergiNoIleGetirAsync(string vergiNo, CancellationToken ct = default);
    Task<IReadOnlyList<Cari>> ListeleAsync(CancellationToken ct = default);
    Task EkleAsync(Cari cari, CancellationToken ct = default);
}

public interface IFaturaRepository
{
    Task<Fatura?> GetirAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Fatura>> CariyeGoreListeleAsync(Guid cariId, CancellationToken ct = default);
    Task EkleAsync(Fatura fatura, CancellationToken ct = default);
    Task GuncelleAsync(Fatura fatura, CancellationToken ct = default);
}

public interface ITahsilatRepository
{
    Task<IReadOnlyList<Tahsilat>> CariyeGoreListeleAsync(Guid cariId, CancellationToken ct = default);
    Task EkleAsync(Tahsilat tahsilat, CancellationToken ct = default);
}

/// <summary>Seri ve yıl bazında kesintisiz, tekrarsız fatura numarası üretir.</summary>
public interface IFaturaNumaratoru
{
    Task<FaturaNumarasi> SonrakiAsync(string seri, int yil, CancellationToken ct = default);
}

public interface ISaat
{
    DateOnly Bugun { get; }
}
