using OnMuhasebe.Domain.Common;

namespace OnMuhasebe.Domain.Faturalar;

public sealed record FaturaSatiri
{
    public static readonly IReadOnlySet<int> GecerliKdvOranlari = new HashSet<int> { 0, 1, 10, 20 };

    public string UrunAdi { get; }
    public decimal Miktar { get; }
    public decimal BirimFiyat { get; }
    public int KdvOrani { get; }
    public decimal IskontoOrani { get; }

    public decimal BrutTutar => Para.Yuvarla(Miktar * BirimFiyat);
    public decimal IskontoTutari => Para.Yuvarla(BrutTutar * IskontoOrani / 100m);
    public decimal NetTutar => BrutTutar - IskontoTutari;
    public decimal KdvTutari => Para.Yuvarla(NetTutar * KdvOrani / 100m);
    public decimal ToplamTutar => NetTutar + KdvTutari;

    public FaturaSatiri(string urunAdi, decimal miktar, decimal birimFiyat, int kdvOrani, decimal iskontoOrani = 0)
    {
        if (string.IsNullOrWhiteSpace(urunAdi))
            throw new DomainException("Ürün adı boş olamaz.");
        if (miktar <= 0)
            throw new DomainException("Miktar sıfırdan büyük olmalıdır.");
        if (birimFiyat < 0)
            throw new DomainException("Birim fiyat negatif olamaz.");
        if (!GecerliKdvOranlari.Contains(kdvOrani))
            throw new DomainException($"KDV oranı {string.Join(", ", GecerliKdvOranlari)} değerlerinden biri olmalıdır.");
        if (iskontoOrani is < 0 or > 100)
            throw new DomainException("İskonto oranı 0 ile 100 arasında olmalıdır.");

        UrunAdi = urunAdi.Trim();
        Miktar = miktar;
        BirimFiyat = birimFiyat;
        KdvOrani = kdvOrani;
        IskontoOrani = iskontoOrani;
    }
}
