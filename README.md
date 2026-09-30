# Activité 4 — Calculatrice .NET MAUI

Calculatrice tactile réalisée avec .NET MAUI (C# / XAML) dans le cadre de l'atelier
de développement mobile.

## Fonctionnalités

- Saisie chiffrée avec virgule décimale, quatre opérations (`+`, `−`, `×`, `÷`)
  et priorités opératoires.
- Pourcentage, changement de signe, effacement de l'entrée courante (`CE`),
  retour arrière (`⌫`) et tout effacement (`AC`).
- Fonctions avancées : carré (`x²`), racine carrée (`√x`), inverse (`1/x`),
  constantes `π` et `e`. Chacune occupe sa propre case, alignée sur la même
  grille que les chiffres et les opérateurs.
- Presse-papiers : `Copier` place le résultat dans le presse-papiers, `Coller`
  y prend un nombre ou une expression. Le texte collé est nettoyé par le moteur
  (symboles `×` `÷` `−` et virgule décimale française convertis, espaces de
  milliers et caractères inconnus retirés) puis injecté par les méthodes de saisie
  ordinaires, pour que toutes leurs règles restent valides.
- Mémoire : `M+` (ajouter), `MR` (rappeler), `MC` (vider), avec pastille
  indicatrice. **Ces trois touches sont masquées dans l'interface** — les
  fonctionnalités avancées ne sont pas obligatoires dans l'énoncé — mais le
  moteur, le ViewModel et les 6 tests associés sont conservés. Pour les
  réafficher, passer `IsVisible` à `True` dans `MainPage.xaml`.
- Historique persistant des calculs, consultable et restaurable en un appui.
- Gestion des erreurs : division par zéro, racine d'un nombre négatif,
  dépassement de la plage des nombres.
- Thème clair / sombre, retour haptique, annonces d'accessibilité
  (`SemanticProperties`).

## Layouts utilisés

Six conteneurs différents sont employés sur la page principale (l'énoncé en
demande au moins quatre) :

| Layout | Rôle |
| --- | --- |
| `Grid` | racine adaptative, pavé numérique 4 colonnes × 6 lignes, rangée des 5 fonctions en 5 colonnes |
| `ScrollView` | zone d'affichage, défile au lieu de déborder |
| `Border` | cadre de l'affichage, avec coins arrondis |
| `VerticalStackLayout` | empilement des blocs de la zone d'affichage |
| `FlexLayout` | barre d'outils : passe à la ligne si l'écran est étroit |
| `HorizontalStackLayout` | ligne du message d'erreur |

## Architecture

```
Activite4_MAUI_Calculatrice/
├── CalculatriceMaui.sln
├── src/
│   ├── CalculatriceMaui/          application MAUI (XAML + code-behind)
│   │   ├── MainPage.xaml(.cs)     page principale, 6 layouts, orientation
│   │   ├── Pages/                 page Historique
│   │   ├── ViewModels/            CalculatorViewModel (INotifyPropertyChanged)
│   │   ├── Services/              persistance de l'historique (Preferences)
│   │   └── Resources/Styles/      palette et styles des touches
│   └── CalculatriceMaui.Core/     moteur de calcul pur, sans dépendance UI
└── tests/
    └── CalculatriceMaui.Core.Tests/   167 tests xUnit
```

Le moteur (`CalculatorEngine`) ne contient aucune référence à l'interface : il
peut donc être testé seul. La page appelle le ViewModel, qui relaie l'état du
moteur et notifie les changements.

La page principale est une `ContentPage` simple, enveloppée dans une
`NavigationPage` créée par `App.CreateWindow` : c'est cette `NavigationPage` qui
fournit la pile de navigation, sur laquelle la page Historique est poussée par
`Navigation.PushAsync` puis retirée par `Navigation.PopAsync`. Un `Shell`
n'apporterait rien ici.

## Prérequis

- .NET SDK 9.0
- Charge de travail MAUI : `dotnet workload install maui`
- Pour Windows : le SDK de développement Windows 10 (19041 ou plus)

## Compilation et tests

```bash
# Bibliothèque du moteur
dotnet build src/CalculatriceMaui.Core/CalculatriceMaui.Core.csproj

# Tests (167 tests)
dotnet test tests/CalculatriceMaui.Core.Tests/CalculatriceMaui.Core.Tests.csproj

# Application (Windows)
dotnet build src/CalculatriceMaui/CalculatriceMaui.csproj -f net9.0-windows10.0.19041.0

# Lancer l'application
dotnet run --project src/CalculatriceMaui/CalculatriceMaui.csproj -f net9.0-windows10.0.19041.0
```

Pour Android, remplacer `-f net9.0-windows10.0.19041.0` par
`-f net9.0-android` (un SDK Android et un JDK sont alors nécessaires).

## État de la remise

Ce que l'énoncé demande de déposer dans l'espace de cours, et où en est le projet.

| Livrable | État |
| --- | --- |
| Lien du dépôt GitHub **public** | **À faire** — voir ci-dessous |
| Vidéo MP4 ≤ 15 s (écran + webcam) | **À faire** — à enregistrer depuis l'app lancée |
| Rapport PDF d'une page | **À faire** — le tableau « Layouts utilisés » ci-dessus fournit les justifications exigées |

### Dépôt GitHub

Git n'était pas installé sur la machine. À installer depuis
<https://git-scm.com/download/win>, puis :

```bash
cd CalculatriceMaui
git init
git add .
git commit -m "Calculatrice .NET MAUI - Activite 4"
git branch -M main
git remote add origin <URL_DU_DEPOT_GITHUB_PUBLIC>
git push -u origin main
```

Le fichier `.gitignore` exclut déjà `bin/`, `obj/` et les dossiers de
plateforme, le dépôt ne contiendra donc que les sources.

## Résolution des problèmes identifiés

- **Crash au clic sur « Historique »** : la fenêtre racine était créée directement à partir de `MainPage`, alors que `OnHistoriqueTouche` appelait `Navigation.PushAsync`. L'application est désormais encapsulée dans `NavigationPage` dans `App.xaml.cs`. Cela permet d'ouvrir et de fermer l'historique sans planter l'application (vérifié par tests d'automatisation UIA).
- **Rognage du pavé et fenêtre trop grande pour l'écran** : la taille initiale de fenêtre Windows est calculée dynamiquement à partir de `DeviceDisplay.MainDisplayInfo` pour rester entièrement visible tout en conservant le format portrait. La   disposition reste responsive (testée entre 320×520 et plus grand).

### Points connus

- Trois avertissements `XC0022` subsistent sur `Pages/HistoryPage.xaml` : les
  bindings y sont résolus à l'exécution et non à la compilation. Ils n'affectent
  ni le fonctionnement ni la page principale, et le build ne remonte aucune
  erreur.

