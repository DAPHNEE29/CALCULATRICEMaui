using Microsoft.Extensions.Logging;
using CalculatriceMaui.Core;
using CalculatriceMaui.Services;
using CalculatriceMaui.ViewModels;

namespace CalculatriceMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder builder = MauiApp.CreateBuilder();

		builder.UseMauiApp<App>();

		// Le moteur de calcul porte l'expression en cours, la mémoire et
		// l'historique : il doit être unique. Le ViewModel l'enveloppe pour
		// l'interface et MainPage est résolu par injection.
		builder.Services.AddSingleton(_ =>
		{
			CalculatorEngine moteur = new(new PreferencesHistoryStorage());

			return new CalculatorViewModel(moteur);
		});

		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<Pages.HistoryPage>();

		builder.ConfigureFonts(fonts =>
		{
			fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
