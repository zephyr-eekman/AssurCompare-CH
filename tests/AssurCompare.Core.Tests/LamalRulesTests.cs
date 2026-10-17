using AssurCompare.Core.Lamal;

namespace AssurCompare.Core.Tests;

public class LamalRulesTests
{
    [Theory]
    [InlineData(0, 300, 0, 0)]
    [InlineData(200, 300, 200, 0)]          // sous la franchise : tout est payé par l'assuré
    [InlineData(1300, 300, 300, 100)]       // 300 de franchise + 10 % de 1000
    [InlineData(20000, 300, 300, 700)]      // quote-part plafonnée à 700
    [InlineData(5000, 2500, 2500, 250)]
    public void Adult_cost_sharing_follows_franchise_then_10_percent(
        decimal costs, int franchise, decimal expectedFranchise, decimal expectedQuotePart)
    {
        var sharing = LamalRules.ComputeCostSharing(costs, franchise, AgeGroup.Adulte);

        Assert.Equal(expectedFranchise, sharing.Franchise);
        Assert.Equal(expectedQuotePart, sharing.QuotePart);
    }

    [Fact]
    public void Child_quote_part_is_capped_at_350()
    {
        var sharing = LamalRules.ComputeCostSharing(50_000, 0, AgeGroup.Enfant);

        Assert.Equal(350, sharing.QuotePart);
        Assert.Equal(350, sharing.Total);
    }

    [Fact]
    public void Negative_costs_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LamalRules.ComputeCostSharing(-1, 300, AgeGroup.Adulte));
    }

    [Fact]
    public void Max_participation_is_reached_at_franchise_plus_7000_for_adults()
    {
        Assert.Equal(7_300, LamalRules.CostsAtMaxParticipation(300, AgeGroup.Adulte));
        Assert.Equal(3_500, LamalRules.CostsAtMaxParticipation(0, AgeGroup.Enfant));
    }
}
