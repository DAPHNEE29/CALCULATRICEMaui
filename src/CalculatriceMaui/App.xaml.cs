using CalculatriceMaui.ViewModels;
using Microsoft.Maui.Devices;

namespace CalculatriceMaui;

public partial class App : Application
{
	private readonly CalculatorViewModel _viewModel;

	/// <summary>
	/// Le ViewModel est injecté puis transmis à la page principale, qui
	/// l'expose comme BindingContext : l'historique et la mémoire sont ainsi
	/// conservés d'une page à l'autre.
	/// </summary>
	public App(CalculatorViewModel viewModel)
	{
		InitializeComponent();

		_viewModel = viewModel;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// La page principale est enveloppée dans une NavigationPage : c'est
		// elle qui fournit la pile de navigation. Sans elle, l'appel à
		// Navigation.PushAsync du bouton « Historique » n'a aucune pile sur
		// laquelle pousser la page et la fenêtre se ferme.
		var navigation = new NavigationPage(new MainPage(_viewModel));

		var window = new Window(navigation)
		{
			Title = "Calculatrice"
		};

		DimensionnerFenetre(window);

		return window;
	}

	/// <summary>
	/// Choisit une taille de fenêtre qui rappelle un téléphone en portrait sans
	/// dépasser l'écran. La mise en page étant adaptative, ces dimensions ne
	/// servent qu'à proposer un bon point de départ : l'utilisateur peut
	/// redimensionner la fenêtre librement.
	/// </summary>
	private static void DimensionnerFenetre(Window window)
	{
#if WINDOWS
		// La fenêtre s'ouvrait trop basse sur un écran de bureau : le pavé
		// était alors rogné en bas. Les valeurs demandées sont exprimées en
		// unités indépendantes de la résolution, alors que la fenêtre les
		// convertit ensuite en pixels selon l'échelle d'affichage. Sur un écran
		// agrandi, une hauteur fixe dépassait donc la hauteur disponible et la
		// dernière rangée de touches sortait de l'écran, hors de tout clic.
		// On limite donc la taille à une fraction de l'écran réellement
		// disponible, en convertissant les pixels de l'écran via sa densité.
		var ecran = DeviceDisplay.Current.MainDisplayInfo;
		if (ecran.Density <= 0)
		{
			return;
		}

		window.Width = Math.Min(420, ecran.Width / ecran.Density * 0.45);
		window.Height = Math.Min(880, ecran.Height / ecran.Density * 0.90);
#endif
	}
}
