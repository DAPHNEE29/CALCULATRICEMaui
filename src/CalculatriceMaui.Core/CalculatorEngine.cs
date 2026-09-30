using System.Collections.ObjectModel;
using System.Globalization;

namespace CalculatriceMaui.Core;

/// <summary>
/// Machine à états de la calculatrice : expression en cours, mémoire,
/// historique et dernière erreur.
/// <para>
/// Aucune référence à l'interface : ce type est directement testable. Le
/// code-behind de la page appelle ces méthodes et se contente de relire
/// <see cref="ExpressionText"/>, <see cref="DisplayText"/> et
/// <see cref="StatusText"/>.
/// </para>
/// </summary>
public sealed class CalculatorEngine
{
	/// <summary>Nombre maximal de chiffres saisis pour un même terme.</summary>
	public const int LongueurMaxTerme = 15;

	/// <summary>Nombre maximal de calculs conservés dans l'historique.</summary>
	public const int HistoriqueMax = 50;

	private readonly IHistoryStorage? _stockage;

	private string _expression = string.Empty;
	private double _memoire;
	private double? _dernierResultat;
	private CalculatorError _erreur = CalculatorError.Aucune;
	private bool _resultatFrais;

	/// <summary>Crée un moteur, éventuellement adossé à un stockage persistant.</summary>
	public CalculatorEngine(IHistoryStorage? stockage = null)
	{
		_stockage = stockage;
		Historique = new ObservableCollection<CalculationHistoryEntry>();

		ChargerHistorique();
	}

	/// <summary>Levé à chaque modification de l'état, pour rafraîchir l'interface.</summary>
	public event EventHandler? StateChanged;

	/// <summary>Historique, du plus récent au plus ancien.</summary>
	public ObservableCollection<CalculationHistoryEntry> Historique { get; }

	/// <summary>Opération en cours, mise en forme pour la lecture.</summary>
	public string ExpressionText => NumberFormatter.Prettify(_expression);

	/// <summary>Nombre saisi, ou dernier résultat affiché.</summary>
	public string DisplayText
	{
		get
		{
			if (_resultatFrais && _dernierResultat.HasValue)
			{
				return NumberFormatter.Format(_dernierResultat.Value);
			}

			string? terme = NumberFormatter.NombreEnCoursBrut(_expression);

			if (terme is not null && NumberFormatter.TryParseNombre(terme, out double valeur))
			{
				return NumberFormatter.Format(valeur);
			}

			return "0";
		}
	}

	/// <summary>Message d'erreur, chaîne vide si tout va bien.</summary>
	public string StatusText => _erreur switch
	{
		CalculatorError.DivisionParZero => "Division par zéro impossible",
		CalculatorError.RacineNegative => "Racine carrée impossible d'un nombre négatif",
		CalculatorError.ExpressionInvalide => "Expression incomplète",
		CalculatorError.Debordement => "Résultat hors de la plage des nombres",
		CalculatorError.FonctionIndisponible => "Saisissez d'abord un nombre",
		_ => string.Empty
	};

	/// <summary>Vrai tant qu'une erreur est active.</summary>
	public bool HasError => _erreur != CalculatorError.Aucune;

	/// <summary>Erreur courante, utile pour les tests et le journal technique.</summary>
	public CalculatorError Erreur => _erreur;

	/// <summary>Vrai si la mémoire contient une valeur.</summary>
	public bool IsMemoryUsed => _memoire != 0d;

	/// <summary>Valeur stockée en mémoire, mise en forme.</summary>
	public string MemoryText => NumberFormatter.Format(_memoire);

	/// <summary>Valeur brute en mémoire.</summary>
	public double Memoire => _memoire;

	/// <summary>Dernier résultat calculé, s'il existe.</summary>
	public double? LastResult => _dernierResultat;

	/// <summary>Expression brute, au format invariant (point décimal).</summary>
	public string Expression => _expression;

	// ------------------------------------------------------------------
	// Saisie
	// ------------------------------------------------------------------

	/// <summary>Ajoute un chiffre. Démarre une nouvelle saisie après un résultat.</summary>
	public void SaisirChiffre(char chiffre)
	{
		if (!char.IsDigit(chiffre))
		{
			return;
		}

		EffacerErreur();

		if (_resultatFrais)
		{
			_expression = chiffre.ToString(CultureInfo.InvariantCulture);
			_resultatFrais = false;
			_dernierResultat = null;
			Notifier();
			return;
		}

		string? terme = NumberFormatter.NombreEnCoursBrut(_expression);

		// Un « 0 » isolé est remplacé plutôt que prolongé : « 0 » puis « 5 »
		// donne « 5 », pas « 05 ».
		if (terme == "0" && chiffre != '0')
		{
			_expression = _expression[..^1] + chiffre;
			Notifier();
			return;
		}

		if (terme is not null && terme.Replace(".", string.Empty).Length >= LongueurMaxTerme)
		{
			return;
		}

		_expression += chiffre;
		Notifier();
	}

	/// <summary>Ajoute la virgule décimale au terme en cours.</summary>
	public void SaisirVirgule()
	{
		EffacerErreur();

		if (_resultatFrais)
		{
			_expression = "0.";
			_resultatFrais = false;
			_dernierResultat = null;
			Notifier();
			return;
		}

		string? terme = NumberFormatter.NombreEnCoursBrut(_expression);

		if (terme is not null && terme.Contains('.'))
		{
			return;
		}

		// Après un opérateur, la virgule s'ajoute au terme qui commence à « 0. ».
		_expression = _expression.Length == 0 ? "0." : _expression + ".";
		Notifier();
	}

	/// <summary>Enregistre l'opérateur binaire saisi, en remplaçant l'opérateur en attente.</summary>
	public void SaisirOperateur(CalculatorOperator operateur)
	{
		EffacerErreur();

		if (_resultatFrais)
		{
			// On poursuit le calcul à partir du dernier résultat.
			_expression = VersInvariant(_dernierResultat ?? 0d);
			_resultatFrais = false;
		}

		if (_expression.Length == 0)
		{
			return;
		}

		char symbole = Symbole(operateur);

		if (NumberFormatter.EstOperateur(_expression[^1]))
		{
			// Un signe unaire en attente n'est pas remplacé : « 5×- » reste valide.
			if (NumberFormatter.EstOperateurUnaire(_expression, _expression.Length - 1))
			{
				return;
			}

			_expression = _expression[..^1] + symbole;
			Notifier();
			return;
		}

		_expression += symbole;
		Notifier();
	}

	/// <summary>Évalue l'expression et mémorise le résultat.</summary>
	public void Calculer()
	{
		if (_expression.Length == 0 || NumberFormatter.EstOperateur(_expression[^1]))
		{
			_erreur = CalculatorError.ExpressionInvalide;
			Notifier();
			return;
		}

		if (!ExpressionEvaluator.TryEvaluate(_expression, out double resultat, out CalculatorError erreur))
		{
			_erreur = erreur == CalculatorError.Aucune ? CalculatorError.ExpressionInvalide : erreur;
			Notifier();
			return;
		}

		string expressionLisible = NumberFormatter.Prettify(_expression);

		_dernierResultat = resultat;
		_expression = VersInvariant(resultat);
		_resultatFrais = true;
		_erreur = CalculatorError.Aucune;

		AjouterALHistorique(new CalculationHistoryEntry(expressionLisible, resultat));
		Notifier();
	}

	/// <summary>Change le signe du terme en cours, ou du dernier résultat.</summary>
	public void ChangerLeSigne()
	{
		EffacerErreur();

		if (_resultatFrais)
		{
			if (!_dernierResultat.HasValue)
			{
				return;
			}

			_dernierResultat = -_dernierResultat.Value;
			_expression = VersInvariant(_dernierResultat.Value);
			Notifier();
			return;
		}

		string? terme = NumberFormatter.NombreEnCoursBrut(_expression);

		if (terme is null)
		{
			return;
		}

		int debut = _expression.Length - terme.Length;

		if (_expression[debut] == '-')
		{
			_expression = _expression.Remove(debut, 1);
		}
		else
		{
			_expression = _expression.Insert(debut, "-");
		}

		Notifier();
	}

	/// <summary>Divise le terme en cours par cent : « 200+10 % » vaut 200,1.</summary>
	public void AppliquerPourcentage()
	{
		EffacerErreur();

		if (_resultatFrais)
		{
			if (!_dernierResultat.HasValue)
			{
				_erreur = CalculatorError.FonctionIndisponible;
				Notifier();
				return;
			}

			double pourcentageFrais = _dernierResultat.Value / 100d;
			_dernierResultat = pourcentageFrais;
			_expression = VersInvariant(pourcentageFrais);
			Notifier();
			return;
		}

		int debut = 0;
		bool trouve = false;

		for (int i = _expression.Length - 1; i >= 0; i--)
		{
			if (NumberFormatter.EstOperateur(_expression[i]))
			{
				bool unaire = _expression[i] == '-'
					&& (i == 0 || NumberFormatter.EstOperateur(_expression[i - 1]));

				debut = unaire ? i : i + 1;
				trouve = true;
				break;
			}
		}

		if (!trouve)
		{
			debut = 0;
		}

		string terme = _expression[debut..];

		if (terme.Length == 0 || !NumberFormatter.TryParseNombre(terme, out double valeur))
		{
			_erreur = CalculatorError.ExpressionInvalide;
			Notifier();
			return;
		}

		_expression = _expression[..debut] + VersInvariant(valeur / 100d);
		Notifier();
	}

	/// <summary>Supprime le dernier caractère saisi.</summary>
	public void EffacerLeDernierCaractere()
	{
		EffacerErreur();

		if (_resultatFrais)
		{
			ToutEffacer();
			return;
		}

		if (_expression.Length == 0)
		{
			return;
		}

		_expression = _expression[..^1];

		// Ne pas laisser un opérateur nu en fin d'expression.
		if (_expression.Length > 0 && NumberFormatter.EstOperateur(_expression[^1]))
		{
			_expression = _expression[..^1];
		}

		if (_expression.Length == 0)
		{
			_expression = "0";
		}

		Notifier();
	}

	/// <summary>Supprime le terme en cours et laisse l'opérateur en place.</summary>
	public void EffacerLEntree()
	{
		EffacerErreur();

		if (_resultatFrais)
		{
			ToutEffacer();
			return;
		}

		if (_expression.Length == 0)
		{
			return;
		}

		int debut = 0;

		for (int i = _expression.Length - 1; i >= 0; i--)
		{
			if (NumberFormatter.EstOperateur(_expression[i]))
			{
				debut = i + 1;
				break;
			}
		}

		_expression = _expression[..debut];
		_expression = _expression.Length == 0 ? "0" : _expression;
		Notifier();
	}

	/// <summary>Remet l'expression, la mémoire et l'erreur à zéro.</summary>
	public void ToutEffacer()
	{
		_expression = string.Empty;
		_dernierResultat = null;
		_erreur = CalculatorError.Aucune;
		_resultatFrais = false;
		Notifier();
	}

	/// <summary>Applique une fonction au terme en cours.</summary>
	public void AppliquerFonction(CalculatorFunction fonction)
	{
		EffacerErreur();

		if (_resultatFrais && _dernierResultat.HasValue)
		{
			_expression = VersInvariant(_dernierResultat.Value);
			_resultatFrais = false;
			_dernierResultat = null;
		}

		int debut = 0;

		for (int i = _expression.Length - 1; i >= 0; i--)
		{
			if (NumberFormatter.EstOperateur(_expression[i]))
			{
				bool unaire = _expression[i] == '-'
					&& (i == 0 || NumberFormatter.EstOperateur(_expression[i - 1]));

				debut = unaire ? i : i + 1;
				break;
			}
		}

		string terme = _expression[debut..];

		// Une constante s'ajoute même sans nombre saisi.
		if (terme.Length == 0 && fonction is CalculatorFunction.Pi or CalculatorFunction.E)
		{
			_expression += VersInvariant(fonction == CalculatorFunction.Pi ? Math.PI : Math.E);
			Notifier();
			return;
		}

		if (terme.Length == 0 || !NumberFormatter.TryParseNombre(terme, out double valeur))
		{
			_erreur = CalculatorError.FonctionIndisponible;
			Notifier();
			return;
		}

		double resultat;

		switch (fonction)
		{
			case CalculatorFunction.Carre:
				resultat = valeur * valeur;
				break;

			case CalculatorFunction.RacineCarree:
				if (valeur < 0d)
				{
					_erreur = CalculatorError.RacineNegative;
					Notifier();
					return;
				}

				resultat = Math.Sqrt(valeur);
				break;

			case CalculatorFunction.Inverse:
				if (valeur == 0d)
				{
					_erreur = CalculatorError.DivisionParZero;
					Notifier();
					return;
				}

				resultat = 1d / valeur;
				break;

			default:
				_erreur = CalculatorError.FonctionIndisponible;
				Notifier();
				return;
		}

		_expression = _expression[..debut] + VersInvariant(resultat);

		if (double.IsInfinity(resultat) || double.IsNaN(resultat))
		{
			_erreur = CalculatorError.Debordement;
		}

		Notifier();
	}

	// ------------------------------------------------------------------
	// Mémoire
	// ------------------------------------------------------------------

	/// <summary>Ajoute la valeur courante à la mémoire.</summary>
	public void MemoireAjouter()
	{
		EffacerErreur();

		if (!TryValeurCourante(out double valeur))
		{
			_erreur = CalculatorError.FonctionIndisponible;
			Notifier();
			return;
		}

		_memoire += valeur;
		Notifier();
	}

	/// <summary>Rappelle la valeur en mémoire dans l'expression.</summary>
	public void MemoireRappeler()
	{
		EffacerErreur();

		if (!IsMemoryUsed)
		{
			_erreur = CalculatorError.FonctionIndisponible;
			Notifier();
			return;
		}

		string valeur = VersInvariant(_memoire);

		if (_resultatFrais || _expression.Length == 0)
		{
			_expression = valeur;
			_resultatFrais = false;
			_dernierResultat = null;
		}
		else if (NumberFormatter.EstOperateur(_expression[^1])
			&& !NumberFormatter.EstOperateurUnaire(_expression, _expression.Length - 1))
		{
			// L'opérateur est en attente : la valeur se colle simplement à lui.
			_expression += valeur;
		}
		else
		{
			// Un terme est déjà saisi : la mémoire multiplie ce terme.
			_expression += "*" + valeur;
		}

		Notifier();
	}

	/// <summary>Vide la mémoire.</summary>
	public void MemoireEffacer()
	{
		_memoire = 0d;
		Notifier();
	}

	// ------------------------------------------------------------------
	// Presse-papiers
	// ------------------------------------------------------------------

	/// <summary>Longueur maximale de texte collageable, pour borner la saisie.</summary>
	private const int LongueurMaxColler = 200;

	/// <summary>
	/// Colle un texte venu du presse-papiers dans l'expression en cours.
	/// <para>
	/// Le texte fourni est d'abord ramené au format interne : les opérateurs
	/// typographiques (×, ÷, −) deviennent leurs équivalents ASCII, la virgule
	/// décimale française devient un point, et tout ce que la calculatrice ne
	/// sait pas interpréter — espaces, lettres, parenthèses, séparateurs de
	/// milliers — est retiré. Le résultat est ensuite relayé par les méthodes de
	/// saisie ordinaires, ce qui conserve toutes leurs règles (longueur maximale
	/// d'un terme, virgule unique, opérateur en attente).
	/// </para>
	/// <para>
	/// Coller remplace la saisie en cours et ne valide pas le calcul : la touche
	/// « = » reste à l'utilisateur.
	/// </para>
	/// </summary>
	/// <returns>Vrai si au moins un caractère exploitable a été collé.</returns>
	public bool CollerTexte(string? texte)
	{
		EffacerErreur();

		string nettoye = NettoyerTexteAColler(texte);

		if (nettoye.Length == 0)
		{
			_erreur = CalculatorError.ExpressionInvalide;
			Notifier();
			return false;
		}

		_expression = string.Empty;
		_dernierResultat = null;
		_resultatFrais = false;

		foreach (char caractere in nettoye)
		{
			switch (caractere)
			{
				case '+':
					SaisirOperateur(CalculatorOperator.Addition);
					break;
				case '-':
					SaisirOperateur(CalculatorOperator.Soustraction);
					break;
				case '*':
					SaisirOperateur(CalculatorOperator.Multiplication);
					break;
				case '/':
					SaisirOperateur(CalculatorOperator.Division);
					break;
				case '.':
					SaisirVirgule();
					break;
				default:
					SaisirChiffre(caractere);
					break;
			}
		}

		Notifier();
		return true;
	}

	/// <summary>
	/// Ramène un texte arbitraire au format interne de la machine à états.
	/// </summary>
	private static string NettoyerTexteAColler(string? texte)
	{
		if (string.IsNullOrWhiteSpace(texte))
		{
			return string.Empty;
		}

		System.Text.StringBuilder propre = new(LongueurMaxColler);

		foreach (char caractere in texte)
		{
			if (propre.Length >= LongueurMaxColler)
			{
				break;
			}

			// Les espaces, y compris insécables, servent de séparateurs de
			// milliers dans les nombres français : « 1 234,5 » doit donner
			// « 1234.5 », pas « 1234 » puis une virgule.
			if (char.IsWhiteSpace(caractere) || caractere is ' ' or ' ')
			{
				continue;
			}

			switch (caractere)
			{
				// Opérateurs typographiques.
				case '×' or '·' or '∗':
					propre.Append('*');
					break;
				case '÷' or '⁄':
					propre.Append('/');
					break;
				case '−' or '–' or '—' or '‒':
					propre.Append('-');
					break;
				// La virgule est le séparateur décimal attendu en français.
				case ',' or '٫':
					propre.Append('.');
					break;
				// Les parenthèses ne sont pas gérées par l'évaluateur : les
				// retirer évite de produire une expression rejetée au « = ».
				case '(' or ')' or '[' or ']':
					break;
				default:
					if (char.IsAsciiDigit(caractere) || caractere is '+' or '-' or '*' or '/' or '.')
					{
						propre.Append(caractere);
					}
					break;
			}
		}

		return propre.ToString();
	}

	// ------------------------------------------------------------------
	// Historique
	// ------------------------------------------------------------------

	/// <summary>Vide l'historique, en mémoire et dans le stockage.</summary>
	public void EffacerHistorique()
	{
		Historique.Clear();

		try
		{
			_stockage?.Clear();
		}
		catch (Exception)
		{
			// Un stockage défaillant ne doit jamais empêcher de vider l'écran.
		}

		Notifier();
	}

	/// <summary>Replace l'expression sur le résultat d'une entrée d'historique.</summary>
	public void RestaurerDepuisHistorique(CalculationHistoryEntry entree)
	{
		ArgumentNullException.ThrowIfNull(entree);

		_expression = VersInvariant(entree.Resultat);
		_dernierResultat = entree.Resultat;
		_resultatFrais = true;
		_erreur = CalculatorError.Aucune;
		Notifier();
	}

	// ------------------------------------------------------------------
	// Interne
	// ------------------------------------------------------------------

	private void AjouterALHistorique(CalculationHistoryEntry entree)
	{
		Historique.Insert(0, entree);

		while (Historique.Count > HistoriqueMax)
		{
			Historique.RemoveAt(Historique.Count - 1);
		}

		try
		{
			_stockage?.Save(Historique);
		}
		catch (Exception)
		{
			// L'historique reste utilisable même si l'enregistrement échoue.
		}
	}

	private void ChargerHistorique()
	{
		if (_stockage is null)
		{
			return;
		}

		try
		{
			foreach (CalculationHistoryEntry entree in _stockage.Load())
			{
				Historique.Add(entree);
			}
		}
		catch (Exception)
		{
			// Un historique illisible est ignoré plutôt que bloquant.
		}
	}

	private bool TryValeurCourante(out double valeur)
	{
		if (_resultatFrais && _dernierResultat.HasValue)
		{
			valeur = _dernierResultat.Value;
			return true;
		}

		string? terme = NumberFormatter.NombreEnCoursBrut(_expression);

		if (terme is not null && NumberFormatter.TryParseNombre(terme, out valeur))
		{
			return true;
		}

		valeur = 0d;
		return false;
	}

	private void EffacerErreur() => _erreur = CalculatorError.Aucune;

	private void Notifier() => StateChanged?.Invoke(this, EventArgs.Empty);

	private static char Symbole(CalculatorOperator operateur) => operateur switch
	{
		CalculatorOperator.Addition => '+',
		CalculatorOperator.Soustraction => '-',
		CalculatorOperator.Multiplication => '*',
		CalculatorOperator.Division => '/',
		_ => '+'
	};

	/// <summary>Convertit une valeur en texte invariant, avec point décimal.</summary>
	private static string VersInvariant(double valeur) =>
		valeur.ToString("R", CultureInfo.InvariantCulture);
}
