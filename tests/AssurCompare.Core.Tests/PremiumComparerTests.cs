using AssurCompare.Core.Lamal;

namespace AssurCompare.Core.Tests;

public class PremiumComparerTests
{
    private static readonly InsurerInfo A = new(1, "Caisse A", null);
    private static readonly InsurerInfo B = new(2, "Caisse B", null);
    private static readonly TariffInfo BaseA = new(1, "BASE", TariffType.Standard, "Base");
    private static readonly TariffInfo HmoA = new(1, "HMO", TariffType.MedecinDeFamille, "HMO");
    private static readonly TariffInfo BaseB = new(2, "BASE", TariffType.Standard, "Base");

    private static readonly PremiumOffer[] Offers =
    [
        new(A, BaseA, 0, AgeGroup.Adulte, true, 300, 600m),
        new(A, HmoA, 0, AgeGroup.Adulte, true, 300, 500m),
        new(B, BaseB, 0, AgeGroup.Adulte, true, 300, 550m),
        new(B, BaseB, 0, AgeGroup.Adulte, false, 300, 520m),   // sans accident
        new(B, BaseB, 0, AgeGroup.Adulte, true, 2500, 420m),   // autre franchise
        new(B, BaseB, 0, AgeGroup.Enfant, true, 0, 150m),      // enfant
    ];

    private static ComparisonQuery Query(params TariffType[] types) =>
        new(0, AgeGroup.Adulte, true, 300, types.Length > 0 ? types.ToHashSet() : Enum.GetValues<TariffType>().ToHashSet());

    [Fact]
    public void Ranks_matching_offers_from_cheapest_to_most_expensive()
    {
        var ranked = PremiumComparer.Rank(Offers, Query());

        Assert.Equal([500m, 550m, 600m], ranked.Select(r => r.Offer.MonthlyPremium));
        Assert.Equal([1, 2, 3], ranked.Select(r => r.Rank));
    }

    [Fact]
    public void Filters_by_insurance_model()
    {
        var ranked = PremiumComparer.Rank(Offers, Query(TariffType.Standard));

        Assert.All(ranked, r => Assert.Equal(TariffType.Standard, r.Offer.Tariff.Type));
        Assert.Equal(2, ranked.Count);
    }

    [Fact]
    public void Computes_annual_saving_against_current_insurer()
    {
        var current = Offers[0]; // 600.-/mois
        var ranked = PremiumComparer.Rank(Offers, Query(), current);

        Assert.Equal(1_200m, ranked[0].AnnualSavingVsReference); // (600 - 500) x 12
        Assert.Equal(0m, ranked[^1].AnnualSavingVsReference);
    }

    [Fact]
    public void Expected_total_cost_includes_health_costs()
    {
        var ranked = PremiumComparer.Rank(Offers, Query() with { ExpectedHealthCosts = 1_300 });

        // 500 x 12 + 300 de franchise + 100 de quote-part
        Assert.Equal(6_400m, ranked[0].ExpectedTotalCost);
    }

    [Fact]
    public void Stats_give_spread_between_cheapest_and_most_expensive()
    {
        var stats = PremiumComparer.Stats(PremiumComparer.Rank(Offers, Query()))!;

        Assert.Equal(500m, stats.Cheapest);
        Assert.Equal(550m, stats.Median);
        Assert.Equal(600m, stats.MostExpensive);
        Assert.Equal(1_200m, stats.AnnualSpread);
    }

    [Fact]
    public void Stats_are_null_without_offers()
    {
        Assert.Null(PremiumComparer.Stats([]));
    }
}
