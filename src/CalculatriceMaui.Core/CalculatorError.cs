namespace CalculatriceMaui.Core;

/// <summary>
/// Cause d'un échec de calcul. Le moteur convertit chaque valeur en un message
/// en français destiné à l'interface.
/// </summary>
public enum CalculatorError
{
	/// <summary>Aucune erreur : l'état courant est exploitable.</summary>
	Aucune,

	/// <summary>Division par zéro.</summary>
	DivisionParZero,

	/// <summary>Racine carrée d'un nombre négatif.</summary>
	RacineNegative,

	/// <summary>Expression incomplète ou mal formée.</summary>
	ExpressionInvalide,

	/// <summary>Résultat hors de la plage des nombres finis.</summary>
	Debordement,

	/// <summary>Fonction demandée alors qu'aucun nombre n'est disponible.</summary>
	FonctionIndisponible
}
