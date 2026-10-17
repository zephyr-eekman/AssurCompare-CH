using System.Text.Json.Serialization;

namespace AssurCompare.Core.Lamal;

[JsonConverter(typeof(JsonStringEnumConverter<TariffType>))]
public enum TariffType
{
    /// <summary>BASE : libre choix du médecin.</summary>
    Standard,
    /// <summary>PRAXIS : médecin de famille, HMO, cabinet de santé.</summary>
    MedecinDeFamille,
    /// <summary>TEL_DIG : télémédecine, modèle numérique.</summary>
    Telemedecine,
    /// <summary>FLEX : modèle combiné (pharmacie, médecin ou téléphone au choix).</summary>
    Combine,
}

public static class TariffTypeExtensions
{
    public static TariffType FromOfspCode(string code) => code switch
    {
        "BASE" => TariffType.Standard,
        "PRAXIS" => TariffType.MedecinDeFamille,
        "TEL_DIG" => TariffType.Telemedecine,
        "FLEX" or "PHARM" => TariffType.Combine,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Type de tarif OFSP inconnu."),
    };

    public static string Label(this TariffType type) => type switch
    {
        TariffType.Standard => "Standard (libre choix)",
        TariffType.MedecinDeFamille => "Médecin de famille / HMO",
        TariffType.Telemedecine => "Télémédecine",
        _ => "Modèle combiné",
    };
}

public sealed record InsurerInfo(int Id, string Name, string? Group);

public sealed record CantonInfo(string Code, string Name, int[] Regions);

/// <summary>Fichier <c>data/index.json</c> : ce qui est commun à tous les cantons.</summary>
public sealed class PremiumIndex
{
    public int Year { get; set; }
    public string Source { get; set; } = "";
    public DateOnly GeneratedOn { get; set; }
    public List<CantonInfo> Cantons { get; set; } = [];
    public List<InsurerInfo> Insurers { get; set; } = [];
}

public sealed record TariffInfo(int InsurerId, string Code, TariffType Type, string Name);

/// <summary>
/// Fichier <c>data/{canton}.json</c>. Les primes sont stockées en tableaux compacts
/// pour garder des fichiers légers : [tarif, région, classe d'âge, accident, franchise, prime mensuelle].
/// </summary>
public sealed class CantonPremiums
{
    public string Canton { get; set; } = "";
    public int Year { get; set; }
    public List<TariffInfo> Tariffs { get; set; } = [];
    public List<decimal[]> Rows { get; set; } = [];

    public IEnumerable<PremiumOffer> ToOffers(IReadOnlyDictionary<int, InsurerInfo> insurers)
    {
        foreach (var row in Rows)
        {
            var tariff = Tariffs[(int)row[0]];
            yield return new PremiumOffer(
                insurers[tariff.InsurerId],
                tariff,
                Region: (int)row[1],
                Age: (AgeGroup)(int)row[2],
                WithAccident: row[3] == 1,
                Franchise: (int)row[4],
                MonthlyPremium: row[5]);
        }
    }
}

public sealed record PremiumOffer(
    InsurerInfo Insurer,
    TariffInfo Tariff,
    int Region,
    AgeGroup Age,
    bool WithAccident,
    int Franchise,
    decimal MonthlyPremium)
{
    public decimal AnnualPremium => MonthlyPremium * 12;
}
