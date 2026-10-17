namespace AssurCompare.Core.Lamal;

public sealed record FranchiseOption(int Franchise, decimal MonthlyPremium)
{
    public decimal AnnualPremium => MonthlyPremium * 12;
}

public sealed record FranchiseCostPoint(decimal HealthCosts, decimal TotalCost);

public sealed record BreakEvenPoint(int LowFranchise, int HighFranchise, decimal? HealthCosts);

/// <summary>
/// Calcule le coût annuel réel d'une franchise (primes + participation aux coûts)
/// et le seuil de rentabilité entre deux franchises.
/// </summary>
public static class FranchiseAnalyzer
{
    public static decimal AnnualTotalCost(FranchiseOption option, decimal healthCosts, AgeGroup group) =>
        option.AnnualPremium + LamalRules.ComputeCostSharing(healthCosts, option.Franchise, group).Total;

    public static FranchiseOption Best(IEnumerable<FranchiseOption> options, decimal healthCosts, AgeGroup group) =>
        options
            .OrderBy(o => AnnualTotalCost(o, healthCosts, group))
            .ThenBy(o => o.Franchise)
            .First();

    public static IReadOnlyList<FranchiseCostPoint> Curve(
        FranchiseOption option, AgeGroup group, decimal maxCosts, decimal step)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);

        var points = new List<FranchiseCostPoint>();
        for (var costs = 0m; costs <= maxCosts; costs += step)
            points.Add(new FranchiseCostPoint(costs, AnnualTotalCost(option, costs, group)));
        return points;
    }

    /// <summary>
    /// Frais médicaux annuels à partir desquels la franchise basse devient plus avantageuse
    /// que la franchise haute. <c>null</c> si la franchise haute reste toujours la moins chère.
    /// </summary>
    public static BreakEvenPoint BreakEven(FranchiseOption low, FranchiseOption high, AgeGroup group)
    {
        if (low.Franchise >= high.Franchise)
            throw new ArgumentException("La première franchise doit être la plus basse.", nameof(low));

        // Écart = coût(haute) - coût(basse). Il ne fait qu'augmenter avec les frais :
        // on peut donc chercher le point où il devient positif par dichotomie.
        decimal Gap(decimal costs) => AnnualTotalCost(high, costs, group) - AnnualTotalCost(low, costs, group);

        if (Gap(0) > 0)
            return new BreakEvenPoint(low.Franchise, high.Franchise, 0);

        var upper = LamalRules.CostsAtMaxParticipation(high.Franchise, group);
        if (Gap(upper) <= 0)
            return new BreakEvenPoint(low.Franchise, high.Franchise, null);

        var lower = 0m;
        while (upper - lower > 0.01m)
        {
            var middle = (lower + upper) / 2;
            if (Gap(middle) > 0) upper = middle;
            else lower = middle;
        }

        return new BreakEvenPoint(low.Franchise, high.Franchise, Math.Round(upper, 0));
    }
}
