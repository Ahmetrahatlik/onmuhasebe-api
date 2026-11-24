using OnMuhasebe.Domain.Cariler;
using OnMuhasebe.Domain.Common;
using OnMuhasebe.Domain.Faturalar;

namespace OnMuhasebe.Tests;

public class FaturaTests
{
    private static readonly DateOnly Tarih = new(2026, 11, 20);

    private static Cari EFaturaCari()
    {
        var cari = new Cari(VergiNumarasi.Olustur("3973535717"), "Efe Toptan Ltd");
        cari.EFaturaKullanicisiOlarakIsaretle("urn:mail:defaultpk@efe.com");
        return cari;
    }

    private static Cari EArsivCari() => new(VergiNumarasi.Olustur("10000000146"), "Ali Veli");

    private static FaturaSatiri[] Satirlar() =>
    [
        new("Vida", miktar: 3, birimFiyat: 33.335m, kdvOrani: 20, iskontoOrani: 10),
        new("Kitap", miktar: 1, birimFiyat: 100m, kdvOrani: 10),
    ];

    [Fact]
    public void Kayitli_aliciya_eFatura_kesilir()
    {
        var f = Fatura.Olustur(EFaturaCari(), Tarih, Satirlar(), FaturaSenaryosu.Ticari);
        Assert.Equal(BelgeTipi.EFatura, f.BelgeTipi);
        Assert.Equal(FaturaSenaryosu.Ticari, f.Senaryo);
        Assert.Equal("urn:mail:defaultpk@efe.com", f.AliciEtiketi);
    }

    [Fact]
    public void Kayitsiz_aliciya_eArsiv_kesilir_ve_senaryo_olmaz()
    {
        var f = Fatura.Olustur(EArsivCari(), Tarih, Satirlar(), FaturaSenaryosu.Ticari);
        Assert.Equal(BelgeTipi.EArsiv, f.BelgeTipi);
        Assert.Null(f.Senaryo);
        Assert.Null(f.AliciEtiketi);
    }

    [Fact]
    public void Tutarlar_kurus_hassasiyetinde_hesaplanir()
    {
        var f = Fatura.Olustur(EArsivCari(), Tarih, Satirlar());

        // 3 x 33,335 = 100,005 → 100,01 ; %10 iskonto = 10,00 ; net 90,01 ; KDV %20 = 18,00
        var vida = f.Satirlar[0];
        Assert.Equal(100.01m, vida.BrutTutar);
        Assert.Equal(10.00m, vida.IskontoTutari);
        Assert.Equal(90.01m, vida.NetTutar);
        Assert.Equal(18.00m, vida.KdvTutari);

        Assert.Equal(190.01m, f.NetToplam);
        Assert.Equal(28.00m, f.KdvToplami);
        Assert.Equal(218.01m, f.GenelToplam);
        Assert.Equal((100m, 10m), f.KdvDokumu[10]);
        Assert.Equal((90.01m, 18.00m), f.KdvDokumu[20]);
    }

    [Theory]
    [InlineData(18)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Gecersiz_KDV_orani_reddedilir(int oran)
    {
        Assert.Throws<DomainException>(() => new FaturaSatiri("Ürün", 1, 10, oran));
    }

    [Fact]
    public void Satirsiz_fatura_olusturulamaz()
    {
        Assert.Throws<DomainException>(() => Fatura.Olustur(EArsivCari(), Tarih, []));
    }

    [Fact]
    public void Gonderilen_eArsiv_iptal_edilebilir()
    {
        var f = Fatura.Olustur(EArsivCari(), Tarih, Satirlar());
        f.Gonder(new FaturaNumarasi("ABC", 2026, 1));
        f.IptalEt("Yanlış tutar");
        Assert.Equal(FaturaDurumu.Iptal, f.Durum);
        Assert.Equal("Yanlış tutar", f.IptalNedeni);
    }

    [Fact]
    public void Gonderilen_eFatura_iptal_edilemez()
    {
        var f = Fatura.Olustur(EFaturaCari(), Tarih, Satirlar());
        f.Gonder(new FaturaNumarasi("ABC", 2026, 1));
        var hata = Assert.Throws<DomainException>(() => f.IptalEt("Hata"));
        Assert.Contains("iade faturası", hata.Message);
    }

    [Fact]
    public void Taslak_eFatura_iptal_edilebilir()
    {
        var f = Fatura.Olustur(EFaturaCari(), Tarih, Satirlar());
        f.IptalEt("Vazgeçildi");
        Assert.Equal(FaturaDurumu.Iptal, f.Durum);
    }

    [Fact]
    public void Fatura_iki_kez_gonderilemez_ve_yil_uyusmali()
    {
        var f = Fatura.Olustur(EArsivCari(), Tarih, Satirlar());
        Assert.Throws<DomainException>(() => f.Gonder(new FaturaNumarasi("ABC", 2025, 1)));
        f.Gonder(new FaturaNumarasi("ABC", 2026, 1));
        Assert.Throws<DomainException>(() => f.Gonder(new FaturaNumarasi("ABC", 2026, 2)));
    }
}
