# AssurCompare CH expliqué simplement (pour préparer l'entretien)

Ce document est pour toi, Zéphyr. Il explique le projet avec des mots simples, pour que tu puisses
en parler avec assurance devant un recruteur. Lis-le une fois en entier, puis relis les questions en bas
avant chaque entretien.

---

## 1. Le projet en une phrase

> « J'ai créé un petit logiciel qui aide un conseiller en assurance : il compare les primes des caisses-maladie
> avec les vrais chiffres 2027 de l'OFSP, calcule la meilleure franchise, chiffre un 3e pilier et repère
> les lacunes de couverture d'un client. Je l'ai fait avec l'aide de l'IA pour montrer ma motivation pour le métier. »

Dis toujours honnêtement que l'IA t'a aidé. Ce qu'on attend de toi, c'est de **comprendre ce que fait l'outil
et pourquoi** : c'est exactement ce que ce document t'apprend.

---

## 2. Les 5 outils et ce qu'ils montrent de toi

### Comparateur de primes LAMal
- **Ce qu'il fait** : pour un canton, un âge, une franchise et un modèle, il classe toutes les caisses de la moins chère à la plus chère.
- **Le chiffre qui parle** : à Neuchâtel, pour un adulte avec franchise 300, l'écart entre l'offre la moins chère et la plus chère
  dépasse **CHF 2'500 par an** pour exactement la même assurance de base.
- **Ce que ça montre** : tu sais que la LAMal est la même partout (les prestations sont fixées par la loi),
  donc la différence se joue sur la prime et le modèle.

### Simulateur de franchise
- **Ce qu'il fait** : il calcule ce que le client paie **vraiment** sur une année, pas seulement la prime.
- **La règle à connaître par cœur** :
  1. le client paie d'abord ses frais jusqu'au montant de la **franchise** (300 à 2'500 francs pour un adulte) ;
  2. ensuite il paie la **quote-part** : 10 % du reste, au maximum **700 francs** par an (350 pour un enfant) ;
  3. coût total = 12 primes + franchise payée + quote-part.
- **Le seuil de rentabilité** : c'est le montant de frais médicaux où deux franchises coûtent pareil.
  En dessous, la franchise haute est plus avantageuse ; au-dessus, c'est la franchise basse.
- **Exemple à savoir expliquer** : si la franchise 2'500 fait économiser 100 francs de prime par mois (1'200 par an),
  elle reste plus avantageuse tant que les frais médicaux ne dépassent pas environ 1'630 francs par an.
  Une personne en bonne santé a donc souvent intérêt à prendre une franchise haute ; une personne avec un traitement régulier, une basse.

### Analyse des besoins
- **Ce qu'il fait** : comme au premier rendez-vous avec un client. On coche sa situation (salarié ou indépendant,
  locataire ou propriétaire, enfants, voiture...) et ce qu'il a déjà. L'outil sort une liste de priorités et une fiche PDF.
- **Quelques règles qu'il applique** (à connaître) :
  - la LAMal est **obligatoire** pour toute personne domiciliée en Suisse ;
  - un salarié qui travaille **au moins 8 heures par semaine** est couvert contre les accidents par l'assurance de son employeur (LAA) :
    il peut **retirer l'accident de sa LAMal** et payer moins cher ;
  - la **RC véhicule** est obligatoire pour circuler ; en leasing, la **casco complète** est en général exigée ;
  - un **indépendant** n'a pas de perte de gain d'un employeur : c'est une priorité pour lui ;
  - avec des enfants, une assurance **risque décès / invalidité** protège la famille.

### Comparateur d'offres
- **Ce qu'il fait** : il compare des offres (auto, ménage, RC) sur le **coût total** : primes + ce qui reste à la charge du client en cas de sinistre.
- **Le message de vente** : « La prime la plus basse n'est pas toujours la meilleure affaire. »
  Avec l'exemple affiché, l'offre à 290 francs avec 500 francs de franchise coûte plus cher sur 5 ans que l'offre à 340 francs avec 200 francs de franchise, dès qu'il y a quelques sinistres.

### Calculateur 3e pilier
- **Ce qu'il fait** : il montre combien un client accumule s'il verse chaque année dans un pilier 3a, et combien d'impôts il économise.
- **À savoir** : en 2026, un salarié affilié à une caisse de pension peut verser au maximum **7'258 francs par an** dans le 3a.
  Ces versements se **déduisent du revenu imposable**. Avec un taux marginal de 25 %, ça fait environ 1'800 francs d'impôts en moins chaque année.
- **Rendement composé** : les intérêts rapportent eux-mêmes des intérêts. Sur 30 ou 40 ans, c'est ce qui fait la plus grosse différence.

---

## 3. Comment c'est construit (version simple)

| Mot technique | Ce que ça veut dire |
|---|---|
| **C# / .NET 10** | Le langage de programmation et la plateforme de Microsoft, très utilisés dans les banques et les assurances. |
| **Blazor WebAssembly** | Le programme C# tourne directement dans le navigateur, comme un site web. Pas besoin de serveur. |
| **MudBlazor** | Une bibliothèque de composants tout faits (menus, listes déroulantes, tableaux) pour avoir une belle interface. |
| **Données ouvertes (open data)** | L'OFSP publie gratuitement toutes les primes. J'utilise ce fichier officiel, rien n'est inventé. |
| **Préparation des données** | Le fichier de l'OFSP fait 24 Mo et 220'000 lignes. Un petit programme le découpe en un fichier par canton, beaucoup plus léger. |
| **Tests automatiques** | 36 petits programmes qui vérifient que chaque calcul donne le bon résultat. Si on modifie quelque chose et qu'on casse un calcul, un test le signale tout de suite. |
| **Séparation calcul / affichage** | Les formules sont dans une partie à part (`AssurCompare.Core`) et l'interface dans une autre. On peut ainsi vérifier les calculs sans l'interface. |
| **GitHub / GitHub Pages** | GitHub garde le code en ligne ; GitHub Pages publie le site gratuitement. À chaque modification, les tests sont relancés et le site est mis à jour automatiquement. |

---

## 4. Questions probables en entretien (et réponses possibles)

**« Pourquoi avez-vous fait ce projet ? »**
> Je veux devenir conseiller en assurance et j'ai voulu comprendre concrètement ce que le client paie et comment le conseiller peut l'aider.
> En construisant l'outil, j'ai appris comment fonctionnent la franchise, la quote-part, les modèles alternatifs et le 3e pilier.

**« Vous savez programmer ? »**
> J'ai réalisé ce projet avec l'aide de l'IA. Je ne me présente pas comme développeur, mais je comprends ce que fait chaque partie
> et chaque formule. Ce qui m'intéresse, c'est le conseil : l'outil sert à rendre les chiffres clairs pour le client.

**« Quelle franchise conseilleriez-vous à un jeune en bonne santé ? »**
> En général la franchise 2'500, parce que l'économie sur la prime dépasse ce qu'il risque de payer en plus.
> Mais je lui conseillerais de garder de côté l'argent de la franchise en cas de pépin, et de revoir le choix chaque année.

**« Qu'est-ce qu'un modèle alternatif ? »**
> Le client s'engage à appeler d'abord une ligne de télémédecine, à passer par son médecin de famille ou par un réseau HMO.
> En échange, la prime est plus basse. Dans le comparateur, ce sont souvent les offres les moins chères.

**« Quelles sont les limites de votre outil ? »**
> Ce sont des estimations. Par exemple, je ne prends pas en compte la contribution de 15 francs par jour à l'hôpital,
> ni l'impôt au moment du retrait du 3e pilier. Ça ne remplace pas une offre officielle : ça aide à préparer et à expliquer.

**« D'où viennent les chiffres ? »**
> Du fichier officiel des primes 2027 publié par l'Office fédéral de la santé publique sur opendata.swiss. J'ai vérifié des montants
> directement dans le fichier brut, par exemple Agrisano à Neuchâtel.

---

## 5. Démonstration en 2 minutes (si on te demande de montrer)

1. **Comparateur** : choisir Neuchâtel, adulte, franchise 300. Montrer l'écart de plus de CHF 2'500 par an. Choisir une « caisse actuelle » chère et montrer l'économie.
2. **Franchise** : bouger le curseur des frais médicaux de 0 à 5'000 et montrer que la meilleure franchise change. Montrer le seuil de rentabilité.
3. **Analyse des besoins** : cocher « voiture en leasing » et montrer que la casco devient obligatoire, puis cliquer « Imprimer / PDF ».
