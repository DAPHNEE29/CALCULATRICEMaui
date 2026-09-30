using System.Globalization;

namespace CalculatriceMaui.Core;

/// <summary>Une ligne de l'historique : l'expression saisie et son résultat.</summary>
public sealed class CalculationHistoryEntry
{
	/// <summary>Crée une entrée d'historique.</summary>
	public CalculationHistoryEntry(string expression, double resultat)
	{
		ArgumentNullException.ThrowIfNull(expression);

		Expression = expression;
		Resultat = resultat;
		Horodatage = DateTime.Now;
	}

	/// <summary>Expression telle que l'utilisateur l'a saisie, formatée pour la lecture.</summary>
	public string Expression { get; }

	/// <summary>Valeur brute du résultat.</summary>
	public double Resultat { get; }

	/// <summary>Résultat mis en forme pour la lecture.</summary>
	public string ResultatTexte => NumberFormatter.Format(Resultat);

	/// <summary>Date et heure du calcul, affichées dans la page Historique.</summary>
	public DateTime Horodatage { get; }

	/// <summary>Heure du calcul au format court français.</summary>
	public string Heure => Horodatage.ToString("HH:mm:ss", CultureInfo.CurrentCulture);

	/// <summary>Texte affichable « expression = résultat ».</summary>
	public string LigneComplete => string.Concat(Expression, " = ", ResultatTexte);

	/// <inheritdoc />
	public override string ToString() => LigneComplete;
}
