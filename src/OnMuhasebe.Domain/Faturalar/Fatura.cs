using OnMuhasebe.Domain.Cariler;
using OnMuhasebe.Domain.Common;

namespace OnMuhasebe.Domain.Faturalar;

public enum BelgeTipi { EFatura, EArsiv }

public enum FaturaSenaryosu { Temel, Ticari }

public enum FaturaDurumu { Taslak, Gonderildi, Iptal }

public sealed class Fatura
{
    private readonly List<FaturaSatiri> _satirlar = [];

    public Guid Id { get; } = Guid.NewGuid();
    public Guid CariId { get; }
    public DateOnly Tarih { get; }
    public BelgeTipi BelgeTipi { get; }
    public FaturaSenaryosu? Senaryo { get; }
    public string? AliciEtiketi { get; }
    public FaturaDurumu Durum { get; private set; } = FaturaDurumu.Taslak;
    public FaturaNumarasi? Numara { get; private set; }
    public string? IptalNedeni { get; private set; }

    public IReadOnlyList<FaturaSatiri> Satirlar => _satirlar;

    public decimal NetToplam => _satirlar.Sum(s => s.NetTutar);
    public decimal IskontoToplami => _satirlar.Sum(s => s.IskontoTutari);
    public decimal KdvToplami => _satirlar.Sum(s => s.KdvTutari);
    public decimal GenelToplam => NetToplam + KdvToplami;

    /// <summary>KDV oranına göre matrah ve vergi dökümü (faturadaki KDV tablosu).</summary>
    public IReadOnlyDictionary<int, (decimal Matrah, decimal Kdv)> KdvDokumu =>
        _satirlar.GroupBy(s => s.KdvOrani)
                 .OrderBy(g => g.Key)
                 .ToDictionary(g => g.Key, g => (g.Sum(s => s.NetTutar), g.Sum(s => s.KdvTutari)));

    private Fatura(Guid cariId, DateOnly tarih, BelgeTipi tip, FaturaSenaryosu? senaryo, string? etiket)
    {
        CariId = cariId;
        Tarih = tarih;
        BelgeTipi = tip;
        Senaryo = senaryo;
        AliciEtiketi = etiket;
    }

    /// <summary>
    /// Belge tipini alıcıya göre belirler: alıcı kayıtlı e-Fatura kullanıcısıysa
    /// e-Fatura, değilse e-Arşiv. Senaryo yalnızca e-Fatura için geçerlidir.
    /// </summary>
    public static Fatura Olustur(Cari alici, DateOnly tarih, IEnumerable<FaturaSatiri> satirlar,
                                 FaturaSenaryosu senaryo = FaturaSenaryosu.Temel)
    {
        var fatura = alici.EFaturaMukellefi
            ? new Fatura(alici.Id, tarih, BelgeTipi.EFatura, senaryo, alici.PostaKutusuEtiketi)
            : new Fatura(alici.Id, tarih, BelgeTipi.EArsiv, null, null);

        fatura._satirlar.AddRange(satirlar);
        if (fatura._satirlar.Count == 0)
            throw new DomainException("Fatura en az bir satır içermelidir.");
        return fatura;
    }

    public void Gonder(FaturaNumarasi numara)
    {
        if (Durum != FaturaDurumu.Taslak)
            throw new DomainException("Sadece taslak durumundaki fatura gönderilebilir.");
        if (numara.Yil != Tarih.Year)
            throw new DomainException("Fatura numarasındaki yıl, fatura tarihiyle aynı olmalıdır.");
        Numara = numara;
        Durum = FaturaDurumu.Gonderildi;
    }

    /// <summary>
    /// Gönderilmiş e-Arşiv fatura iptal edilebilir. Alıcıya ulaşmış e-Fatura
    /// sistemden geri çekilemez; red (ticari senaryo) veya iade faturası gerekir.
    /// </summary>
    public void IptalEt(string neden)
    {
        if (string.IsNullOrWhiteSpace(neden))
            throw new DomainException("İptal nedeni zorunludur.");
        if (Durum == FaturaDurumu.Iptal)
            throw new DomainException("Fatura zaten iptal edilmiş.");
        if (Durum == FaturaDurumu.Gonderildi && BelgeTipi == BelgeTipi.EFatura)
            throw new DomainException(
                "Gönderilmiş e-Fatura iptal edilemez. Ticari senaryoda alıcı reddedebilir; aksi halde iade faturası düzenlenmelidir.");

        Durum = FaturaDurumu.Iptal;
        IptalNedeni = neden.Trim();
    }
}
