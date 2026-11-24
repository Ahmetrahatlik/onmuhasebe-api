using System.Text.RegularExpressions;
using OnMuhasebe.Domain.Common;

namespace OnMuhasebe.Domain.Faturalar;

/// <summary>
/// 16 karakterli fatura numarası: 3 karakter seri + 4 hane yıl + 9 hane sıra.
/// Örnek: ABC2026000000001
/// </summary>
public readonly partial record struct FaturaNumarasi
{
    public string Seri { get; }
    public int Yil { get; }
    public long Sira { get; }

    public FaturaNumarasi(string seri, int yil, long sira)
    {
        if (!SeriRegex().IsMatch(seri ?? string.Empty))
            throw new DomainException("Seri 3 karakter olmalı ve harf/rakamdan oluşmalıdır (ör. ABC).");
        if (yil is < 2000 or > 9999)
            throw new DomainException("Geçersiz yıl.");
        if (sira is < 1 or > 999_999_999)
            throw new DomainException("Sıra numarası 1 ile 999999999 arasında olmalıdır.");
        Seri = seri!.ToUpperInvariant();
        Yil = yil;
        Sira = sira;
    }

    public static FaturaNumarasi Coz(string deger)
    {
        if (deger is not { Length: 16 } || !long.TryParse(deger[7..], out var sira) || !int.TryParse(deger[3..7], out var yil))
            throw new DomainException($"Geçersiz fatura numarası: '{deger}'.");
        return new FaturaNumarasi(deger[..3], yil, sira);
    }

    public override string ToString() => $"{Seri}{Yil:D4}{Sira:D9}";

    [GeneratedRegex("^[A-Za-z0-9]{3}$")]
    private static partial Regex SeriRegex();
}
