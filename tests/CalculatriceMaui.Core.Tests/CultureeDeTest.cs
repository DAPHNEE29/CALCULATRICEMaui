using System.Globalization;
using System.Runtime.CompilerServices;

namespace CalculatriceMaui.Core.Tests;

/// <summary>
/// Force la culture française pour toute la suite de tests : l'affichage de la
/// calculatrice est français, les tests doivent donc le refléter.
/// </summary>
internal static class CultureeDeTest
{
	[ModuleInitializer]
	internal static void Appliquer()
	{
		CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("fr-FR");
		CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("fr-FR");
		CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
		CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
	}
}
