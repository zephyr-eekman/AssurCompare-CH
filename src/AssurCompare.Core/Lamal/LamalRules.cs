namespace AssurCompare.Core.Lamal;

/// <summary>Classe d'âge LAMal (selon l'OFSP : enfant 0-18, jeune adulte 19-25, adulte 26+).</summary>
public enum AgeGroup
{
    Enfant = 0,
    JeuneAdulte = 1,
    Adulte = 2,
}

/// <summary>Montants payés par l'assuré pour une année de frais médicaux.</summary>
public readonly record struct CostSharing(decimal Franchise, decimal QuotePart)
{
    public decimal Total => Franchise + QuotePart;
}

/// <summary>
/// Règles légales de la participation aux coûts LAMal (art. 64 LAMal, art. 93 et 103 OAMal) :
/// l'assuré paie d'abord sa franchise, puis une quote-part de 10 % du reste, plafonnée.
/// </summary>
public static class LamalRules
{
    public static readonly IReadOnlyList<int> AdultFranchises = [300, 500, 1000, 1500, 2000, 2500];
    public static readonly IReadOnlyList<int> ChildFranchises = [0, 100, 200, 300, 400, 500, 600];

    public const decimal QuotePartRate = 0.10m;

    public static decimal MaxQuotePart(AgeGroup group) => group == AgeGroup.Enfant ? 350m : 700m;

    public static IReadOnlyList<int> Franchises(AgeGroup group) =>
        group == AgeGroup.Enfant ? ChildFranchises : AdultFranchises;

    public static int DefaultFranchise(AgeGroup group) => group == AgeGroup.Enfant ? 0 : 300;

    public static CostSharing ComputeCostSharing(decimal healthCosts, int franchise, AgeGroup group)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(healthCosts);
        ArgumentOutOfRangeException.ThrowIfNegative(franchise);

        var franchisePaid = Math.Min(healthCosts, franchise);
        var quotePart = Math.Min((healthCosts - franchisePaid) * QuotePartRate, MaxQuotePart(group));
        return new CostSharing(franchisePaid, quotePart);
    }

    /// <summary>Frais médicaux à partir desquels la participation maximale est atteinte.</summary>
    public static decimal CostsAtMaxParticipation(int franchise, AgeGroup group) =>
        franchise + MaxQuotePart(group) / QuotePartRate;

    public static string Label(this AgeGroup group) => group switch
    {
        AgeGroup.Enfant => "Enfant (0-18 ans)",
        AgeGroup.JeuneAdulte => "Jeune adulte (19-25 ans)",
        _ => "Adulte (26 ans et plus)",
    };
}
