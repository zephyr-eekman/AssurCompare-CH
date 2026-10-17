using AssurCompare.Core.Lamal;

namespace AssurCompare.Core.Tests;

public class FranchiseAnalyzerTests
{
    // Primes inventées pour le test : la franchise 2500 coûte 100.- de moins par mois.
    private static readonly FranchiseOption Low = new(300, 500m);
    private static readonly FranchiseOption High = new(2500, 400m);

    [Fact]
    public void Total_cost_is_premiums_plus_cost_sharing()
    {
        // 12 x 500 = 6000 de primes + 300 de franchise + 10 % de 700 = 70
        Assert.Equal(6_370m, FranchiseAnalyzer.AnnualTotalCost(Low, 1_000, AgeGroup.Adulte));
    }

    [Fact]
    public void Without_health_costs_the_highest_franchise_is_cheapest()
    {
        Assert.Equal(2500, FranchiseAnalyzer.Best([Low, High], 0, AgeGroup.Adulte).Franchise);
    }

    [Fact]
    public void With_high_health_costs_the_lowest_franchise_is_cheapest()
    {
        Assert.Equal(300, FranchiseAnalyzer.Best([Low, High], 10_000, AgeGroup.Adulte).Franchise);
    }

    [Fact]
    public void Break_even_is_where_both_franchises_cost_the_same()
    {
        var breakEven = FranchiseAnalyzer.BreakEven(Low, High, AgeGroup.Adulte);

        Assert.NotNull(breakEven.HealthCosts);
        var atBreakEven = breakEven.HealthCosts!.Value;
        var low = FranchiseAnalyzer.AnnualTotalCost(Low, atBreakEven, AgeGroup.Adulte);
        var high = FranchiseAnalyzer.AnnualTotalCost(High, atBreakEven, AgeGroup.Adulte);
        Assert.InRange(high - low, -1m, 1m);
    }

    [Fact]
    public void Break_even_matches_hand_calculation()
    {
        // Économie de prime : 1200.-/an. Entre 300 et 2500 de frais, la franchise haute coûte 0.90 de plus
        // par franc (1.- au lieu de 0.10). 1200 / 0.9 = 1333.33 -> seuil à 300 + 1333 = 1633.
        var breakEven = FranchiseAnalyzer.BreakEven(Low, High, AgeGroup.Adulte);

        Assert.Equal(1_633m, breakEven.HealthCosts);
    }

    [Fact]
    public void No_break_even_when_premium_saving_exceeds_extra_franchise()
    {
        // 2400.-/an d'économie de prime > 2200.- de franchise supplémentaire : la haute gagne toujours.
        var cheapHigh = new FranchiseOption(2500, 300m);

        Assert.Null(FranchiseAnalyzer.BreakEven(Low, cheapHigh, AgeGroup.Adulte).HealthCosts);
    }

    [Fact]
    public void Curve_has_one_point_per_step()
    {
        var curve = FranchiseAnalyzer.Curve(Low, AgeGroup.Adulte, maxCosts: 1_000, step: 250);

        Assert.Equal([0m, 250m, 500m, 750m, 1_000m], curve.Select(p => p.HealthCosts));
    }
}
