using OnMuhasebe.Domain.Common;
using OnMuhasebe.Domain.Faturalar;
using OnMuhasebe.Infrastructure;

namespace OnMuhasebe.Tests;

public class FaturaNumarasiTests
{
    [Fact]
    public void Numara_16_karakter_bicimlenir_ve_cozulur()
    {
        var no = new FaturaNumarasi("abc", 2026, 42);
        Assert.Equal("ABC2026000000042", no.ToString());
        Assert.Equal(no, FaturaNumarasi.Coz("ABC2026000000042"));
    }

    [Theory]
    [InlineData("AB", 2026, 1)]
    [InlineData("AB-", 2026, 1)]
    [InlineData("ABC", 2026, 0)]
    [InlineData("ABC", 2026, 1_000_000_000)]
    public void Gecersiz_numara_reddedilir(string seri, int yil, long sira)
    {
        Assert.Throws<DomainException>(() => new FaturaNumarasi(seri, yil, sira));
    }

    [Fact]
    public async Task Numarator_seri_ve_yil_bazinda_ardisik_numara_verir()
    {
        var n = new InMemoryFaturaNumaratoru();
        Assert.Equal("ABC2026000000001", (await n.SonrakiAsync("abc", 2026)).ToString());
        Assert.Equal("ABC2026000000002", (await n.SonrakiAsync("ABC", 2026)).ToString());
        Assert.Equal("XYZ2026000000001", (await n.SonrakiAsync("XYZ", 2026)).ToString());
        Assert.Equal("ABC2027000000001", (await n.SonrakiAsync("ABC", 2027)).ToString());
    }

    [Fact]
    public async Task Gecersiz_seri_sayaci_ilerletmez()
    {
        var n = new InMemoryFaturaNumaratoru();
        await Assert.ThrowsAsync<DomainException>(() => n.SonrakiAsync("A!", 2026));
        Assert.Equal(1, (await n.SonrakiAsync("ABC", 2026)).Sira);
    }

    [Fact]
    public async Task Eszamanli_isteklerde_mukerrer_numara_uretilmez()
    {
        var n = new InMemoryFaturaNumaratoru();
        var gorevler = Enumerable.Range(0, 1000).Select(_ => Task.Run(() => n.SonrakiAsync("ABC", 2026)));
        var numaralar = await Task.WhenAll(gorevler);

        Assert.Equal(1000, numaralar.Select(x => x.Sira).Distinct().Count());
        Assert.Equal(1000, numaralar.Max(x => x.Sira));
    }
}
