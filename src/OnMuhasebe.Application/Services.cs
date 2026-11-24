using OnMuhasebe.Domain.Cariler;
using OnMuhasebe.Domain.Common;
using OnMuhasebe.Domain.Faturalar;
using OnMuhasebe.Domain.Tahsilatlar;

namespace OnMuhasebe.Application;

public sealed class CariService(ICariRepository cariler)
{
    public async Task<CariDto> OlusturAsync(CariOlusturIstegi istek, CancellationToken ct = default)
    {
        var vergiNo = VergiNumarasi.Olustur(istek.VergiNo);
        if (await cariler.VergiNoIleGetirAsync(vergiNo.Deger, ct) is not null)
            throw new DomainException($"{vergiNo} vergi numaralı cari zaten kayıtlı.");

        var cari = new Cari(vergiNo, istek.Unvan, istek.Eposta);
        if (!string.IsNullOrWhiteSpace(istek.EFaturaEtiketi))
            cari.EFaturaKullanicisiOlarakIsaretle(istek.EFaturaEtiketi);

        await cariler.EkleAsync(cari, ct);
        return CariDto.From(cari);
    }

    public async Task<CariDto> GetirAsync(Guid id, CancellationToken ct = default) =>
        CariDto.From(await cariler.GetirAsync(id, ct) ?? throw new NotFoundException("Cari bulunamadı."));

    public async Task<IReadOnlyList<CariDto>> ListeleAsync(CancellationToken ct = default) =>
        (await cariler.ListeleAsync(ct)).Select(CariDto.From).ToList();
}

public sealed class FaturaService(
    ICariRepository cariler,
    IFaturaRepository faturalar,
    IFaturaNumaratoru numarator,
    ISaat saat)
{
    public async Task<FaturaDto> TaslakOlusturAsync(FaturaOlusturIstegi istek, CancellationToken ct = default)
    {
        var cari = await cariler.GetirAsync(istek.CariId, ct) ?? throw new NotFoundException("Cari bulunamadı.");
        var satirlar = (istek.Satirlar ?? [])
            .Select(s => new FaturaSatiri(s.UrunAdi, s.Miktar, s.BirimFiyat, s.KdvOrani, s.IskontoOrani));

        var fatura = Fatura.Olustur(cari, istek.Tarih ?? saat.Bugun, satirlar, istek.Senaryo);
        await faturalar.EkleAsync(fatura, ct);
        return FaturaDto.From(fatura);
    }

    public async Task<FaturaDto> GetirAsync(Guid id, CancellationToken ct = default) =>
        FaturaDto.From(await Bul(id, ct));

    public async Task<FaturaDto> GonderAsync(Guid id, FaturaGonderIstegi istek, CancellationToken ct = default)
    {
        var fatura = await Bul(id, ct);
        if (fatura.Durum != FaturaDurumu.Taslak)
            throw new DomainException("Sadece taslak durumundaki fatura gönderilebilir.");

        // Numara sadece gönderimde alınır: iptal edilen taslaklar numara serisinde boşluk bırakmaz.
        var numara = await numarator.SonrakiAsync(istek.Seri, fatura.Tarih.Year, ct);
        fatura.Gonder(numara);
        await faturalar.GuncelleAsync(fatura, ct);
        return FaturaDto.From(fatura);
    }

    public async Task<FaturaDto> IptalEtAsync(Guid id, FaturaIptalIstegi istek, CancellationToken ct = default)
    {
        var fatura = await Bul(id, ct);
        fatura.IptalEt(istek.Neden);
        await faturalar.GuncelleAsync(fatura, ct);
        return FaturaDto.From(fatura);
    }

    private async Task<Fatura> Bul(Guid id, CancellationToken ct) =>
        await faturalar.GetirAsync(id, ct) ?? throw new NotFoundException("Fatura bulunamadı.");
}

public sealed class TahsilatService(ICariRepository cariler, ITahsilatRepository tahsilatlar, ISaat saat)
{
    public async Task<TahsilatDto> KaydetAsync(TahsilatIstegi istek, CancellationToken ct = default)
    {
        _ = await cariler.GetirAsync(istek.CariId, ct) ?? throw new NotFoundException("Cari bulunamadı.");
        var tahsilat = new Tahsilat(istek.CariId, istek.Tarih ?? saat.Bugun, istek.Tutar, istek.Yontem, istek.Aciklama);
        await tahsilatlar.EkleAsync(tahsilat, ct);
        return TahsilatDto.From(tahsilat);
    }
}

/// <summary>
/// Cari hesap ekstresi: gönderilmiş faturalar borç, tahsilatlar alacak yazılır.
/// Taslak ve iptal edilmiş faturalar bakiyeyi etkilemez.
/// </summary>
public sealed class EkstreService(ICariRepository cariler, IFaturaRepository faturalar, ITahsilatRepository tahsilatlar)
{
    public async Task<EkstreDto> OlusturAsync(Guid cariId, CancellationToken ct = default)
    {
        var cari = await cariler.GetirAsync(cariId, ct) ?? throw new NotFoundException("Cari bulunamadı.");

        var borclar = (await faturalar.CariyeGoreListeleAsync(cariId, ct))
            .Where(f => f.Durum == FaturaDurumu.Gonderildi)
            .Select(f => (f.Tarih, Aciklama: $"{f.BelgeTipi} {f.Numara}", Borc: f.GenelToplam, Alacak: 0m));
        var alacaklar = (await tahsilatlar.CariyeGoreListeleAsync(cariId, ct))
            .Select(t => (t.Tarih, Aciklama: $"Tahsilat ({t.Yontem})", Borc: 0m, Alacak: t.Tutar));

        var bakiye = 0m;
        var hareketler = new List<EkstreSatiri>();
        foreach (var h in borclar.Concat(alacaklar).OrderBy(h => h.Tarih).ThenByDescending(h => h.Borc))
        {
            bakiye += h.Borc - h.Alacak;
            hareketler.Add(new EkstreSatiri(h.Tarih, h.Aciklama, h.Borc, h.Alacak, bakiye));
        }
        return new EkstreDto(cari.Id, cari.Unvan, hareketler, bakiye);
    }
}
