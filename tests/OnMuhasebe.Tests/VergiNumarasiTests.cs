using OnMuhasebe.Domain.Common;

namespace OnMuhasebe.Tests;

public class VergiNumarasiTests
{
    [Theory]
    [InlineData("3973535717")]
    [InlineData("2037637860")]
    [InlineData("2823097943")]
    [InlineData("2012460224")]
    public void Gecerli_VKN_kabul_edilir(string vkn)
    {
        var no = VergiNumarasi.Olustur(vkn);
        Assert.Equal(vkn, no.Deger);
        Assert.True(no.TuzelKisiMi);
    }

    [Fact]
    public void Gecerli_TCKN_kabul_edilir()
    {
        var no = VergiNumarasi.Olustur(" 10000000146 ");
        Assert.Equal("10000000146", no.Deger);
        Assert.False(no.TuzelKisiMi);
    }

    [Theory]
    [InlineData("3973535718")]   // kontrol hanesi yanlış
    [InlineData("10000000147")]  // TCKN son hane yanlış
    [InlineData("01234567890")]  // TCKN sıfırla başlayamaz
    [InlineData("12345")]
    [InlineData("39735357ab")]
    [InlineData("")]
    public void Gecersiz_numara_reddedilir(string deger)
    {
        Assert.Throws<DomainException>(() => VergiNumarasi.Olustur(deger));
    }

    [Fact]
    public void Bos_numara_reddedilir()
    {
        Assert.Throws<DomainException>(() => VergiNumarasi.Olustur(null));
    }
}
