namespace AssurCompare.Core.Parametres;

/// <summary>Montants légaux suisses d'une année (AVS, LPP, pilier 3a).</summary>
public sealed record SwissYearParameters(
    int Year,
    decimal Pillar3aWithPensionFund,
    decimal Pillar3aWithoutPensionFund,
    decimal AvsMinMonthlyPension,
    decimal AvsMaxMonthlyPension,
    decimal LppEntryThreshold,
    decimal LppCoordinationDeduction,
    decimal LppMaxCoordinatedSalary,
    decimal LppMinCoordinatedSalary,
    decimal LppMinInterestPct,
    decimal LppConversionRatePct,
    string Source)
{
    /// <summary>Rente AVS annuelle maximale : la base de la plupart des seuils LPP et 3a.</summary>
    public decimal AvsMaxAnnualPension => AvsMaxMonthlyPension * 12;
}

/// <summary>
/// Tous les paramètres annuels en un seul endroit : pour passer à une nouvelle année,
/// on ajoute une entrée ici et on change <see cref="Current"/>.
/// </summary>
public static class SwissParameters
{
    public static readonly SwissYearParameters Y2026 = new(
        Year: 2026,
        Pillar3aWithPensionFund: 7_258m,
        Pillar3aWithoutPensionFund: 36_288m,
        AvsMinMonthlyPension: 1_260m,
        AvsMaxMonthlyPension: 2_520m,
        LppEntryThreshold: 22_680m,
        LppCoordinationDeduction: 26_460m,
        LppMaxCoordinatedSalary: 64_260m,
        LppMinCoordinatedSalary: 3_780m,
        LppMinInterestPct: 1.25m,
        LppConversionRatePct: 6.8m,
        Source: "OFAS, montants valables en 2026");

    public static readonly SwissYearParameters Y2027 = new(
        Year: 2027,
        Pillar3aWithPensionFund: 7_373m,
        Pillar3aWithoutPensionFund: 36_864m,
        AvsMinMonthlyPension: 1_280m,
        AvsMaxMonthlyPension: 2_560m,
        LppEntryThreshold: 23_040m,
        LppCoordinationDeduction: 26_880m,
        LppMaxCoordinatedSalary: 65_280m,
        LppMinCoordinatedSalary: 3_840m,
        LppMinInterestPct: 1.25m,
        LppConversionRatePct: 6.8m,
        Source: "Conseil fédéral, décision du 2 octobre 2026");

    public static IReadOnlyList<SwissYearParameters> All { get; } = [Y2026, Y2027];

    public static SwissYearParameters Current => Y2027;

    public static SwissYearParameters For(int year) =>
        All.FirstOrDefault(p => p.Year == year)
        ?? throw new ArgumentOutOfRangeException(nameof(year), year, "Pas de paramètres pour cette année.");

    /// <summary>La 13e rente AVS (1/12 de la rente annuelle) est versée chaque décembre dès 2026.</summary>
    public const int AvsThirteenthPensionFrom = 2026;

    public const int RetirementAge = 65;
}
