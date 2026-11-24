using OnMuhasebe.Domain.Common;

namespace OnMuhasebe.Domain.Tahsilatlar;

public enum OdemeYontemi { Nakit, Havale, KrediKarti, Cek }

public sealed class Tahsilat
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid CariId { get; }
    public DateOnly Tarih { get; }
    public decimal Tutar { get; }
    public OdemeYontemi Yontem { get; }
    public string? Aciklama { get; }

    public Tahsilat(Guid cariId, DateOnly tarih, decimal tutar, OdemeYontemi yontem, string? aciklama = null)
    {
        if (tutar <= 0)
            throw new DomainException("Tahsilat tutarı sıfırdan büyük olmalıdır.");
        CariId = cariId;
        Tarih = tarih;
        Tutar = Para.Yuvarla(tutar);
        Yontem = yontem;
        Aciklama = aciklama?.Trim();
    }
}
