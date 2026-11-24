using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;

namespace OnMuhasebe.Api.Auth;

public sealed class KullaniciAyari
{
    public string KullaniciAdi { get; set; } = "";
    public string SifreHash { get; set; } = "";
    public string Rol { get; set; } = Roller.Muhasebe;
}

public static class Roller
{
    public const string Yonetici = "Yonetici";
    public const string Muhasebe = "Muhasebe";
}

public sealed record GirisIstegi(string KullaniciAdi, string Sifre);

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/giris", (GirisIstegi istek, IConfiguration config) =>
        {
            var kullanicilar = config.GetSection("Kullanicilar").Get<List<KullaniciAyari>>() ?? [];
            var kullanici = kullanicilar.FirstOrDefault(k =>
                string.Equals(k.KullaniciAdi, istek.KullaniciAdi, StringComparison.OrdinalIgnoreCase));

            if (kullanici is null || !SifreHasher.Dogrula(istek.Sifre ?? "", kullanici.SifreHash))
                return Results.Problem("Kullanıcı adı veya şifre hatalı.", statusCode: StatusCodes.Status401Unauthorized);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, kullanici.KullaniciAdi), new Claim(ClaimTypes.Role, kullanici.Rol)],
                BearerTokenDefaults.AuthenticationScheme));

            // Cevap: { tokenType, accessToken, expiresIn, refreshToken }
            return Results.SignIn(principal, authenticationScheme: BearerTokenDefaults.AuthenticationScheme);
        })
        .AllowAnonymous()
        .WithTags("Kimlik");
    }
}
