using AssurCompare.Core.Besoins;
using AssurCompare.Core.Lamal;
using AssurCompare.Core.Parametres;
using AssurCompare.Core.Prevoyance;

namespace AssurCompare.Core.Analyses;

/// <summary>Les résultats clés d'une analyse, assez petits pour être sauvegardés et imprimés.</summary>
public sealed class AnalysisSummary
{
    public int PremiumYear { get; set; }
    public string Canton { get; set; } = "";

    public string? CurrentOffer { get; set; }
    public decimal? CurrentAnnualCost { get; set; }

    public string? BestOffer { get; set; }
    public string? BestOfferModel { get; set; }
    public decimal? BestMonthlyPremium { get; set; }
    public int? RecommendedFranchise { get; set; }
    public decimal? BestAnnualCost { get; set; }

    /// <summary>Économie annuelle en passant de l'offre actuelle à la meilleure offre (si l'offre actuelle est connue).</summary>
    public decimal? AnnualSaving { get; set; }
    public decimal? AccidentRemovalSaving { get; set; }

    public int CoverageScore { get; set; }
    public List<string> PriorityGaps { get; set; } = [];

    public decimal? Pillar3aTaxSaving { get; set; }
    public decimal? RetirementProjectedMonthly { get; set; }
    public decimal? RetirementTargetMonthly { get; set; }
    public decimal? RetirementGapMonthly { get; set; }
    public decimal? Annual3aNeeded { get; set; }
    public decimal? Pillar3aMaximum { get; set; }

    /// <summary>Le 3a seul ne suffit pas : il faudrait verser plus que le maximum légal.</summary>
    public bool Needs3bOrBuyback => Annual3aNeeded > Pillar3aMaximum;

    public List<string> NextSteps { get; set; } = [];

    /// <summary>Économie sur la LAMal identifiée pour le client (changement d'offre + accident retiré).</summary>
    public decimal IdentifiedAnnualSaving => Math.Max(0, AnnualSaving ?? 0) + (AccidentRemovalSaving ?? 0);
}

public sealed class SavedAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ClientProfile Profile { get; set; } = new();
    public AnalysisSummary Summary { get; set; } = new();
}

/// <summary>
/// Réunit les résultats de tous les outils pour un profil : meilleure offre LAMal pour les frais attendus,
/// économie par rapport à l'offre actuelle, lacunes de couverture, 3e pilier et lacune de prévoyance.
/// Aucune nouvelle formule : on réutilise les calculateurs existants.
/// </summary>
public static class ClientAnalysisBuilder
{
    public static AnalysisSummary Build(
        ClientProfile p, IReadOnlyList<PremiumOffer> cantonOffers, int premiumYear, SwissYearParameters parameters)
    {
        var summary = new AnalysisSummary { PremiumYear = premiumYear, Canton = p.Canton };
        AddLamal(p, cantonOffers, summary);
        AddNeeds(p, summary);
        AddRetirement(p, parameters, summary);
        summary.NextSteps = NextSteps(summary);
        return summary;
    }

    public static string OfferKey(PremiumOffer offer) => $"{offer.Insurer.Id}|{offer.Tariff.Code}";

    /// <summary>Meilleure offre (toutes franchises confondues) pour le coût total attendu.</summary>
    public static PremiumOffer? BestPlan(ClientProfile p, IEnumerable<PremiumOffer> offers, bool withAccident) =>
        Candidates(p, offers, withAccident)
            .Where(o => p.AcceptsAlternativeModels || o.Tariff.Type == TariffType.Standard)
            .OrderBy(o => PremiumComparer.ExpectedTotalCost(o, p.ExpectedHealthCosts))
            .ThenBy(o => o.Insurer.Name)
            .FirstOrDefault();

    public static PremiumOffer? CurrentOffer(ClientProfile p, IEnumerable<PremiumOffer> offers)
    {
        if (p.CurrentTariffKey is null) return null;
        var franchise = p.CurrentFranchise ?? LamalRules.DefaultFranchise(LamalRules.AgeGroupFor(p.Age));
        return Candidates(p, offers, p.HasAccidentInLamal)
            .FirstOrDefault(o => OfferKey(o) == p.CurrentTariffKey && o.Franchise == franchise);
    }

    private static IEnumerable<PremiumOffer> Candidates(ClientProfile p, IEnumerable<PremiumOffer> offers, bool withAccident)
    {
        var age = LamalRules.AgeGroupFor(p.Age);
        return offers.Where(o => o.Region == p.Region && o.Age == age && o.WithAccident == withAccident);
    }

    private static void AddLamal(ClientProfile p, IReadOnlyList<PremiumOffer> offers, AnalysisSummary s)
    {
        var best = BestPlan(p, offers, p.HasAccidentInLamal);
        if (best is null) return;

        var bestCost = PremiumComparer.ExpectedTotalCost(best, p.ExpectedHealthCosts);
        s.BestOffer = $"{best.Insurer.Name} · {best.Tariff.Name}";
        s.BestOfferModel = best.Tariff.Type.Label();
        s.BestMonthlyPremium = best.MonthlyPremium;
        s.RecommendedFranchise = best.Franchise;
        s.BestAnnualCost = bestCost;

        var current = CurrentOffer(p, offers);
        if (current is not null)
        {
            var currentCost = PremiumComparer.ExpectedTotalCost(current, p.ExpectedHealthCosts);
            s.CurrentOffer = $"{current.Insurer.Name} · {current.Tariff.Name} · franchise {current.Franchise}";
            s.CurrentAnnualCost = currentCost;
            s.AnnualSaving = currentCost - bestCost;
        }

        var coveredByEmployer = p.Work == WorkStatus.Salarie && p.WeeklyHours >= 8;
        if (coveredByEmployer && p.HasAccidentInLamal && BestPlan(p, offers, withAccident: false) is { } withoutAccident)
        {
            s.AccidentRemovalSaving = Math.Max(0, bestCost - PremiumComparer.ExpectedTotalCost(withoutAccident, p.ExpectedHealthCosts));
        }
    }

    private static void AddNeeds(ClientProfile p, AnalysisSummary s)
    {
        var report = NeedsAnalyzer.Analyze(p);
        s.CoverageScore = report.CoverageScore;
        s.PriorityGaps = report.Gaps.Where(g => g.Priority <= Priority.Haute).Select(g => g.Title).ToList();
    }

    private static void AddRetirement(ClientProfile p, SwissYearParameters parameters, AnalysisSummary s)
    {
        if (p.Work is not (WorkStatus.Salarie or WorkStatus.Independant) || p.AnnualSalary <= 0) return;

        var affiliated = p.Work == WorkStatus.Salarie && p.HasPensionFund;
        if (!p.Has3a)
        {
            var maximum = affiliated
                ? parameters.Pillar3aWithPensionFund
                : Math.Min(parameters.Pillar3aWithoutPensionFund, p.AnnualSalary * 0.20m);
            s.Pillar3aTaxSaving = Math.Round(maximum * p.MarginalTaxRatePct / 100m, 0);
        }

        if (p.Age >= SwissParameters.RetirementAge) return;
        var result = RetirementGapCalculator.Calculate(
            new RetirementInput(p.Age, p.AnnualSalary, p.LppCapital, p.RetirementTargetPct, AffiliatedToLpp: affiliated),
            parameters);
        s.RetirementProjectedMonthly = result.ProjectedMonthly;
        s.RetirementTargetMonthly = result.TargetMonthly;
        s.RetirementGapMonthly = result.GapMonthly;
        s.Annual3aNeeded = result.Annual3aNeeded;
        s.Pillar3aMaximum = result.Pillar3aMaximum;
    }

    private static List<string> NextSteps(AnalysisSummary s)
    {
        var steps = new List<string>();
        if (s.AnnualSaving > 0)
            steps.Add($"Changer pour {s.BestOffer} avec la franchise {s.RecommendedFranchise} : {Chf.Format(s.AnnualSaving.Value, false)} d'économie par an. Résilier avant le 30 novembre.");
        if (s.AccidentRemovalSaving > 0)
            steps.Add($"Retirer la couverture accident de la LAMal (déjà couvert par l'employeur) : {Chf.Format(s.AccidentRemovalSaving.Value, false)} par an.");
        // Le 3a a sa propre étape chiffrée plus bas : pas de doublon.
        steps.AddRange(s.PriorityGaps
            .Where(gap => !(gap == "Pilier 3a" && s.Pillar3aTaxSaving > 0))
            .Take(3)
            .Select(gap => $"Mettre en place : {gap}."));
        if (s.Pillar3aTaxSaving > 0)
            steps.Add($"Ouvrir un 3e pilier : jusqu'à {Chf.Format(s.Pillar3aTaxSaving.Value, false)} d'impôts en moins chaque année.");
        if (s.RetirementGapMonthly > 0)
            steps.Add(s.Needs3bOrBuyback
                ? $"Lacune de prévoyance estimée à {Chf.Format(s.RetirementGapMonthly.Value, false)} par mois : le 3a ne suffit pas (maximum {Chf.Format(s.Pillar3aMaximum ?? 0, false)} par an), compléter par un rachat LPP ou une épargne 3b."
                : $"Lacune de prévoyance estimée à {Chf.Format(s.RetirementGapMonthly.Value, false)} par mois à la retraite : planifier l'épargne.");
        return steps;
    }
}
