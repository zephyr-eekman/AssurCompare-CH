namespace AssurCompare.Core.Lamal;

public sealed record ComparisonQuery(
    int Region,
    AgeGroup Age,
    bool WithAccident,
    int Franchise,
    IReadOnlySet<TariffType> Types,
    decimal ExpectedHealthCosts = 0);

public sealed record RankedOffer(
    int Rank,
    PremiumOffer Offer,
    decimal ExpectedTotalCost,
    decimal? AnnualSavingVsReference);

public sealed record PremiumStats(int Count, decimal Cheapest, decimal Average, decimal Median, decimal MostExpensive)
{
    /// <summary>Écart annuel entre l'offre la plus chère et la moins chère.</summary>
    public decimal AnnualSpread => (MostExpensive - Cheapest) * 12;
}

/// <summary>Filtre et classe les offres LAMal d'un canton selon le profil de l'assuré.</summary>
public static class PremiumComparer
{
    public static IReadOnlyList<RankedOffer> Rank(
        IEnumerable<PremiumOffer> offers, ComparisonQuery query, PremiumOffer? reference = null)
    {
        return Filter(offers, query)
            .Select(o => (Offer: o, Total: ExpectedTotalCost(o, query.ExpectedHealthCosts)))
            .OrderBy(x => x.Total)
            .ThenBy(x => x.Offer.Insurer.Name)
            .Select((x, i) => new RankedOffer(
                i + 1,
                x.Offer,
                x.Total,
                reference is null ? null : reference.AnnualPremium - x.Offer.AnnualPremium))
            .ToList();
    }

    public static IEnumerable<PremiumOffer> Filter(IEnumerable<PremiumOffer> offers, ComparisonQuery query) =>
        offers.Where(o =>
            o.Region == query.Region
            && o.Age == query.Age
            && o.WithAccident == query.WithAccident
            && o.Franchise == query.Franchise
            && query.Types.Contains(o.Tariff.Type));

    /// <summary>Prime annuelle + franchise et quote-part pour les frais médicaux attendus.</summary>
    public static decimal ExpectedTotalCost(PremiumOffer offer, decimal expectedHealthCosts) =>
        offer.AnnualPremium
        + LamalRules.ComputeCostSharing(expectedHealthCosts, offer.Franchise, offer.Age).Total;

    public static PremiumStats? Stats(IReadOnlyCollection<RankedOffer> ranked)
    {
        if (ranked.Count == 0) return null;

        var premiums = ranked.Select(r => r.Offer.MonthlyPremium).Order().ToArray();
        var middle = premiums.Length / 2;
        var median = premiums.Length % 2 == 1 ? premiums[middle] : (premiums[middle - 1] + premiums[middle]) / 2;

        return new PremiumStats(
            premiums.Length,
            premiums[0],
            Math.Round(premiums.Average(), 2),
            median,
            premiums[^1]);
    }
}
