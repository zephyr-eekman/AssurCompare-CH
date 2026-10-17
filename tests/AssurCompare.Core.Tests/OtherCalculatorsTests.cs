using AssurCompare.Core.Besoins;
using AssurCompare.Core.Offres;
using AssurCompare.Core.Prevoyance;

namespace AssurCompare.Core.Tests;

public class Pillar3aCalculatorTests
{
    [Fact]
    public void Without_return_capital_equals_contributions()
    {
        var result = Pillar3aCalculator.Simulate(new(7_000, 10, 0, 20, 0));

        Assert.Equal(70_000m, result.Final.Capital3a);
        Assert.Equal(70_000m, result.Final.TotalContributions);
        Assert.Equal(14_000m, result.Final.TaxSavings);
    }

    [Fact]
    public void Compound_return_is_applied_each_year()
    {
        // An 1 : 1000. An 2 : 1000 x 1.10 + 1000 = 2100.
        var result = Pillar3aCalculator.Simulate(new(1_000, 2, 10, 0, 0));

        Assert.Equal(2_100m, result.Final.Capital3a);
    }

    [Fact]
    public void Advantage_includes_tax_savings()
    {
        var result = Pillar3aCalculator.Simulate(new(5_000, 1, 0, 25, 0));

        Assert.Equal(1_250m, result.AdvantageVsSavingsAccount);
    }

    [Theory]
    [InlineData(7_258, true, false)]
    [InlineData(7_259, true, true)]
    [InlineData(20_000, false, false)]
    public void Flags_contributions_above_legal_maximum(decimal contribution, bool hasPensionFund, bool exceeds)
    {
        var result = Pillar3aCalculator.Simulate(new(contribution, 1, 0, 0, 0, hasPensionFund));

        Assert.Equal(exceeds, result.ExceedsLegalMaximum);
    }
}

public class OfferComparerTests
{
    private static readonly InsuranceOffer CheapPremium = new("Prime basse", 400, 1_000, 0);
    private static readonly InsuranceOffer LowDeductible = new("Franchise basse", 550, 200, 0);

    [Fact]
    public void Without_claims_the_cheapest_premium_wins()
    {
        var results = OfferComparer.Compare([LowDeductible, CheapPremium], new(0, 0, 3));

        Assert.Equal("Prime basse", results[0].Offer.Name);
        Assert.Equal(450m, results[1].DifferenceVsBest); // 150 x 3 ans
    }

    [Fact]
    public void With_frequent_claims_the_low_deductible_wins()
    {
        // 1 sinistre de 2000.- par an : 400 + 1000 = 1400 contre 550 + 200 = 750 par an.
        var results = OfferComparer.Compare([CheapPremium, LowDeductible], new(1, 2_000, 1));

        Assert.Equal("Franchise basse", results[0].Offer.Name);
        Assert.Equal(750m, results[0].TotalExpectedCost);
    }

    [Fact]
    public void Amount_above_coverage_limit_stays_with_the_client()
    {
        var limited = new InsuranceOffer("Limitée", 300, 200, 5_000);

        Assert.Equal(200m + 3_000m, OfferComparer.OutOfPocketPerClaim(limited, 8_000));
        Assert.True(OfferComparer.Compare([limited], new(1, 8_000, 1))[0].CoverageInsufficient);
    }

    [Fact]
    public void Premium_increase_is_compounded()
    {
        var offer = new InsuranceOffer("Hausse", 1_000, 0, 0, AnnualPremiumIncreasePct: 10);

        Assert.Equal(3_310m, OfferComparer.TotalPremiums(offer, 3)); // 1000 + 1100 + 1210
    }
}

public class NeedsAnalyzerTests
{
    [Fact]
    public void Employee_with_accident_in_lamal_gets_a_saving_hint()
    {
        var report = NeedsAnalyzer.Analyze(new ClientProfile { Work = WorkStatus.Salarie, WeeklyHours = 42, HasAccidentInLamal = true });

        Assert.Contains(report.Gaps, g => g.Title.Contains("Suspendre"));
    }

    [Fact]
    public void Self_employed_needs_loss_of_earnings_and_3a_as_priority()
    {
        var report = NeedsAnalyzer.Analyze(new ClientProfile { Work = WorkStatus.Independant });

        Assert.Contains(report.Gaps, g => g.Title.StartsWith("Perte de gain") && g.Priority == Priority.Haute);
        Assert.Contains(report.Gaps, g => g.Title == "Pilier 3a" && g.Priority == Priority.Haute);
    }

    [Fact]
    public void Car_with_leasing_makes_casco_mandatory()
    {
        var report = NeedsAnalyzer.Analyze(new ClientProfile { HasCar = true, HasLeasing = true });

        Assert.Contains(report.Items, i => i.Title.StartsWith("Casco") && i.Priority == Priority.Obligatoire);
    }

    [Fact]
    public void Fully_covered_client_scores_100()
    {
        var report = NeedsAnalyzer.Analyze(new ClientProfile
        {
            HasAccidentInLamal = false, HasRcPrivee = true, HasMenage = true, Has3a = true,
        });

        Assert.Equal(100, report.CoverageScore);
    }

    [Fact]
    public void Uncovered_tenant_has_a_low_score()
    {
        var report = NeedsAnalyzer.Analyze(new ClientProfile());

        Assert.True(report.CoverageScore < 50);
    }
}
