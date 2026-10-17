# AssurCompare CH

Outils d'aide au conseil en assurance, en Suisse : comparateur de primes LAMal 2027, simulateur de franchise,
analyse des besoins du client, comparateur d'offres et calculateur de 3e pilier.

L'application tourne entièrement dans le navigateur (Blazor WebAssembly, .NET 10), sans serveur ni base de données.

> Projet personnel de **Zéphyr Eekman**, réalisé avec l'aide de l'IA, pour illustrer ma motivation
> pour le conseil en assurance. Outil indicatif : il ne remplace ni une offre officielle ni [priminfo.admin.ch](https://www.priminfo.admin.ch).

## Les outils

| Outil | À quoi il sert |
|---|---|
| **Comparateur de primes LAMal** | Classe les 32 caisses d'un canton pour un profil (âge, franchise, accident, modèle) et chiffre l'économie annuelle par rapport à la caisse actuelle du client. |
| **Simulateur de franchise** | Calcule le coût réel de chaque franchise (primes + franchise + quote-part) et le **seuil de rentabilité** : à partir de quel montant de frais une franchise basse devient avantageuse. |
| **Analyse des besoins** | À partir de la situation du client (travail, logement, famille, véhicule), liste les couvertures obligatoires, les lacunes prioritaires et les pistes d'économie. Fiche imprimable en PDF. |
| **Comparateur d'offres** | Pour l'auto, le ménage ou la RC : compare le **coût total** sur plusieurs années selon une hypothèse de sinistres, pas seulement la prime. |
| **Calculateur 3e pilier** | Capital à 65 ans, rendement composé, impôts économisés et comparaison avec un compte épargne. |

## Données

- **Primes 2027** : jeu de données officiel de l'Office fédéral de la santé publique (OFSP),
  [Primes de l'assurance-maladie sur opendata.swiss](https://opendata.swiss/fr/dataset/health-insurance-premiums),
  soit environ 220 000 tarifs pour 26 cantons et 32 caisses.
- **Noms des caisses** : liste officielle des assureurs-maladie admis (OFSP, état au 1.1.2026).

Le programme `tools/AssurCompare.DataPrep` convertit le fichier brut (24 Mo) en un petit fichier JSON par canton
(90 à 300 Ko), que l'application télécharge seulement quand on choisit ce canton.

## Architecture

```
src/
  AssurCompare.Core/      Règles et calculs métier (C# pur, sans interface)
    Lamal/                Participation aux coûts, franchises, comparaison des primes
    Prevoyance/           Simulation du pilier 3a
    Offres/               Coût total attendu d'une offre non-vie
    Besoins/              Règles d'analyse des besoins
  AssurCompare.Web/       Application Blazor WebAssembly (MudBlazor, graphiques SVG maison)
tests/
  AssurCompare.Core.Tests/  36 tests automatiques (xUnit) sur tous les calculs
tools/
  AssurCompare.DataPrep/    Préparation des données OFSP
```

Les calculs sont séparés de l'interface : on peut les tester seuls et les réutiliser ailleurs.
Chaque formule importante est vérifiée par un test, souvent avec un exemple calculé à la main.

## Lancer le projet

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet test                                   # lance les 36 tests
dotnet run --project src/AssurCompare.Web     # puis ouvrir l'adresse affichée
```

Pour régénérer les données (après avoir téléchargé `Prämien_CH.csv` et `Tarife.csv` depuis opendata.swiss
dans `data-source/ofsp/`, renommés `Primes_CH.csv` et `Tarifs.csv`) :

```bash
dotnet run --project tools/AssurCompare.DataPrep -- data-source/ofsp src/AssurCompare.Web/wwwroot/data
```

## Publication

À chaque envoi sur la branche `main`, GitHub Actions lance les tests, publie l'application et la met en ligne
sur GitHub Pages (`.github/workflows/deploy.yml`).

## Limites connues

- La contribution aux frais hospitaliers (CHF 15 par jour) n'est pas modélisée.
- Les rabais pour le 2e ou 3e enfant ne sont pas repris.
- Pour les cantons à plusieurs régions de primes, l'utilisateur choisit la région lui-même.
- 3e pilier : rendement constant, impôt au retrait non modélisé. Montants maximaux 2026.
