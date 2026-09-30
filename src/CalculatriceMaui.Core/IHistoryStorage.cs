namespace CalculatriceMaui.Core;

/// <summary>Persistance de l'historique des calculs.</summary>
public interface IHistoryStorage
{
	/// <summary>Relit l'historique enregistré. Renvoie une liste vide si rien n'est stocké.</summary>
	IReadOnlyList<CalculationHistoryEntry> Load();

	/// <summary>Réécrit entièrement l'historique.</summary>
	void Save(IEnumerable<CalculationHistoryEntry> entrees);

	/// <summary>Supprime l'historique enregistré.</summary>
	void Clear();
}

/// <summary>Stockage en mémoire, utilisé par défaut et par les tests.</summary>
public sealed class InMemoryHistoryStorage : IHistoryStorage
{
	private readonly List<CalculationHistoryEntry> _entrees = [];

	/// <inheritdoc />
	public IReadOnlyList<CalculationHistoryEntry> Load() => _entrees.ToList();

	/// <inheritdoc />
	public void Save(IEnumerable<CalculationHistoryEntry> entrees)
	{
		ArgumentNullException.ThrowIfNull(entrees);

		_entrees.Clear();
		_entrees.AddRange(entrees);
	}

	/// <inheritdoc />
	public void Clear() => _entrees.Clear();
}
