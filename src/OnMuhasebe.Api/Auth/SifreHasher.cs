using System.Security.Cryptography;

namespace OnMuhasebe.Api.Auth;

/// <summary>
/// PBKDF2 (SHA-256) ile şifre özeti. Biçim: "iterasyon.tuz(base64).özet(base64)".
/// Karşılaştırma sabit sürede yapılır (timing saldırılarına karşı).
/// </summary>
public static class SifreHasher
{
    private const int Iterasyon = 100_000;
    private const int TuzBoyu = 16;
    private const int OzetBoyu = 32;

    public static string Hashle(string sifre)
    {
        var tuz = RandomNumberGenerator.GetBytes(TuzBoyu);
        var ozet = Rfc2898DeriveBytes.Pbkdf2(sifre, tuz, Iterasyon, HashAlgorithmName.SHA256, OzetBoyu);
        return $"{Iterasyon}.{Convert.ToBase64String(tuz)}.{Convert.ToBase64String(ozet)}";
    }

    public static bool Dogrula(string sifre, string kayitliHash)
    {
        var parcalar = kayitliHash.Split('.');
        if (parcalar.Length != 3 || !int.TryParse(parcalar[0], out var iterasyon))
            return false;
        try
        {
            var tuz = Convert.FromBase64String(parcalar[1]);
            var beklenen = Convert.FromBase64String(parcalar[2]);
            var gercek = Rfc2898DeriveBytes.Pbkdf2(sifre, tuz, iterasyon, HashAlgorithmName.SHA256, beklenen.Length);
            return CryptographicOperations.FixedTimeEquals(gercek, beklenen);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
