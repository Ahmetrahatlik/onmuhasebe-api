using OnMuhasebe.Api.Auth;
using OnMuhasebe.Application;
using OnMuhasebe.Domain.Common;
using OnMuhasebe.Domain.Faturalar;
using OnMuhasebe.Domain.Tahsilatlar;
using OnMuhasebe.Infrastructure;

namespace OnMuhasebe.Tests;

public class ServisTests
{
    private sealed class SabitSaat(DateOnly bugun) : ISaat
    {
        public DateOnly Bugun { get; } = bugun;
    }

    private readonly InMemoryCariRepository _cariler = new();
    private readonly InMemoryFaturaRepository _faturalar = new();
    private readonly InMemoryTahsilatRepository _tahsilatlar = new();
    private readonly ISaat _saat = new SabitSaat(new DateOnly(2026, 11, 24));

    private CariService CariService => new(_cariler);
    private FaturaService FaturaService => new(_cariler, _faturalar, new InMemoryFaturaNumaratoru(), _saat);

    [Fact]
    public async Task Ayni_vergi_numarasiyla_ikinci_cari_eklenemez()
    {
        await CariService.OlusturAsync(new("3973535717", "Efe Toptan", null, null));
        await Assert.ThrowsAsync<DomainException>(() => CariService.OlusturAsync(new("3973535717", "Başka", null, null)));
    }

    [Fact]
    public async Task Tarihsiz_fatura_bugunun_tarihini_alir()
    {
        var cari = await CariService.OlusturAsync(new("10000000146", "Ali Veli", null, null));
        var f = await FaturaService.TaslakOlusturAsync(new(cari.Id, null, FaturaSenaryosu.Temel,
            [new FaturaSatiriIstegi("Hizmet", 1, 100, 20)]));
        Assert.Equal(new DateOnly(2026, 11, 24), f.Tarih);
        Assert.Equal("EArsiv", f.BelgeTipi);
    }

    [Fact]
    public async Task Olmayan_cariye_fatura_kesilemez()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => FaturaService.TaslakOlusturAsync(
            new(Guid.NewGuid(), null, FaturaSenaryosu.Temel, [new FaturaSatiriIstegi("x", 1, 1, 20)])));
    }

    [Fact]
    public async Task Ekstre_sadece_gonderilmis_faturalari_ve_tahsilatlari_icerir()
    {
        var cari = await CariService.OlusturAsync(new("3973535717", "Efe Toptan", null, "urn:mail:defaultpk@efe.com"));
        var faturaService = FaturaService;

        var gonderilen = await faturaService.TaslakOlusturAsync(new(cari.Id, new DateOnly(2026, 11, 1), FaturaSenaryosu.Temel,
            [new FaturaSatiriIstegi("Vida", 10, 10, 20)]));                 // 100 + 20 KDV = 120
        await faturaService.GonderAsync(gonderilen.Id, new("EFE"));

        await faturaService.TaslakOlusturAsync(new(cari.Id, new DateOnly(2026, 11, 2), FaturaSenaryosu.Temel,
            [new FaturaSatiriIstegi("Taslak", 1, 999, 20)]));               // taslak: ekstreye girmez

        await new TahsilatService(_cariler, _tahsilatlar, _saat)
            .KaydetAsync(new(cari.Id, new DateOnly(2026, 11, 5), 50, OdemeYontemi.Havale, null));

        var ekstre = await new EkstreService(_cariler, _faturalar, _tahsilatlar).OlusturAsync(cari.Id);

        Assert.Equal(2, ekstre.Hareketler.Count);
        Assert.Equal(120m, ekstre.Hareketler[0].Borc);
        Assert.Equal(50m, ekstre.Hareketler[1].Alacak);
        Assert.Equal(70m, ekstre.Bakiye);
    }

    [Fact]
    public void Sifre_hash_dogrulanir_ve_yanlis_sifre_reddedilir()
    {
        var hash = SifreHasher.Hashle("Gizli123!");
        Assert.True(SifreHasher.Dogrula("Gizli123!", hash));
        Assert.False(SifreHasher.Dogrula("gizli123!", hash));
        Assert.False(SifreHasher.Dogrula("Gizli123!", "bozuk-hash"));
        Assert.NotEqual(hash, SifreHasher.Hashle("Gizli123!")); // her seferinde farklı tuz
    }
}
