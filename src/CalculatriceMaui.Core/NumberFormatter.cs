using System.Globalization;
using System.Text;

namespace CalculatriceMaui.Core;

/// <summary>
/// Mise en forme des nombres pour l'affichage et analyse d'une expression en
/// cours de saisie.
/// <para>
/// L'analyse se fait toujours en <see cref="CultureInfo.InvariantCulture"/>
/// (point décimal) ; seule la présentation utilise la culture française
/// (virgule décimale). Cette séparation garantit qu'une expression reste
/// valide quelle que soit la langue du téléphone.
/// </para>
/// </summary>
public static class NumberFormatter
{
	/// <summary>Nombre de chiffres significatifs conservés à l'affichage.</summary>
	public const int ChiffresSignificatifs = 12;

	private const double SeuilExposantInf = 1e-9;
	private const double SeuilExposantSup = 1e12;

	/// <summary>
	/// Met une valeur en forme pour l'affichage français : virgule décimale,
	/// pas de zéros inutiles, notation scientifique aux extrêmes.
	/// </summary>
	public static string Format(double valeur)
	{
		if (double.IsNaN(valeur) || double.IsInfinity(valeur))
		{
			return "Erreur";
		}

		if (valeur == 0d)
		{
			return "0";
		}

		double arrondi = Math.Round(valeur, ChiffresSignificatifs - 1, MidpointRounding.AwayFromZero);
		double absolu = Math.Abs(arrondi);

		if (absolu >= SeuilExposantSup || absolu < SeuilExposantInf)
		{
			string scientifique = arrondi
				.ToString("0.###########E+0", CultureInfo.InvariantCulture)
				.Replace("E+", "e+");

			return scientifique.Replace(".", ",");
		}

		return arrondi
			.ToString("0.##############", CultureInfo.InvariantCulture)
			.Replace(".", ",");
	}

	/// <summary>
	/// Analyse un nombre stocké dans l'expression. L'expression utilise toujours
	/// le point décimal, quelle que soit la culture d'affichage.
	/// </summary>
	public static bool TryParseNombre(string? texte, out double valeur)
	{
		valeur = 0d;

		if (string.IsNullOrEmpty(texte))
		{
			return false;
		}

		return double.TryParse(
			texte,
			NumberStyles.Float,
			CultureInfo.InvariantCulture,
			out valeur);
	}

	/// <summary>
	/// Extrait le nombre en cours de saisie (le dernier terme de l'expression),
	/// sans sa partie opérateur éventuelle.
	/// </summary>
	public static string? NombreEnCoursBrut(string expression)
	{
		if (string.IsNullOrEmpty(expression))
		{
			return null;
		}

		int debut = 0;

		for (int i = expression.Length - 1; i >= 0; i--)
		{
			if (EstOperateur(expression[i]))
			{
				// Un « - » unaire (en tête ou après un opérateur) appartient au
				// terme ; sinon l'opérateur est binaire et le terme suit.
				bool unaire = expression[i] == '-'
					&& (i == 0 || EstOperateur(expression[i - 1]));

				debut = unaire ? i : i + 1;
				break;
			}
		}

		string nombre = expression[debut..];
		return nombre.Length == 0 || nombre == "-" ? null : nombre;
	}

	/// <summary>Indique si le caractère est un opérateur binaire.</summary>
	public static bool EstOperateur(char caractere) =>
		caractere is '+' or '-' or '*' or '/';

	/// <summary>
	/// Version lisible d'une expression : nombres mis en forme à la française,
	/// espaces autour des opérateurs binaires et signes unaires accolés.
	/// </summary>
	public static string Prettify(string? expression)
	{
		if (string.IsNullOrEmpty(expression))
		{
			return string.Empty;
		}

		StringBuilder sortie = new();
		int i = 0;

		while (i < expression.Length)
		{
			char c = expression[i];

			if (char.IsDigit(c) || c == '.')
			{
				int debut = i;

				while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
				{
					i++;
				}

				string brut = expression[debut..i];

				if (TryParseNombre(brut, out double nombre))
				{
					sortie.Append(Format(nombre));

					// Une virgule en cours de saisie reste visible : « 0, ».
					if (brut.EndsWith('.'))
					{
						sortie.Append(',');
					}
				}
				else
				{
					sortie.Append(brut);
				}

				continue;
			}

			bool unaire = EstOperateurUnaire(expression, i);

			// Un opérateur binaire est précédé d'une espace, sauf en début
			// d'expression : « 12 + 3 » et non « 12+ 3 ».
			if (!unaire && sortie.Length > 0 && sortie[^1] != ' ')
			{
				sortie.Append(' ');
			}

			if (c == '*')
			{
				sortie.Append('×');
			}
			else if (c == '/')
			{
				sortie.Append('÷');
			}
			else
			{
				sortie.Append(c);
			}

			i++;

			// Un opérateur binaire est suivi d'une espace ; le TrimEnd final
			// évite les espaces en fin d'expression.
			if (!unaire)
			{
				sortie.Append(' ');
			}
		}

		return sortie.ToString().TrimEnd();
	}

	/// <summary>
	/// Vrai si l'opérateur est unaire, c'est-à-dire placé juste après un autre
	/// opérateur ou en début d'expression.
	/// </summary>
	public static bool EstOperateurUnaire(string expression, int index) =>
		index == 0 || (index > 0 && EstOperateur(expression[index - 1]));
}
