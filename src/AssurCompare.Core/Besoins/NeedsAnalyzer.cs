namespace AssurCompare.Core.Besoins;

public enum WorkStatus { Salarie, Independant, SansEmploi, EnFormation, Retraite }

public enum HousingStatus { Locataire, Proprietaire, ChezSesParents }

public enum Priority { Obligatoire, Haute, Moyenne, Faible }

public sealed class ClientProfile
{
    public string Name { get; set; } = "";
    public int Age { get; set; } = 30;
    public WorkStatus Work { get; set; } = WorkStatus.Salarie;
    public int WeeklyHours { get; set; } = 42;
    public bool HasPensionFund { get; set; } = true;
    public HousingStatus Housing { get; set; } = HousingStatus.Locataire;
    public bool LivesWithPartner { get; set; }
    public int Children { get; set; }
    public bool HasCar { get; set; }
    public bool HasLeasing { get; set; }
    public bool HasMotorbike { get; set; }
    public bool TravelsAbroad { get; set; }

    // Ce que le client possède déjà
    public bool HasAccidentInLamal { get; set; } = true;
    public bool HasRcPrivee { get; set; }
    public bool HasMenage { get; set; }
    public bool HasBatiment { get; set; }
    public bool HasCasco { get; set; }
    public bool Has3a { get; set; }
    public bool HasRiskLifeInsurance { get; set; }
    public bool HasLossOfEarnings { get; set; }
    public bool HasLegalProtection { get; set; }
    public bool HasTravelInsurance { get; set; }
    public bool HasChildDental { get; set; }

    // Profil LAMal, partagé par tous les outils
    public string Canton { get; set; } = "NE";
    public int Region { get; set; }
    public decimal ExpectedHealthCosts { get; set; } = 500;
    public bool AcceptsAlternativeModels { get; set; } = true;
    /// <summary>Caisse et modèle actuels, au format « numéro OFSP|code tarif ».</summary>
    public string? CurrentTariffKey { get; set; }
    public int? CurrentFranchise { get; set; }

    // Prévoyance
    public decimal AnnualSalary { get; set; } = 60_000;
    public decimal LppCapital { get; set; }
    public decimal MarginalTaxRatePct { get; set; } = 20;
    public decimal RetirementTargetPct { get; set; } = 70;

    public ClientProfile Clone() => (ClientProfile)MemberwiseClone();
}

public sealed record Recommendation(string Domain, string Title, Priority Priority, string Reason, bool AlreadyCovered)
{
    public bool IsGap => !AlreadyCovered;
}

public sealed record NeedsReport(ClientProfile Profile, IReadOnlyList<Recommendation> Items)
{
    /// <summary>Part des besoins obligatoires et prioritaires déjà couverts (0 à 100).</summary>
    public int CoverageScore
    {
        get
        {
            var essentials = Items.Where(i => i.Priority is Priority.Obligatoire or Priority.Haute).ToList();
            return essentials.Count == 0 ? 100 : 100 * essentials.Count(i => i.AlreadyCovered) / essentials.Count;
        }
    }

    public IEnumerable<Recommendation> Gaps => Items.Where(i => i.IsGap).OrderBy(i => i.Priority);
}

/// <summary>
/// Règles simples d'analyse des besoins, inspirées d'un entretien conseil.
/// Ce n'est pas un conseil juridique : chaque point est à vérifier avec le client.
/// </summary>
public static class NeedsAnalyzer
{
    public static NeedsReport Analyze(ClientProfile p)
    {
        var items = new List<Recommendation>
        {
            new("Santé", "Assurance obligatoire des soins (LAMal)", Priority.Obligatoire,
                "Obligatoire pour toute personne domiciliée en Suisse.", true),
        };

        AddAccident(p, items);
        AddVehicles(p, items);
        AddHousing(p, items);
        AddRetirement(p, items);
        AddFamilyAndIncome(p, items);
        AddOther(p, items);

        return new NeedsReport(p, items.OrderBy(i => i.Priority).ThenBy(i => i.Domain).ToList());
    }

    private static void AddAccident(ClientProfile p, List<Recommendation> items)
    {
        var coveredByEmployer = p.Work == WorkStatus.Salarie && p.WeeklyHours >= 8;
        if (coveredByEmployer)
        {
            items.Add(new("Santé", "Suspendre la couverture accident LAMal", Priority.Moyenne,
                "Salarié au moins 8 h par semaine : les accidents sont couverts par l'assurance de l'employeur (LAA). "
                + "Retirer l'accident de la LAMal fait baisser la prime.",
                !p.HasAccidentInLamal));
            return;
        }

        var reason = p.Work == WorkStatus.SansEmploi
            ? "Sans employeur, la LAMal doit couvrir les accidents. Exception : les personnes qui touchent des indemnités de chômage sont assurées par la Suva (à vérifier)."
            : "Sans assurance accidents d'un employeur (ou moins de 8 h par semaine), la LAMal doit couvrir les accidents.";
        items.Add(new("Santé", "Couverture accident dans la LAMal", Priority.Haute, reason, p.HasAccidentInLamal));
    }

    private static void AddVehicles(ClientProfile p, List<Recommendation> items)
    {
        if (!p.HasCar && !p.HasMotorbike) return;

        items.Add(new("Véhicule", "Responsabilité civile véhicule", Priority.Obligatoire,
            "Obligatoire pour circuler avec un véhicule à moteur (loi sur la circulation routière).", true));

        if (p.HasCar)
        {
            items.Add(new("Véhicule", "Casco (partielle ou complète)", p.HasLeasing ? Priority.Obligatoire : Priority.Moyenne,
                p.HasLeasing
                    ? "En cas de leasing, la casco complète est en général exigée par la société de leasing."
                    : "Protège son propre véhicule (vol, grêle, bris de glace, collision pour la casco complète).",
                p.HasCasco));
        }
    }

    private static void AddHousing(ClientProfile p, List<Recommendation> items)
    {
        if (p.Housing == HousingStatus.ChezSesParents)
        {
            items.Add(new("Logement", "Ménage et RC privée", Priority.Faible,
                "Souvent couvert par la police des parents tant que l'on vit chez eux : à vérifier.",
                p.HasMenage && p.HasRcPrivee));
            return;
        }

        items.Add(new("Logement", "Responsabilité civile privée", Priority.Haute,
            "Couvre les dommages causés à autrui (dégâts dans le logement loué, accident causé à un tiers). Souvent demandée par les bailleurs.",
            p.HasRcPrivee));
        items.Add(new("Logement", "Assurance ménage (inventaire)", Priority.Haute,
            "Protège ses biens contre l'incendie, les dégâts d'eau et le vol. Obligatoire dans certains cantons, comme Vaud.",
            p.HasMenage));

        if (p.Housing == HousingStatus.Proprietaire)
        {
            items.Add(new("Logement", "Assurance bâtiment", Priority.Obligatoire,
                "Généralement obligatoire, souvent auprès de l'établissement cantonal d'assurance.",
                p.HasBatiment));
        }
    }

    private static void AddRetirement(ClientProfile p, List<Recommendation> items)
    {
        if (p.Work is not (WorkStatus.Salarie or WorkStatus.Independant)) return;

        var withoutLpp = p.Work == WorkStatus.Independant || !p.HasPensionFund;
        items.Add(new("Prévoyance", "Pilier 3a", withoutLpp ? Priority.Haute : Priority.Moyenne,
            withoutLpp
                ? "Sans 2e pilier, le 3a est la principale épargne retraite et se déduit des impôts (jusqu'à 20 % du revenu, plafonné)."
                : "Les versements se déduisent du revenu imposable : une économie d'impôt chaque année.",
            p.Has3a));
    }

    private static void AddFamilyAndIncome(ClientProfile p, List<Recommendation> items)
    {
        var hasDependents = p.Children > 0 || p.LivesWithPartner;
        if (hasDependents)
        {
            items.Add(new("Famille", "Assurance risque décès / invalidité", p.Children > 0 ? Priority.Haute : Priority.Moyenne,
                "Protège le partenaire et les enfants si le revenu disparaît (décès ou invalidité).",
                p.HasRiskLifeInsurance));
        }

        if (p.Children > 0)
        {
            items.Add(new("Famille", "Complémentaire dentaire / orthodontie pour les enfants", Priority.Moyenne,
                "Les traitements d'orthodontie sont chers et non couverts par la LAMal. Une complémentaire se conclut de préférence tôt.",
                p.HasChildDental));
        }

        if (p.Work == WorkStatus.Independant)
        {
            items.Add(new("Revenu", "Perte de gain maladie (indemnités journalières)", Priority.Haute,
                "Un indépendant n'a pas d'assurance perte de gain d'un employeur : sans elle, une maladie longue fait perdre le revenu.",
                p.HasLossOfEarnings));
        }
    }

    private static void AddOther(ClientProfile p, List<Recommendation> items)
    {
        items.Add(new("Autres", "Protection juridique", p.HasCar || p.HasMotorbike ? Priority.Moyenne : Priority.Faible,
            "Prend en charge les frais d'avocat et de procédure (bail, travail, circulation).",
            p.HasLegalProtection));

        if (p.TravelsAbroad)
        {
            items.Add(new("Autres", "Assurance voyage / assistance", Priority.Moyenne,
                "Annulation, rapatriement et frais médicaux élevés à l'étranger (hors de ce que couvre la LAMal).",
                p.HasTravelInsurance));
        }
    }

    public static string Label(this Priority priority) => priority switch
    {
        Priority.Obligatoire => "Obligatoire",
        Priority.Haute => "Prioritaire",
        Priority.Moyenne => "Recommandé",
        _ => "Optionnel",
    };

    public static string Label(this WorkStatus status) => status switch
    {
        WorkStatus.Salarie => "Salarié",
        WorkStatus.Independant => "Indépendant",
        WorkStatus.SansEmploi => "Sans emploi",
        WorkStatus.EnFormation => "En formation",
        _ => "Retraité",
    };

    public static string Label(this HousingStatus status) => status switch
    {
        HousingStatus.Locataire => "Locataire",
        HousingStatus.Proprietaire => "Propriétaire",
        _ => "Chez ses parents",
    };
}
