namespace OnMuhasebe.Domain.Common;

public static class Para
{
    /// <summary>Kuruş hassasiyetinde, "yarım yukarı" yuvarlama.</summary>
    public static decimal Yuvarla(decimal tutar) => Math.Round(tutar, 2, MidpointRounding.AwayFromZero);
}
