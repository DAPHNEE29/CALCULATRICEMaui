using CalculatriceMaui.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CalculatriceMaui.ViewModels;

/// <summary>
/// Expose l'état du moteur de calcul à l'interface sous forme de propriétés
/// bindables. Le code-behind se contente d'appeler les méthodes de saisie :
/// aucune logique de calcul ne se trouve dans la page.
/// </summary>
public sealed class CalculatorViewModel : INotifyPropertyChanged
{
	private readonly CalculatorEngine _moteur;

	public CalculatorViewModel(CalculatorEngine moteur)
	{
		_moteur = moteur ?? throw new ArgumentNullException(nameof(moteur));
		_moteur.StateChanged += SurEtatChange;
	}

	/// <summary>Moteur sous-jacent, également utilisé par la page Historique.</summary>
	public CalculatorEngine Moteur => _moteur;

	/// <summary>Opération en cours, affichée au-dessus du résultat.</summary>
	public string ExpressionText => _moteur.ExpressionText;

	/// <summary>Nombre saisi ou aperçu du résultat.</summary>
	public string DisplayText => _moteur.DisplayText;

	/// <summary>Message d'erreur, chaîne vide si tout va bien.</summary>
	public string StatusText => _moteur.StatusText;

	/// <summary>Commande l'affichage du message d'erreur.</summary>
	public bool HasError => _moteur.HasError;

	/// <summary>Commande l'affichage de la pastille « M ».</summary>
	public bool IsMemoryUsed => _moteur.IsMemoryUsed;

	/// <summary>Valeur stockée en mémoire.</summary>
	public string MemoryText => _moteur.MemoryText;

	/// <summary>Historique des calculs, alimenté directement par le moteur.</summary>
	public ObservableCollection<CalculationHistoryEntry> History => _moteur.Historique;

	public event PropertyChangedEventHandler? PropertyChanged;

	// ------------------------------------------------------------------
	// Saisie — appelées par les gestionnaires d'événements de MainPage
	// ------------------------------------------------------------------

	public void SaisirChiffre(char chiffre) => _moteur.SaisirChiffre(chiffre);

	public void SaisirVirgule() => _moteur.SaisirVirgule();

	public void SaisirOperateur(CalculatorOperator operateur) => _moteur.SaisirOperateur(operateur);

	public void Calculer() => _moteur.Calculer();

	public void ChangerLeSigne() => _moteur.ChangerLeSigne();

	public void AppliquerPourcentage() => _moteur.AppliquerPourcentage();

	public void EffacerLeDernierCaractere() => _moteur.EffacerLeDernierCaractere();

	public void EffacerLEntree() => _moteur.EffacerLEntree();

	public void ToutEffacer() => _moteur.ToutEffacer();

	public void AppliquerFonction(CalculatorFunction fonction) => _moteur.AppliquerFonction(fonction);

	public void MemoireAjouter() => _moteur.MemoireAjouter();

	public void MemoireRappeler() => _moteur.MemoireRappeler();

	public void MemoireEffacer() => _moteur.MemoireEffacer();

	public void EffacerHistorique() => _moteur.EffacerHistorique();

	/// <summary>Colle un texte du presse-papiers dans l'expression en cours.</summary>
	public bool CollerTexte(string? texte) => _moteur.CollerTexte(texte);

	public void Restaurer(CalculationHistoryEntry entree) => _moteur.RestaurerDepuisHistorique(entree);

	/// <summary>Valeur affichable, destinée au presse-papiers.</summary>
	public string ResultatCopiable => _moteur.LastResult.HasValue
		? NumberFormatter.Format(_moteur.LastResult.Value)
		: _moteur.DisplayText;

	private void SurEtatChange(object? sender, EventArgs e)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExpressionText)));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasError)));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMemoryUsed)));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MemoryText)));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultatCopiable)));
	}
}
