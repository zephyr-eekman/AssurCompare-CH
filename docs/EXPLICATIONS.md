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
| **État partagé** | Le profil du client est gardé à un seul endroit, et tous les outils le lisent. C'est pour ça qu'on ne le ressaisit jamais. |
| **Stockage local** | Le navigateur garde le profil et les analyses sur l'ordinateur (localStorage). Rien ne part sur internet. |
| **Tests automatiques** | 73 petits programmes qui vérifient que chaque calcul donne le bon résultat. Si on modifie quelque chose et qu'on casse un calcul, un test le signale tout de suite. |
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

## 5. Ce qui a changé avec la refonte (octobre 2026)

**L'idée** : un vrai parcours, comme un rendez-vous avec un client, au lieu de 5 outils séparés.

- **Le profil est saisi une seule fois.** Canton, âge, frais médicaux, caisse actuelle, salaire : tous les outils les réutilisent.
  En haut de chaque outil, une barre rappelle le profil avec un bouton « Modifier ».
- **Parcours guidé en 4 étapes** : Profil → Résultats → Recommandations → Récapitulatif.
- **Chaque résultat a son « chiffre héros »** (le résultat principal en très grand) et une **prochaine étape** proposée.
- **Deux modes** dans l'en-tête : *Particulier* et *Conseiller*.

### Le nouvel outil « Lacune de prévoyance » (à savoir expliquer)

- **AVS (1er pilier)** : en 2027, la rente va de **1'280 à 2'560 francs par mois** selon le revenu, pour une carrière complète.
  Depuis décembre 2026, il y a une **13e rente** chaque année : en moyenne, c'est la rente × 13/12.
- **LPP (2e pilier)** :
  - on n'est assuré qu'au-dessus de **23'040 francs** de salaire par an (seuil d'entrée) ;
  - le **salaire coordonné** est le salaire moins **26'880 francs** (déduction de coordination), au maximum 65'280 ;
  - chaque année, on épargne un pourcentage de ce salaire coordonné : **7 %** de 25 à 34 ans, **10 %** de 35 à 44, **15 %** de 45 à 54, **18 %** dès 55 ans ;
  - à 65 ans, le capital est transformé en rente : **6,8 %** du capital par an (taux de conversion).
- **Lacune** = revenu souhaité (souvent 60 à 80 % du salaire) − AVS − LPP.
- **3a pour combler** : l'outil calcule combien verser chaque année. Maximum 2027 : **7'373 francs** avec caisse de pension ;
  sans caisse de pension, **20 % du revenu**, au plus **36'864 francs**. Si ça ne suffit pas, l'outil le dit : il faut alors un rachat LPP ou une épargne libre (3b).

**Exemple à savoir raconter** : un salarié de 30 ans qui gagne 72'000 francs touchera environ 2'480 francs d'AVS (avec la 13e rente)
et 1'430 francs de LPP par mois. S'il veut 70 % de son salaire (4'200 francs), il lui manque environ 290 francs par mois.
Un 3a d'environ 1'400 francs par an suffit à combler cet écart, et lui fait en plus économiser des impôts.

### Le mode conseiller

- **Tableau de bord** : nombre d'analyses et économie totale trouvée pour les clients.
- **Sauvegarder l'analyse** : garde le profil et les résultats clés, **dans le navigateur uniquement** (rien n'est envoyé).
- **Mes analyses** : retrouver, dupliquer ou supprimer un client.
- **Fiche client** : une page A4 à imprimer ou enregistrer en PDF.
- **Mode présentation** : cache les menus et grossit les chiffres pour tourner l'écran vers le client. **Échap** pour quitter.
- **Ctrl+K** : chercher un outil ou un client au clavier.
- **Clients fictifs** : un bouton du tableau de bord ajoute 3 exemples inventés pour une démo. Dis bien qu'ils sont fictifs.

**Question probable : « Où sont stockées les données des clients ? »**
> Uniquement dans le navigateur de l'ordinateur, avec le stockage local. Il n'y a ni serveur ni compte. Pour un vrai cabinet,
> il faudrait bien sûr un système sécurisé conforme à la protection des données ; ici, c'est une démonstration.

**Question probable : « Pourquoi deux modes ? »**
> Parce qu'il y a deux publics. Le particulier veut une réponse rapide : combien il peut économiser. Le conseiller veut préparer
> son rendez-vous, garder une trace et remettre un document au client.

---

## 6. Démonstration en 2 minutes (avec le nouveau parcours)

1. **Accueil (15 secondes)** : montrer la phrase d'accroche. À Neuchâtel, l'écart atteint **CHF 2'539 par an** pour la même assurance de base.
   Ce chiffre est calculé en direct à partir des données officielles.
2. **« Comparer ma prime » (45 secondes)** : remplir le profil (Neuchâtel, 30 ans, caisse actuelle CSS franchise 300), puis « Voir mes résultats ».
   Montrer l'économie en grand, la comparaison aujourd'hui / conseillé et les 5 meilleures offres.
3. **Recommandations et récapitulatif (30 secondes)** : franchise conseillée, besoins couverts, lacune de retraite, plan d'action.
   Passer à l'étape 4.
4. **Mode conseiller (30 secondes)** : basculer sur « Conseiller », cliquer « Sauvegarder l'analyse », ouvrir la **fiche client**
   et montrer l'aperçu avant impression. Terminer avec le **mode présentation**.

Phrase de conclusion possible :
> « Ce que je veux montrer, c'est ma façon de travailler avec un client : partir de sa situation, rendre les chiffres clairs
> et repartir avec des actions concrètes. »
