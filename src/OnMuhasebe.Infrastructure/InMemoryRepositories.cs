using System.Collections.Concurrent;
using OnMuhasebe.Application;
using OnMuhasebe.Domain.Cariler;
using OnMuhasebe.Domain.Faturalar;
using OnMuhasebe.Domain.Tahsilatlar;

namespace OnMuhasebe.Infrastructure;

// Thread-safe, bellek içi depolar. Demo ve testler içindir; uygulama katmanı
// arayüzleri sayesinde kalıcı bir veritabanı (EF Core vb.) ile değiştirilebilir.

public sealed class InMemoryCariRepository : ICariRepository
{
    private readonly ConcurrentDictionary<Guid, Cari> _items = new();

    public Task<Cari?> GetirAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<Cari?> VergiNoIleGetirAsync(string vergiNo, CancellationToken ct = default) =>
        Task.FromResult(_items.Values.FirstOrDefault(c => c.VergiNo.Deger == vergiNo));

    public Task<IReadOnlyList<Cari>> ListeleAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Cari>>(_items.Values.OrderBy(c => c.Unvan).ToList());

    public Task EkleAsync(Cari cari, CancellationToken ct = default)
    {
        _items[cari.Id] = cari;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryFaturaRepository : IFaturaRepository
{
    private readonly ConcurrentDictionary<Guid, Fatura> _items = new();

    public Task<Fatura?> GetirAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<Fatura>> CariyeGoreListeleAsync(Guid cariId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Fatura>>(_items.Values.Where(f => f.CariId == cariId).OrderBy(f => f.Tarih).ToList());

    public Task EkleAsync(Fatura fatura, CancellationToken ct = default)
    {
        _items[fatura.Id] = fatura;
        return Task.CompletedTask;
    }

    public Task GuncelleAsync(Fatura fatura, CancellationToken ct = default)
    {
        _items[fatura.Id] = fatura;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryTahsilatRepository : ITahsilatRepository
{
    private readonly ConcurrentBag<Tahsilat> _items = [];

    public Task<IReadOnlyList<Tahsilat>> CariyeGoreListeleAsync(Guid cariId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Tahsilat>>(_items.Where(t => t.CariId == cariId).OrderBy(t => t.Tarih).ToList());

    public Task EkleAsync(Tahsilat tahsilat, CancellationToken ct = default)
    {
        _items.Add(tahsilat);
        return Task.CompletedTask;
    }
}

/// <summary>Seri+yıl başına atomik sayaç. Eşzamanlı isteklerde aynı numara iki kez verilmez.</summary>
public sealed class InMemoryFaturaNumaratoru : IFaturaNumaratoru
{
    private readonly ConcurrentDictionary<(string Seri, int Yil), long> _sayaclar = new();

    public Task<FaturaNumarasi> SonrakiAsync(string seri, int yil, CancellationToken ct = default)
    {
        var anahtar = ((seri ?? string.Empty).ToUpperInvariant(), yil);
        // Önce numarayı doğrula: geçersiz seri sayacı ilerletmemeli.
        _ = new FaturaNumarasi(anahtar.Item1, yil, 1);
        var sira = _sayaclar.AddOrUpdate(anahtar, 1, (_, mevcut) => mevcut + 1);
        return Task.FromResult(new FaturaNumarasi(anahtar.Item1, yil, sira));
    }
}

public sealed class SistemSaati : ISaat
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public DateOnly Bugun => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Istanbul));
}
