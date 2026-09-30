using CalculatriceMaui.Core;
using CalculatriceMaui.ViewModels;

namespace CalculatriceMaui.Pages;

/// <summary>
/// Historique des calculs. La page lit la même collection que la calculatrice :
/// une ligne sélectionnée recharge son résultat dans l'expression en cours.
/// </summary>
public partial class HistoryPage : ContentPage
{
	private readonly CalculatorViewModel _viewModel;

	public HistoryPage(CalculatorViewModel viewModel)
	{
		InitializeComponent();

		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		MettreAJourResume();
	}

	private void MettreAJourResume()
	{
		int total = _viewModel.History.Count;

		LabelResume.Text = total == 0
			? string.Empty
			: $"{total} calcul{(total > 1 ? "s" : string.Empty)}";

		EtatVide.IsVisible = total == 0;
		ListeHistorique.IsVisible = total > 0;
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not CalculationHistoryEntry entree)
		{
			return;
		}

		ListeHistorique.SelectedItem = null;
		_viewModel.Restaurer(entree);
		MettreAJourResume();

		if (Navigation.NavigationStack.Count > 1)
		{
			Navigation.PopAsync();
		}
	}

	private void OnToutEffacerClicked(object? sender, EventArgs e)
	{
		_viewModel.EffacerHistorique();
		MettreAJourResume();
	}
}
