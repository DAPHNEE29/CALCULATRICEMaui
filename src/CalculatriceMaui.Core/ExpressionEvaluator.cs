using System.Globalization;

namespace CalculatriceMaui.Core;

/// <summary>
/// Évalue une expression arithmétique composée de nombres, de <c>+</c>,
/// <c>-</c>, <c>*</c> et <c>/</c>, en respectant les priorités et les
/// associations de gauche à droite.
/// <para>
/// L'analyse est purely textuelle : elle n'utilise jamais la culture
/// d'affichage, le point décimal de l'expression étant toujours
/// <see cref="CultureInfo.InvariantCulture"/>.
/// </para>
/// </summary>
public static class ExpressionEvaluator
{
	private const char Negation = 'n';
	private const char PlusUnaire = 'p';

	/// <summary>
	/// Tente d'évaluer l'expression. En cas d'expression mal formée, de division
	/// par zéro ou de dépassement de capacité, <paramref name="erreur"/> décrit
	/// la cause et la méthode renvoie <see langword="false"/>.
	/// </summary>
	public static bool TryEvaluate(string? expression, out double resultat, out CalculatorError erreur)
	{
		resultat = 0d;
		erreur = CalculatorError.ExpressionInvalide;

		if (string.IsNullOrWhiteSpace(expression))
		{
			return false;
		}

		Stack<double> valeurs = new();
		Stack<char> operateurs = new();

		int i = 0;
		bool precedentEstOperateur = true;
		bool precedentEstNombre = false;

		while (i < expression.Length)
		{
			char c = expression[i];

			if (char.IsWhiteSpace(c))
			{
				i++;
				continue;
			}

			if (char.IsDigit(c) || c == '.')
			{
				int debut = i;

				while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
				{
					i++;
				}

				string brut = expression[debut..i];

				if (!NumberFormatter.TryParseNombre(brut, out double nombre))
				{
					return false;
				}

				valeurs.Push(nombre);
				precedentEstOperateur = false;
				precedentEstNombre = true;
				continue;
			}

			if (c is '+' or '-' or '*' or '/')
			{
				// Un signe en début d'expression ou juste après un opérateur est
				// unaire : il s'applique au prochain nombre.
				bool unaire = !precedentEstNombre;

				if (unaire)
				{
					if (c is not '+' and not '-')
					{
						return false;
					}

					// Un signe unaire est marqué « n » (négation) ou « p »
					// (signe plus sans effet) pour le distinguer d'un opérateur.
					operateurs.Push(c == '-' ? Negation : PlusUnaire);
					precedentEstOperateur = true;
					i++;
					continue;
				}

				while (operateurs.Count > 0 && Priorite(operateurs.Peek()) >= Priorite(c))
				{
					if (!Depiler(operateurs, valeurs, out erreur))
					{
						return false;
					}
				}

				operateurs.Push(c);
				precedentEstOperateur = true;
				precedentEstNombre = false;
				i++;
				continue;
			}

			// Caractère non reconnu (parenthèse, puissance, lettres…).
			return false;
		}

		if (precedentEstOperateur || valeurs.Count == 0)
		{
			return false;
		}

		while (operateurs.Count > 0)
		{
			if (!Depiler(operateurs, valeurs, out erreur))
			{
				return false;
			}
		}

		if (valeurs.Count != 1)
		{
			return false;
		}

		resultat = valeurs.Pop();

		if (double.IsNaN(resultat) || double.IsInfinity(resultat))
		{
			resultat = 0d;
			erreur = CalculatorError.Debordement;
			return false;
		}

		erreur = CalculatorError.Aucune;
		return true;
	}

	private static bool Depiler(Stack<char> operateurs, Stack<double> valeurs, out CalculatorError erreur)
	{
		erreur = CalculatorError.ExpressionInvalide;

		if (operateurs.Count == 0)
		{
			return false;
		}

		char operateur = operateurs.Pop();

		// Signe unaire : il ne consomme qu'une seule valeur.
		if (operateur is Negation or PlusUnaire)
		{
			if (valeurs.Count < 1)
			{
				return false;
			}

			double seule = valeurs.Pop();
			valeurs.Push(operateur == Negation ? -seule : seule);
			return true;
		}

		if (operateur is '+' or '-')
		{
			if (valeurs.Count < 2)
			{
				return false;
			}

			double droite = valeurs.Pop();
			double gauche = valeurs.Pop();
			valeurs.Push(operateur == '+' ? gauche + droite : gauche - droite);
			return true;
		}

		if (valeurs.Count < 2)
		{
			return false;
		}

		// L'ordre des opérandes est essentiel pour « / » : la pile rend d'abord
		// l'opérande de droite, puis celui de gauche.
		double operandeDroite = valeurs.Pop();
		double operandeGauche = valeurs.Pop();

		if (operateur == '*')
		{
			valeurs.Push(operandeGauche * operandeDroite);
			return true;
		}

		if (operandeDroite == 0d)
		{
			erreur = CalculatorError.DivisionParZero;
			return false;
		}

		valeurs.Push(operandeGauche / operandeDroite);
		return true;
	}

	private static int Priorite(char operateur) => operateur is '+' or '-' ? 1 : 2;
}
