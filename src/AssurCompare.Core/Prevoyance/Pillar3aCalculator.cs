using AssurCompare.Core.Parametres;

namespace AssurCompare.Core.Prevoyance;

public sealed record Pillar3aInput(
    decimal AnnualContribution,
    int Years,
    decimal ExpectedReturnPct,
    decimal MarginalTaxRatePct,
    decimal SavingsAccountRatePct,
    bool HasPensionFund = true,
    int Year = 2026);

public sealed record Pillar3aYear(
    int Year,
    decimal TotalContributions,
    decimal Capital3a,
    decimal TaxSavings,
    decimal CapitalSavingsAccount);

public sealed record Pillar3aResult(IReadOnlyList<Pillar3aYear> Years, bool ExceedsLegalMaximum)
{
    public Pillar3aYear Final => Years[^1];

    /// <summary>Gain des intérêts et du rendement sur la durée.</summary>
    public decimal InvestmentGain => Final.Capital3a - Final.TotalContributions;

    /// <summary>Avantage total du 3a (capital + impôts économisés) par rapport à un compte épargne.</summary>
    public decimal AdvantageVsSavingsAccount => Final.Capital3a + Final.TaxSavings - Final.CapitalSavingsAccount;
}

/// <summary>
/// Simulation simplifiée du pilier 3a : versement en fin d'année, rendement constant,
/// économie d'impôt = versement × taux marginal. L'impôt réduit au retrait n'est pas modélisé.
/// </summary>
public static class Pillar3aCalculator
{
    /// <summary>Montant maximal 2026 pour un salarié affilié à une caisse de pension (LPP).</summary>
    public static decimal MaxWithPensionFund => SwissParameters.Y2026.Pillar3aWithPensionFund;

    /// <summary>Montant maximal 2026 sans 2e pilier (20 % du revenu, plafonné).</summary>
    public static decimal MaxWithoutPensionFund => SwissParameters.Y2026.Pillar3aWithoutPensionFund;

    public static decimal LegalMaximum(bool hasPensionFund, int year = 2026)
    {
        var p = SwissParameters.For(year);
        return hasPensionFund ? p.Pillar3aWithPensionFund : p.Pillar3aWithoutPensionFund;
    }

    public static Pillar3aResult Simulate(Pillar3aInput input)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(input.AnnualContribution);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.Years);

        var returnRate = input.ExpectedReturnPct / 100m;
        var savingsRate = input.SavingsAccountRatePct / 100m;
        var yearlyTaxSaving = input.AnnualContribution * input.MarginalTaxRatePct / 100m;

        decimal capital3a = 0, capitalSavings = 0, contributions = 0, taxSavings = 0;
        var years = new List<Pillar3aYear>(input.Years);

        for (var year = 1; year <= input.Years; year++)
        {
            capital3a = capital3a * (1 + returnRate) + input.AnnualContribution;
            capitalSavings = capitalSavings * (1 + savingsRate) + input.AnnualContribution;
            contributions += input.AnnualContribution;
            taxSavings += yearlyTaxSaving;

            years.Add(new Pillar3aYear(
                year,
                contributions,
                Math.Round(capital3a, 2),
                Math.Round(taxSavings, 2),
                Math.Round(capitalSavings, 2)));
        }

        return new Pillar3aResult(years, input.AnnualContribution > LegalMaximum(input.HasPensionFund, input.Year));
    }
}
