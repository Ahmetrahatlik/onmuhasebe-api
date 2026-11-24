using OnMuhasebe.Api.Auth;
using OnMuhasebe.Application;

namespace OnMuhasebe.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapOnMuhasebe(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        var cariler = api.MapGroup("/cariler").WithTags("Cariler");
        cariler.MapGet("/", (CariService s, CancellationToken ct) => s.ListeleAsync(ct));
        cariler.MapGet("/{id:guid}", (Guid id, CariService s, CancellationToken ct) => s.GetirAsync(id, ct));
        cariler.MapPost("/", async (CariOlusturIstegi istek, CariService s, CancellationToken ct) =>
        {
            var cari = await s.OlusturAsync(istek, ct);
            return Results.Created($"/api/cariler/{cari.Id}", cari);
        });
        cariler.MapGet("/{id:guid}/ekstre", (Guid id, EkstreService s, CancellationToken ct) => s.OlusturAsync(id, ct));

        var faturalar = api.MapGroup("/faturalar").WithTags("Faturalar");
        faturalar.MapGet("/{id:guid}", (Guid id, FaturaService s, CancellationToken ct) => s.GetirAsync(id, ct));
        faturalar.MapPost("/", async (FaturaOlusturIstegi istek, FaturaService s, CancellationToken ct) =>
        {
            var fatura = await s.TaslakOlusturAsync(istek, ct);
            return Results.Created($"/api/faturalar/{fatura.Id}", fatura);
        });
        faturalar.MapPost("/{id:guid}/gonder",
            (Guid id, FaturaGonderIstegi istek, FaturaService s, CancellationToken ct) => s.GonderAsync(id, istek, ct));
        faturalar.MapPost("/{id:guid}/iptal",
            (Guid id, FaturaIptalIstegi istek, FaturaService s, CancellationToken ct) => s.IptalEtAsync(id, istek, ct))
            .RequireAuthorization(p => p.RequireRole(Roller.Yonetici));

        api.MapPost("/tahsilatlar", async (TahsilatIstegi istek, TahsilatService s, CancellationToken ct) =>
        {
            var t = await s.KaydetAsync(istek, ct);
            return Results.Created($"/api/tahsilatlar/{t.Id}", t);
        }).WithTags("Tahsilatlar");
    }
}
