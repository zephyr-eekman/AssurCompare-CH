using AssurCompare.Core.Analyses;
using AssurCompare.Core.Besoins;
using AssurCompare.Core.Lamal;
using AssurCompare.Core.Parametres;

namespace AssurCompare.Core.Tests;

public class ClientAnalysisTests
{
    private static readonly InsurerInfo A = new(1, "Caisse A", null);
    private static readonly InsurerInfo B = new(2, "Caisse B", null);
    private static readonly InsurerInfo C = new(3, "Caisse C", null);
    private static readonly TariffInfo BaseA = new(1, "BASE", TariffType.Standard, "Base");
    private static readonly TariffInfo BaseB = new(2, "BASE", TariffType.Standard, "Base");
    private static readonly TariffInfo HmoC = new(3, "HMO", TariffType.MedecinDeFamille, "HMO");

    private static readonly PremiumOffer[] Offers =
    [
        new(A, BaseA, 0, AgeGroup.Adulte, true, 300, 500m),
        new(A, BaseA, 0, AgeGroup.Adulte, true, 2500, 400m),
        new(A, BaseA, 0, AgeGroup.Adulte, false, 300, 470m),
        new(A, BaseA, 0, AgeGroup.Adulte, false, 2500, 380m),
        new(B, BaseB, 0, AgeGroup.Adulte, true, 300, 600m),
        new(B, BaseB, 0, AgeGroup.Adulte, true, 2500, 480m),
        new(C, HmoC, 0, AgeGroup.Adulte, true, 2500, 300m),
    ];

    private static ClientProfile Profile(bool alternatives = false) => new()
    {
        Age = 30,
        Work = WorkStatus.Salarie,
        WeeklyHours = 42,
        HasAccidentInLamal = true,
        ExpectedHealthCosts = 0,
        AcceptsAlternativeModels = alternatives,
        CurrentTariffKey = "2|BASE",
        CurrentFranchise = 300,
    };

    private static AnalysisSummary Build(ClientProfile p) =>
        ClientAnalysisBuilder.Build(p, Offers, 2027, SwissParameters.Y2027);

    [Theory]
    [InlineData(0, AgeGroup.Enfant)]
    [InlineData(18, AgeGroup.Enfant)]
    [InlineData(19, AgeGroup.JeuneAdulte)]
    [InlineData(25, AgeGroup.JeuneAdulte)]
    [InlineData(26, AgeGroup.Adulte)]
    public void Age_group_follows_ofsp_classes(int age, AgeGroup expected)
    {
        Assert.Equal(expected, LamalRules.AgeGroupFor(age));
    }

    [Fact]
    public void Finds_best_plan_and_saving_against_current_offer()
    {
        var s = Build(Profile());

        Assert.Equal("Caisse A · Base", s.BestOffer);
        Assert.Equal(2500, s.RecommendedFranchise);
        Assert.Equal(4_800m, s.BestAnnualCost);          // 12 x 400, sans frais médicaux
        Assert.Equal(7_200m, s.CurrentAnnualCost);       // 12 x 600
        Assert.Equal(2_400m, s.AnnualSaving);
    }

    [Fact]
    public void Employee_gets_accident_removal_saving()
    {
        var s = Build(Profile());

        Assert.Equal(240m, s.AccidentRemovalSaving);      // (400 - 380) x 12
        Assert.Equal(2_640m, s.IdentifiedAnnualSaving);
    }

    [Fact]
    public void Alternative_models_are_used_only_if_the_client_accepts_them()
    {
        Assert.Equal("Caisse C · HMO", Build(Profile(alternatives: true)).BestOffer);
        Assert.Equal("Caisse A · Base", Build(Profile(alternatives: false)).BestOffer);
    }

    [Fact]
    public void Summary_lists_concrete_next_steps()
    {
        var s = Build(Profile());

        Assert.Contains(s.NextSteps, step => step.StartsWith("Changer pour Caisse A"));
        Assert.Contains(s.NextSteps, step => step.StartsWith("Retirer la couverture accident"));
        Assert.Contains(s.NextSteps, step => step.StartsWith("Ouvrir un 3e pilier"));
    }

    [Fact]
    public void Pillar3a_is_listed_once_in_next_steps()
    {
        var p = Profile();
        p.Work = WorkStatus.Independant;

        var steps = Build(p).NextSteps;

        Assert.DoesNotContain(steps, step => step == "Mettre en place : Pilier 3a.");
        Assert.Single(steps, step => step.StartsWith("Ouvrir un 3e pilier"));
    }

    [Fact]
    public void Unknown_current_offer_gives_no_saving()
    {
        var p = Profile();
        p.CurrentTariffKey = null;

        var s = Build(p);

        Assert.Null(s.AnnualSaving);
        Assert.NotNull(s.BestOffer);
    }

    [Fact]
    public void Pillar3a_tax_saving_uses_2027_maximum_and_marginal_rate()
    {
        var p = Profile();
        p.MarginalTaxRatePct = 20;

        Assert.Equal(Math.Round(7_373m * 0.20m, 0), Build(p).Pillar3aTaxSaving);
    }
}
