using OnMuhasebe.Domain.Cariler;
using OnMuhasebe.Domain.Faturalar;
using OnMuhasebe.Domain.Tahsilatlar;

namespace OnMuhasebe.Application;

// --- İstekler ---

public sealed record CariOlusturIstegi(string VergiNo, string Unvan, string? Eposta, string? EFaturaEtiketi);

public sealed record FaturaSatiriIstegi(string UrunAdi, decimal Miktar, decimal BirimFiyat, int KdvOrani, decimal IskontoOrani = 0);

public sealed record FaturaOlusturIstegi(
    Guid CariId,
    DateOnly? Tarih,
    FaturaSenaryosu Senaryo,
    IReadOnlyList<FaturaSatiriIstegi> Satirlar);

public sealed record FaturaGonderIstegi(string Seri);

public sealed record FaturaIptalIstegi(string Neden);

public sealed record TahsilatIstegi(Guid CariId, DateOnly? Tarih, decimal Tutar, OdemeYontemi Yontem, string? Aciklama);

// --- Cevaplar ---

public sealed record CariDto(Guid Id, string VergiNo, string Unvan, string? Eposta, bool EFaturaMukellefi, string? EFaturaEtiketi)
{
    public static CariDto From(Cari c) =>
        new(c.Id, c.VergiNo.Deger, c.Unvan, c.Eposta, c.EFaturaMukellefi, c.PostaKutusuEtiketi);
}

public sealed record FaturaSatiriDto(string UrunAdi, decimal Miktar, decimal BirimFiyat, int KdvOrani,
                                     decimal IskontoOrani, decimal NetTutar, decimal KdvTutari, decimal ToplamTutar);

public sealed record KdvDokumuDto(int Oran, decimal Matrah, decimal Kdv);

public sealed record FaturaDto(
    Guid Id,
    Guid CariId,
    string? Numara,
    DateOnly Tarih,
    string BelgeTipi,
    string? Senaryo,
    string? AliciEtiketi,
    string Durum,
    IReadOnlyList<FaturaSatiriDto> Satirlar,
    IReadOnlyList<KdvDokumuDto> KdvDokumu,
    decimal IskontoToplami,
    decimal NetToplam,
    decimal KdvToplami,
    decimal GenelToplam,
    string? IptalNedeni)
{
    public static FaturaDto From(Fatura f) => new(
        f.Id, f.CariId, f.Numara?.ToString(), f.Tarih, f.BelgeTipi.ToString(), f.Senaryo?.ToString(),
        f.AliciEtiketi, f.Durum.ToString(),
        f.Satirlar.Select(s => new FaturaSatiriDto(s.UrunAdi, s.Miktar, s.BirimFiyat, s.KdvOrani, s.IskontoOrani,
                                                   s.NetTutar, s.KdvTutari, s.ToplamTutar)).ToList(),
        f.KdvDokumu.Select(k => new KdvDokumuDto(k.Key, k.Value.Matrah, k.Value.Kdv)).ToList(),
        f.IskontoToplami, f.NetToplam, f.KdvToplami, f.GenelToplam, f.IptalNedeni);
}

public sealed record TahsilatDto(Guid Id, Guid CariId, DateOnly Tarih, decimal Tutar, string Yontem, string? Aciklama)
{
    public static TahsilatDto From(Tahsilat t) => new(t.Id, t.CariId, t.Tarih, t.Tutar, t.Yontem.ToString(), t.Aciklama);
}

public sealed record EkstreSatiri(DateOnly Tarih, string Aciklama, decimal Borc, decimal Alacak, decimal Bakiye);

public sealed record EkstreDto(Guid CariId, string Unvan, IReadOnlyList<EkstreSatiri> Hareketler, decimal Bakiye);
