namespace OnMuhasebe.Domain.Common;

/// <summary>
/// 10 haneli Vergi Kimlik Numarası (VKN) veya 11 haneli T.C. Kimlik Numarası (TCKN).
/// Oluşturulurken kontrol hanesi algoritmasıyla doğrulanır, böylece sistemde
/// geçersiz numara bulunamaz.
/// </summary>
public readonly record struct VergiNumarasi
{
    public string Deger { get; }
    public bool TuzelKisiMi => Deger.Length == 10;

    private VergiNumarasi(string deger) => Deger = deger;

    public static VergiNumarasi Olustur(string? deger)
    {
        var temiz = (deger ?? string.Empty).Trim();
        var gecerli = temiz.Length switch
        {
            10 => VknGecerliMi(temiz),
            11 => TcknGecerliMi(temiz),
            _ => false,
        };
        if (!gecerli)
            throw new DomainException($"Geçersiz VKN/TCKN: '{temiz}'.");
        return new VergiNumarasi(temiz);
    }

    public static bool VknGecerliMi(string vkn)
    {
        if (vkn.Length != 10 || !vkn.All(char.IsAsciiDigit))
            return false;

        var toplam = 0;
        for (var i = 0; i < 9; i++)
        {
            var tmp = (vkn[i] - '0' + (9 - i)) % 10;
            var v = tmp * (1 << (9 - i)) % 9;
            if (tmp != 0 && v == 0)
                v = 9;
            toplam += v;
        }
        return (10 - toplam % 10) % 10 == vkn[9] - '0';
    }

    public static bool TcknGecerliMi(string tckn)
    {
        if (tckn.Length != 11 || !tckn.All(char.IsAsciiDigit) || tckn[0] == '0')
            return false;

        var d = tckn.Select(c => c - '0').ToArray();
        var tekler = d[0] + d[2] + d[4] + d[6] + d[8];
        var ciftler = d[1] + d[3] + d[5] + d[7];
        var onuncu = ((tekler * 7 - ciftler) % 10 + 10) % 10;
        var onBirinci = d.Take(10).Sum() % 10;
        return d[9] == onuncu && d[10] == onBirinci;
    }

    public override string ToString() => Deger;
}
