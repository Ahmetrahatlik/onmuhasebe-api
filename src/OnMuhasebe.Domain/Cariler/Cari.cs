using OnMuhasebe.Domain.Common;

namespace OnMuhasebe.Domain.Cariler;

/// <summary>Müşteri / tedarikçi hesabı.</summary>
public sealed class Cari
{
    public Guid Id { get; } = Guid.NewGuid();
    public VergiNumarasi VergiNo { get; }
    public string Unvan { get; private set; }
    public string? Eposta { get; private set; }

    /// <summary>Alıcı GİB'de kayıtlı e-Fatura kullanıcısı mı?</summary>
    public bool EFaturaMukellefi { get; private set; }

    /// <summary>e-Fatura posta kutusu etiketi (ör. urn:mail:defaultpk@firma.com).</summary>
    public string? PostaKutusuEtiketi { get; private set; }

    public Cari(VergiNumarasi vergiNo, string unvan, string? eposta = null)
    {
        if (string.IsNullOrWhiteSpace(unvan))
            throw new DomainException("Ünvan boş olamaz.");
        VergiNo = vergiNo;
        Unvan = unvan.Trim();
        Eposta = eposta?.Trim();
    }

    public void EFaturaKullanicisiOlarakIsaretle(string etiket)
    {
        if (string.IsNullOrWhiteSpace(etiket))
            throw new DomainException("e-Fatura kullanıcısı için posta kutusu etiketi zorunludur.");
        EFaturaMukellefi = true;
        PostaKutusuEtiketi = etiket.Trim();
    }

    public void EFaturaKaydiniKaldir()
    {
        EFaturaMukellefi = false;
        PostaKutusuEtiketi = null;
    }
}
