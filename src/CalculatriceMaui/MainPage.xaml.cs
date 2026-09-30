using CalculatriceMaui.Core;
using CalculatriceMaui.ViewModels;

namespace CalculatriceMaui;

public partial class MainPage : ContentPage
{
	private const double TaillePolicePortrait = 40;
	private const double TaillePolicePaysage = 30;

	private readonly CalculatorViewModel _viewModel;

	private bool _dispositionPaysage;
	private bool _estPaysage;

	public MainPage(CalculatorViewModel viewModel)
	{
		InitializeComponent();

		_viewModel = viewModel;
		BindingContext = viewModel;

		// La place réellement obtenue par une rangée de touches n'est connue
		// qu'après la mise en page : le redimensionnement des touches est donc
		// relancé à chaque changement de taille du pavé.
		PaveNumerique.SizeChanged += (_, _) => AdapterTouches();
	}

	// ------------------------------------------------------------------
	// Adaptation à la rotation de l'appareil
	// ------------------------------------------------------------------

	/// <summary>
	/// Appelé par MAUI à chaque changement de taille de la fenêtre. En paysage,
	/// l'affichage passe à gauche et le pavé à droite ; en portrait, la
	/// disposition verticale est rétablie. La taille du résultat est adaptée
	/// pour qu'un nombre long ne déborde jamais.
	/// </summary>
	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);

		if (width <= 0 || height <= 0)
		{
			return;
		}

		AppliquerDisposition(width > height, width, height);
	}

	private void AppliquerDisposition(bool paysage, double largeur, double hauteur)
	{
		// En paysage, la grille racine passe à deux colonnes ; en portrait,
		// l'affichage occupe la ligne du haut et le pavé la ligne du bas.
		_dispositionPaysage = paysage;

		if (paysage)
		{
			RacineLigne0.Height = GridLength.Star;
			RacineColonne0.Width = new GridLength(1, GridUnitType.Star);
			RacineColonne1.Width = new GridLength(1.15, GridUnitType.Star);

			Grid.SetRow(ZoneAffichage, 0);
			Grid.SetColumn(ZoneAffichage, 0);
			ZoneAffichage.Padding = new Thickness(largeur * 0.03, 0, 8, 0);
			ZoneAffichage.MaximumHeightRequest = hauteur;

			Grid.SetRow(PaveNumerique, 0);
			Grid.SetColumn(PaveNumerique, 1);
			PaveNumerique.Padding = new Thickness(0, 0, largeur * 0.03, 0);
		}
		else
		{
			RacineLigne0.Height = GridLength.Auto;
			RacineColonne0.Width = new GridLength(1, GridUnitType.Star);
			RacineColonne1.Width = new GridLength(0);

			Grid.SetRow(ZoneAffichage, 0);
			Grid.SetColumn(ZoneAffichage, 0);
			ZoneAffichage.Padding = new Thickness(0);

			// L'affichage ne prend jamais plus d'un tiers de la hauteur : le pavé
			// reste ainsi entièrement visible même sur une fenêtre basse. Au-delà,
			// c'est la zone d'affichage qui défile, pas le pavé qui est rogné.
			ZoneAffichage.MaximumHeightRequest = Math.Min(260, hauteur * 0.30);

			Grid.SetRow(PaveNumerique, 1);
			Grid.SetColumn(PaveNumerique, 0);
			PaveNumerique.Padding = new Thickness(12, 0, 12, 12);
		}

		_estPaysage = paysage;
		AjusterPolice(hauteur);
		AdapterTouches();
	}

	/// <summary>
	/// Règle la taille du texte et des marges de chaque touche sur la hauteur
	/// réellement disponible pour une rangée. Les touches rétrécissent donc avec
	/// la fenêtre au lieu d'imposer un plancher de taille : sur un petit écran
	/// elles restent toutes visibles, sur un grand écran elles restent
	/// confortables.
	/// </summary>
	private void AdapterTouches()
	{
		// Place réellement disponible pour les six rangées, une fois les marges
		// du pavé et les cinq espacements retirés.
		double hauteurUtile = PaveNumerique.Height
			- PaveNumerique.Padding.VerticalThickness
			- PaveNumerique.RowSpacing * 5;

		// Les six rangées se partagent l'espace au prorata de leur poids :
		// 0,85 pour la rangée des fonctions avancées, 1 pour les cinq autres.
		const double poidsTotal = 5.85;

		double hauteurChiffre = hauteurUtile / poidsTotal;
		double hauteurFonction = (hauteurChiffre * 0.85) - 8;

		if (hauteurChiffre <= 0)
		{
			// La mise en page n'est pas terminée : on retentera au prochain
			// changement de taille.
			return;
		}

		double policeChiffre = Math.Clamp(hauteurChiffre * 0.40, 10, 26);
		double policeSymbole = Math.Clamp(hauteurChiffre * 0.48, 11, 30);
		double policeFonction = Math.Clamp(hauteurFonction * 0.46, 9, 18);
		double marges = Math.Clamp(hauteurChiffre * 0.05, 0, 10);

		foreach (Button bouton in PaveNumerique.GetVisualTreeDescendants().OfType<Button>())
		{
			bouton.Padding = new Thickness(0, marges);

			bouton.FontSize = (bouton.CommandParameter as string) switch
			{
				"+" or "−" or "×" or "÷" or "=" => policeSymbole,
				"x2" or "Racine" or "Inverse" or "Pi" or "e"
					or "AC" or "CE" or "Retour" or "%" => policeFonction,
				_ => policeChiffre
			};
		}
	}

	/// <summary>Réduit la police du résultat quand la hauteur d'écran est faible.</summary>
	private void AjusterPolice(double hauteur)
	{
		double maximum = _estPaysage ? TaillePolicePaysage : TaillePolicePortrait;
		double cible = _estPaysage ? hauteur * 0.18 : hauteur * 0.13;
		double taille = Math.Clamp(cible, 22, maximum);

		if (Math.Abs(LabelResultat.FontSize - taille) > 0.5)
		{
			LabelResultat.FontSize = taille;
		}
	}

	// ------------------------------------------------------------------
	// Saisie : chaque touche ne fait qu'aiguiller vers le ViewModel
	// ------------------------------------------------------------------

	private void OnChiffreTouche(object? sender, EventArgs e)
	{
		if (TryParametre(sender, out string? parametre) && parametre.Length > 0)
		{
			_viewModel.SaisirChiffre(parametre[0]);
			RetourHaptique();
		}
	}

	private void OnOperateurTouche(object? sender, EventArgs e)
	{
		if (!TryParametre(sender, out string? parametre))
		{
			return;
		}

		CalculatorOperator? operateur = parametre switch
		{
			"+" => CalculatorOperator.Addition,
			"−" or "-" => CalculatorOperator.Soustraction,
			"×" or "*" => CalculatorOperator.Multiplication,
			"÷" or "/" => CalculatorOperator.Division,
			_ => null
		};

		if (operateur is not null)
		{
			_viewModel.SaisirOperateur(operateur.Value);
			RetourHaptique();
		}
	}

	private void OnActionTouche(object? sender, EventArgs e)
	{
		if (!TryParametre(sender, out string? parametre))
		{
			return;
		}

		switch (parametre)
		{
			case "=":
				_viewModel.Calculer();
				break;

			case ",":
				_viewModel.SaisirVirgule();
				break;

			case "AC":
				_viewModel.ToutEffacer();
				break;

			case "CE":
				_viewModel.EffacerLEntree();
				break;

			case "Retour":
				_viewModel.EffacerLeDernierCaractere();
				break;

			case "%":
				_viewModel.AppliquerPourcentage();
				break;

			case "±":
				_viewModel.ChangerLeSigne();
				break;

			case "x2":
				_viewModel.AppliquerFonction(CalculatorFunction.Carre);
				break;

			case "Racine":
				_viewModel.AppliquerFonction(CalculatorFunction.RacineCarree);
				break;

			case "Inverse":
				_viewModel.AppliquerFonction(CalculatorFunction.Inverse);
				break;

			case "Pi":
				_viewModel.AppliquerFonction(CalculatorFunction.Pi);
				break;

			case "e":
				_viewModel.AppliquerFonction(CalculatorFunction.E);
				break;

			default:
				return;
		}

		RetourHaptique();
		AnnoncerResultat();
	}

	// ------------------------------------------------------------------
	// Barre d'outils
	// ------------------------------------------------------------------

	private void OnHistoriqueTouche(object? sender, EventArgs e)
	{
		RetourHaptique();
		Navigation.PushAsync(new Pages.HistoryPage(_viewModel));
	}

	private async void OnCopierTouche(object? sender, EventArgs e)
	{
		RetourHaptique();

		try
		{
			await Clipboard.SetTextAsync(_viewModel.ResultatCopiable);
			Annoncer("Résultat copié");
		}
		catch (Exception)
		{
			// Le presse-papiers peut être indisponible : on ignore sans planter.
		}
	}

	/// <summary>
	/// Colle le contenu du presse-papiers dans l'expression en cours. Le
	/// nettoyage du texte est confié au moteur, qui ignore ce qu'il ne sait
	/// pas interpréter ; la page ne fait que lire le presse-papiers.
	/// </summary>
	private async void OnCollerTouche(object? sender, EventArgs e)
	{
		RetourHaptique();

		string? texte;

		try
		{
			texte = await Clipboard.GetTextAsync();
		}
		catch (Exception)
		{
			// Presse-papiers inaccessible : aucun message, aucun plantage.
			return;
		}

		if (_viewModel.CollerTexte(texte))
		{
			Annoncer("Collé");
		}

		AnnoncerResultat();
	}

	private void OnMemoirePlusTouche(object? sender, EventArgs e)
	{
		_viewModel.MemoireAjouter();
		RetourHaptique();
	}

	private void OnMemoireRappelTouche(object? sender, EventArgs e)
	{
		_viewModel.MemoireRappeler();
		RetourHaptique();
	}

	private void OnMemoireEffacerTouche(object? sender, EventArgs e)
	{
		_viewModel.MemoireEffacer();
		RetourHaptique();
	}

	/// <summary>Force le thème clair ou sombre, quel que soit celui du système.</summary>
	private void OnThemeTouche(object? sender, EventArgs e)
	{
		if (Application.Current is not null)
		{
			Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Dark
				? AppTheme.Light
				: AppTheme.Dark;
		}

		RetourHaptique();
	}

	// ------------------------------------------------------------------
	// Retours utilisateur
	// ------------------------------------------------------------------

	private void RetourHaptique()
	{
		try
		{
			HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		}
		catch (Exception)
		{
			// Le retour haptique n'existe pas sur toutes les plateformes.
		}
	}

	private void AnnoncerResultat() =>
		Annoncer(_viewModel.HasError
			? _viewModel.StatusText
			: _viewModel.DisplayText);

	private void Annoncer(string texte)
	{
		try
		{
			SemanticScreenReader.Announce(texte);
		}
		catch (Exception)
		{
			// L'accessibilité peut être absente : sans conséquence.
		}
	}

	private static bool TryParametre(object? sender, out string parametre)
	{
		parametre = (sender as Button)?.CommandParameter as string ?? string.Empty;
		return parametre.Length > 0;
	}
}
