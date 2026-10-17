using AssurCompare.Core.Parametres;

namespace AssurCompare.Core.Prevoyance;

public sealed record RetirementInput(
    int Age,
    decimal AnnualSalary,
    decimal CurrentLppCapital = 0,
    decimal TargetPctOfSalary = 70,
    int YearsInRetirement = 20,
    decimal Pillar3aReturnPct = 2,
    bool AffiliatedToLpp = true);

public sealed record RetirementResult(
    int YearsToRetirement,
    decimal AvsMonthly,
    decimal AvsMonthlyWithThirteenth,
    decimal CoordinatedSalary,
    decimal LppCapitalAtRetirement,
    decimal LppMonthly,
    decimal TargetMonthly,
    decimal GapMonthly,
    decimal CapitalNeeded,
    decimal Annual3aNeeded,
    decimal Pillar3aMaximum)
{
    public decimal ProjectedMonthly => AvsMonthlyWithThirteenth + LppMonthly;

    public bool HasGap => GapMonthly > 0;

    /// <summary>Le 3a ne suffit pas à lui seul : il faudrait verser plus que le maximum légal.</summary>
    public bool Exceeds3aMaximum => Annual3aNeeded > Pillar3aMaximum;
}

/// <summary>
/// Estimation simplifiée de la lacune de prévoyance d'une personne seule :
/// rente AVS (carrière complète) + rente LPP (minimum légal) comparées à un objectif de revenu.
/// </summary>
public static class RetirementGapCalculator
{
    /// <summary>
    /// Rente AVS mensuelle pour une carrière complète (échelle 44, art. 34 LAVS) :
    /// une part fixe de la rente minimale + une part du revenu annuel moyen, bornée entre minimum et maximum.
    /// </summary>
    public static decimal AvsMonthlyPension(decimal averageAnnualIncome, SwissYearParameters p)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(averageAnnualIncome);

        var min = p.AvsMinMonthlyPension;
        var pension = averageAnnualIncome <= 36 * min
            ? 0.74m * min + 13m / 600m * averageAnnualIncome
            : 1.04m * min + 8m / 600m * averageAnnualIncome;

        return Math.Round(Math.Clamp(pension, min, p.AvsMaxMonthlyPension), 0);
    }

    /// <summary>Avec la 13e rente, l'AVS verse 13 rentes par an : en moyenne mensuelle, la rente × 13/12.</summary>
    public static decimal WithThirteenthPension(decimal monthlyPension) => Math.Round(monthlyPension * 13m / 12m, 2);

    /// <summary>Part du salaire assurée par la LPP obligatoire (salaire coordonné).</summary>
    public static decimal CoordinatedSalary(decimal annualSalary, SwissYearParameters p)
    {
        if (annualSalary < p.LppEntryThreshold) return 0;

        var coordinated = annualSalary - p.LppCoordinationDeduction;
        return Math.Clamp(coordinated, p.LppMinCoordinatedSalary, p.LppMaxCoordinatedSalary);
    }

    /// <summary>Bonifications de vieillesse LPP en % du salaire coordonné, selon l'âge.</summary>
    public static decimal LppCreditRatePct(int age) => age switch
    {
        < 25 => 0,
        < 35 => 7,
        < 45 => 10,
        < 55 => 15,
        _ => 18,
    };

    /// <summary>Capital LPP projeté à la retraite : intérêt sur l'avoir puis bonification de l'année.</summary>
    public static decimal ProjectLppCapital(int age, decimal coordinatedSalary, decimal currentCapital, decimal interestPct)
    {
        var capital = currentCapital;
        for (var a = age; a < SwissParameters.RetirementAge; a++)
            capital = capital * (1 + interestPct / 100m) + coordinatedSalary * LppCreditRatePct(a) / 100m;
        return Math.Round(capital, 0);
    }

    /// <summary>Versement annuel (en fin d'année) nécessaire pour atteindre un capital, au rendement donné.</summary>
    public static decimal AnnualContributionForTarget(decimal futureValue, int years, decimal returnPct)
    {
        if (futureValue <= 0) return 0;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(years);

        var rate = returnPct / 100m;
        if (rate == 0) return Math.Round(futureValue / years, 0);

        var growth = 1m;
        for (var i = 0; i < years; i++) growth *= 1 + rate;
        return Math.Round(futureValue * rate / (growth - 1), 0);
    }

    public static RetirementResult Calculate(RetirementInput input, SwissYearParameters p)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(input.AnnualSalary);

        var years = Math.Max(0, SwissParameters.RetirementAge - input.Age);
        var avs = AvsMonthlyPension(input.AnnualSalary, p);
        var avsWith13th = WithThirteenthPension(avs);

        var coordinated = input.AffiliatedToLpp ? CoordinatedSalary(input.AnnualSalary, p) : 0;
        var lppCapital = input.AffiliatedToLpp
            ? ProjectLppCapital(input.Age, coordinated, input.CurrentLppCapital, p.LppMinInterestPct)
            : 0;
        var lppMonthly = Math.Round(lppCapital * p.LppConversionRatePct / 100m / 12m, 0);

        var target = Math.Round(input.AnnualSalary * input.TargetPctOfSalary / 100m / 12m, 0);
        var gap = Math.Max(0, target - avsWith13th - lppMonthly);

        // Capital à réunir pour verser la lacune chaque mois pendant la retraite (sans rendement, par prudence).
        var capitalNeeded = Math.Round(gap * 12 * input.YearsInRetirement, 0);
        var annual3a = years > 0 ? AnnualContributionForTarget(capitalNeeded, years, input.Pillar3aReturnPct) : 0;
        // Sans 2e pilier : 20 % du revenu, au plus le plafond légal.
        var maximum = coordinated > 0
            ? p.Pillar3aWithPensionFund
            : Math.Min(p.Pillar3aWithoutPensionFund, Math.Round(input.AnnualSalary * 0.20m, 0));

        return new RetirementResult(years, avs, avsWith13th, coordinated, lppCapital, lppMonthly,
            target, gap, capitalNeeded, annual3a, maximum);
    }
}
