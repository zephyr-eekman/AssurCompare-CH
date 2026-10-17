namespace AssurCompare.Web.Components;

/// <summary>Définitions courtes, en français simple, affichées dans les infobulles.</summary>
public static class Glossary
{
    public static readonly IReadOnlyDictionary<string, string> Terms = new Dictionary<string, string>
    {
        ["franchise"] = "Montant des frais médicaux que l'on paie entièrement soi-même chaque année avant que la caisse ne rembourse. De 300 à 2'500 francs pour un adulte.",
        ["quote-part"] = "Après la franchise, on paie encore 10 % des frais, au maximum 700 francs par an (350 pour un enfant).",
        ["coût total"] = "Ce que le client paie vraiment sur une année : 12 primes + franchise + quote-part.",
        ["seuil de rentabilité"] = "Montant de frais médicaux où deux franchises coûtent exactement la même chose. En dessous, la franchise haute gagne ; au-dessus, la basse.",
        ["modèle alternatif"] = "Le client s'engage à appeler d'abord la télémédecine, son médecin de famille ou un réseau HMO. En échange, la prime baisse.",
        ["région de prime"] = "Certains cantons sont découpés en 2 ou 3 régions avec des primes différentes. La région dépend de la commune de domicile.",
        ["accident"] = "Un salarié qui travaille au moins 8 heures par semaine est déjà assuré contre les accidents par son employeur (LAA) : il peut retirer l'accident de sa LAMal.",
        ["salaire coordonné"] = "La part du salaire assurée par la LPP obligatoire : le salaire moins la déduction de coordination (26'880 francs en 2027).",
        ["bonifications"] = "Pourcentage du salaire coordonné versé chaque année sur le compte LPP : 7 % de 25 à 34 ans, 10 % de 35 à 44, 15 % de 45 à 54, 18 % dès 55 ans.",
        ["taux de conversion"] = "Pourcentage du capital LPP versé chaque année en rente à la retraite : 6,8 % pour la partie obligatoire.",
        ["13e rente"] = "Depuis décembre 2026, l'AVS verse une 13e rente chaque année, égale à un mois de rente.",
        ["taux marginal"] = "Pourcentage d'impôt payé sur le dernier franc gagné. C'est lui qui donne l'économie d'impôt d'une déduction comme le 3a.",
        ["lacune de prévoyance"] = "Différence entre le revenu souhaité à la retraite et ce que versent l'AVS et la LPP.",
        ["3a"] = "Épargne retraite volontaire et déductible des impôts, plafonnée chaque année par la loi.",
    };
}
