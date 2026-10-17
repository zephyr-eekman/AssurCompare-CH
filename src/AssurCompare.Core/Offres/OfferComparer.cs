namespace AssurCompare.Core.Offres;

/// <summary>Une offre d'assurance non-vie (auto, ménage, RC...) saisie par le conseiller.</summary>
public sealed record InsuranceOffer(
    string Name,
    decimal AnnualPremium,
    decimal Deductible,
    decimal CoverageLimit,
    decimal AnnualPremiumIncreasePct = 0);

/// <summary>Hypothèse de sinistralité : combien de sinistres par an, de quel montant moyen.</summary>
public sealed record ClaimScenario(decimal ClaimsPerYear, decimal AverageClaimAmount, int Years);

public sealed record OfferResult(
    int Rank,
    InsuranceOffer Offer,
    decimal OutOfPocketPerClaim,
    decimal TotalPremiums,
    decimal TotalOutOfPocket,
    decimal DifferenceVsBest,
    bool CoverageInsufficient)
{
    /// <summary>Coût total attendu sur la période (primes + ce qui reste à charge).</summary>
    public decimal TotalExpectedCost => TotalPremiums + TotalOutOfPocket;
}

/// <summary>
/// Compare des offres sur leur coût total attendu, pas seulement sur la prime :
/// une prime basse avec une grosse franchise peut coûter plus cher s'il y a des sinistres.
/// </summary>
public static class OfferComparer
{
    public static IReadOnlyList<OfferResult> Compare(IEnumerable<InsuranceOffer> offers, ClaimScenario scenario)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scenario.Years);

        var results = offers
            .Select(offer =>
            {
                var perClaim = OutOfPocketPerClaim(offer, scenario.AverageClaimAmount);
                return new
                {
                    Offer = offer,
                    PerClaim = perClaim,
                    Premiums = TotalPremiums(offer, scenario.Years),
                    OutOfPocket = perClaim * scenario.ClaimsPerYear * scenario.Years,
                    Insufficient = offer.CoverageLimit > 0 && scenario.AverageClaimAmount > offer.CoverageLimit,
                };
            })
            .OrderBy(x => x.Premiums + x.OutOfPocket)
            .ToList();

        if (results.Count == 0) return [];

        var best = results[0].Premiums + results[0].OutOfPocket;
        return results
            .Select((x, i) => new OfferResult(
                i + 1,
                x.Offer,
                Math.Round(x.PerClaim, 2),
                Math.Round(x.Premiums, 2),
                Math.Round(x.OutOfPocket, 2),
                Math.Round(x.Premiums + x.OutOfPocket - best, 2),
                x.Insufficient))
            .ToList();
    }

    /// <summary>Franchise + part du sinistre qui dépasse la somme d'assurance (0 = illimitée).</summary>
    public static decimal OutOfPocketPerClaim(InsuranceOffer offer, decimal claimAmount)
    {
        var deductible = Math.Min(claimAmount, offer.Deductible);
        var aboveLimit = offer.CoverageLimit > 0 ? Math.Max(0, claimAmount - offer.CoverageLimit) : 0;
        return deductible + aboveLimit;
    }

    /// <summary>Somme des primes sur la période, avec une hausse annuelle éventuelle.</summary>
    public static decimal TotalPremiums(InsuranceOffer offer, int years)
    {
        var growth = 1 + offer.AnnualPremiumIncreasePct / 100m;
        decimal total = 0, premium = offer.AnnualPremium;
        for (var year = 0; year < years; year++)
        {
            total += premium;
            premium *= growth;
        }
        return total;
    }
}
