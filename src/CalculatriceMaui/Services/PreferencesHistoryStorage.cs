using System.Text.Json;
using CalculatriceMaui.Core;

namespace CalculatriceMaui.Services;

/// <summary>
/// Enregistre l'historique dans les préférences de l'application. MAUI gère
/// le stockage lui-même pour chaque plateforme, ce qui évite d'écrire un
/// fichier par cible.
/// </summary>
public sealed class PreferencesHistoryStorage : IHistoryStorage
{
	private const string Cle = "historique_calculs";

	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = false
	};

	/// <inheritdoc />
	public IReadOnlyList<CalculationHistoryEntry> Load()
	{
		string? brut = Preferences.Default.Get<string?>(Cle, null);

		if (string.IsNullOrWhiteSpace(brut))
		{
			return [];
		}

		List<EntreeEnregistree>? entrees =
			JsonSerializer.Deserialize<List<EntreeEnregistree>>(brut, Options);

		if (entrees is null)
		{
			return [];
		}

		List<CalculationHistoryEntry> resultat = [];

		foreach (EntreeEnregistree entree in entrees)
		{
			if (entree is null || string.IsNullOrEmpty(entree.Expression))
			{
				continue;
			}

			resultat.Add(new CalculationHistoryEntry(entree.Expression, entree.Resultat));
		}

		return resultat;
	}

	/// <inheritdoc />
	public void Save(IEnumerable<CalculationHistoryEntry> entrees)
	{
		ArgumentNullException.ThrowIfNull(entrees);

		List<EntreeEnregistree> enregistrables = [];

		foreach (CalculationHistoryEntry entree in entrees)
		{
			enregistrables.Add(new EntreeEnregistree(entree.Expression, entree.Resultat));
		}

		Preferences.Default.Set(Cle, JsonSerializer.Serialize(enregistrables, Options));
	}

	/// <inheritdoc />
	public void Clear() => Preferences.Default.Remove(Cle);

	private sealed record EntreeEnregistree(string Expression, double Resultat);
}
