using AssurCompare.Core.Parametres;
using AssurCompare.Core.Prevoyance;

namespace AssurCompare.Core.Tests;

public class RetirementGapCalculatorTests
{
    private static readonly SwissYearParameters P = SwissParameters.Y2027;

    [Theory]
    [InlineData(0, 1_280)]          // revenu très bas : rente minimale
    [InlineData(15_360, 1_280)]     // 12 x rente minimale : encore la rente minimale
    [InlineData(60_000, 2_131)]     // 1.04 x 1280 + 8/600 x 60'000 = 2131.20
    [InlineData(92_160, 2_560)]     // 72 x rente minimale : rente maximale
    [InlineData(200_000, 2_560)]    // plafonnée
    public void Avs_pension_follows_the_official_scale(decimal income, decimal expected)
    {
        Assert.Equal(expected, RetirementGapCalculator.AvsMonthlyPension(income, P));
    }

    [Fact]
    public void Avs_scale_is_continuous_at_the_breakpoint()
    {
        // À 36 x rente minimale (46'080), les deux formules donnent 1945.60.
        Assert.Equal(1_946m, RetirementGapCalculator.AvsMonthlyPension(46_080, P));
    }

    [Fact]
    public void Thirteenth_pension_adds_one_twelfth()
    {
        Assert.Equal(2_773.33m, RetirementGapCalculator.WithThirteenthPension(2_560));
    }

    [Theory]
    [InlineData(20_000, 0)]          // sous le seuil d'entrée de 23'040 : pas de LPP
    [InlineData(25_000, 3_840)]      // salaire coordonné minimal
    [InlineData(60_000, 33_120)]     // 60'000 - 26'880
    [InlineData(150_000, 65_280)]    // plafonné
    public void Coordinated_salary_uses_2027_thresholds(decimal salary, decimal expected)
    {
        Assert.Equal(expected, RetirementGapCalculator.CoordinatedSalary(salary, P));
    }

    [Theory]
    [InlineData(24, 0)]
    [InlineData(25, 7)]
    [InlineData(34, 7)]
    [InlineData(35, 10)]
    [InlineData(45, 15)]
    [InlineData(55, 18)]
    [InlineData(64, 18)]
    public void Lpp_credit_rates_depend_on_age(int age, decimal expected)
    {
        Assert.Equal(expected, RetirementGapCalculator.LppCreditRatePct(age));
    }

    [Fact]
    public void Lpp_capital_sums_credits_without_interest()
    {
        // 10 ans à 7 %, 10 à 10 %, 10 à 15 %, 10 à 18 % de 10'000.-
        Assert.Equal(50_000m, RetirementGapCalculator.ProjectLppCapital(25, 10_000, 0, 0));
    }

    [Fact]
    public void Contribution_for_target_is_the_inverse_of_compounding()
    {
        // Le calculateur 3a donne 2100 après 2 versements de 1000 à 10 % : l'inverse doit redonner 1000.
        Assert.Equal(1_000m, RetirementGapCalculator.AnnualContributionForTarget(2_100, 2, 10));
        Assert.Equal(250m, RetirementGapCalculator.AnnualContributionForTarget(1_000, 4, 0));
    }

    [Fact]
    public void Gap_is_target_minus_avs_and_lpp()
    {
        var result = RetirementGapCalculator.Calculate(new RetirementInput(Age: 30, AnnualSalary: 80_000), P);

        Assert.Equal(Math.Round(80_000m * 0.70m / 12, 0), result.TargetMonthly);
        Assert.Equal(result.TargetMonthly - result.ProjectedMonthly, result.GapMonthly);
        Assert.True(result.Annual3aNeeded > 0);
        Assert.Equal(35, result.YearsToRetirement);
    }

    [Fact]
    public void Low_salary_has_no_gap_thanks_to_avs()
    {
        var result = RetirementGapCalculator.Calculate(new RetirementInput(Age: 40, AnnualSalary: 20_000), P);

        Assert.Equal(0, result.CoordinatedSalary);
        Assert.False(result.HasGap);
        Assert.Equal(0, result.Annual3aNeeded);
    }

    [Theory]
    [InlineData(95_000, 19_000)]     // 20 % du revenu
    [InlineData(300_000, 36_864)]    // plafonné
    public void Pillar3a_maximum_without_lpp_is_20_percent_of_income(decimal salary, decimal expected)
    {
        var result = RetirementGapCalculator.Calculate(new RetirementInput(Age: 50, AnnualSalary: salary, AffiliatedToLpp: false), P);

        Assert.Equal(expected, result.Pillar3aMaximum);
    }

    [Fact]
    public void Pillar3a_maximum_follows_the_selected_year()
    {
        Assert.Equal(7_258m, Pillar3aCalculator.LegalMaximum(true, 2026));
        Assert.Equal(7_373m, Pillar3aCalculator.LegalMaximum(true, 2027));
        Assert.Equal(36_864m, Pillar3aCalculator.LegalMaximum(false, 2027));
    }
}
