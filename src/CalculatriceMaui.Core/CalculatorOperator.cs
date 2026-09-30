namespace CalculatriceMaui.Core;

/// <summary>Opérateur binaire en cours de saisie.</summary>
public enum CalculatorOperator
{
	/// <summary>Aucun opérateur : la saisie attend un nombre.</summary>
	Aucun,

	/// <summary>Addition.</summary>
	Addition,

	/// <summary>Soustraction.</summary>
	Soustraction,

	/// <summary>Multiplication.</summary>
	Multiplication,

	/// <summary>Division.</summary>
	Division
}

/// <summary>Fonctions accessibles depuis la rangée avancée du pavé.</summary>
public enum CalculatorFunction
{
	/// <summary>Élévation au carré.</summary>
	Carre,

	/// <summary>Racine carrée.</summary>
	RacineCarree,

	/// <summary>Inverse (1/x).</summary>
	Inverse,

	/// <summary>Constante π.</summary>
	Pi,

	/// <summary>Constante e.</summary>
	E
}
