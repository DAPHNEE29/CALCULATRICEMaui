namespace CalculatriceMaui.Core.Tests;

/// <summary>Saisie, opérations, fonctions, mémoire et historique du moteur.</summary>
public class CalculatorEngineTests
{
	private static CalculatorEngine Moteur() => new();

	private static void Saisir(CalculatorEngine moteur, string chiffres)
	{
		foreach (char c in chiffres)
		{
			switch (c)
			{
				case '.':
					moteur.SaisirVirgule();
					break;

				case '+':
					moteur.SaisirOperateur(CalculatorOperator.Addition);
					break;

				case '-':
					moteur.SaisirOperateur(CalculatorOperator.Soustraction);
					break;

				case '*':
					moteur.SaisirOperateur(CalculatorOperator.Multiplication);
					break;

				case '/':
					moteur.SaisirOperateur(CalculatorOperator.Division);
					break;

				default:
					moteur.SaisirChiffre(c);
					break;
			}
		}
	}

	// ------------------------------------------------------------------
	// Saisie
	// ------------------------------------------------------------------

	[Fact]
	public void Depart_AfficheZero()
	{
		Assert.Equal("0", Moteur().DisplayText);
		Assert.Equal(string.Empty, Moteur().ExpressionText);
	}

	[Fact]
	public void SaisirChiffre_ConstruitLeNombre()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "1234");

		Assert.Equal("1234", moteur.DisplayText);
		Assert.Equal("1234", moteur.ExpressionText);
	}

	[Fact]
	public void SaisirChiffre_RemplaceUnZeroIsole()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirChiffre('0');
		moteur.SaisirChiffre('5');

		Assert.Equal("5", moteur.DisplayText);
	}

	[Fact]
	public void SaisirChiffres_IgnoreLesCaracteresNonNumeriques()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirChiffre('a');
		moteur.SaisirChiffre('7');

		Assert.Equal("7", moteur.DisplayText);
	}

	[Fact]
	public void SaisirChiffre_LimiteLaLongueurDuTerme()
	{
		CalculatorEngine moteur = Moteur();

		for (int i = 0; i < 30; i++)
		{
			moteur.SaisirChiffre('9');
		}

		Assert.Equal(CalculatorEngine.LongueurMaxTerme, moteur.Expression.TrimStart('-').Length);
	}

	[Fact]
	public void SaisirVirgule_DemandeUnZeroPrealable()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirVirgule();

		Assert.Equal("0", moteur.DisplayText);
		Assert.Equal("0,", moteur.ExpressionText);
	}

	[Fact]
	public void SaisirVirgule_IgnoreUneSecondeVirgule()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirVirgule();
		moteur.SaisirVirgule();
		moteur.SaisirChiffre('5');

		Assert.Equal("0,5", moteur.DisplayText);
	}

	[Fact]
	public void SaisirVirgule_FonctionneApresUnOperateur()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "1+");
		moteur.SaisirVirgule();
		moteur.SaisirChiffre('5');

		Assert.Equal("1 + 0,5", moteur.ExpressionText);
	}

	// ------------------------------------------------------------------
	// Opérations
	// ------------------------------------------------------------------

	[Theory]
	[InlineData("2+3", "5")]
	[InlineData("7-9", "-2")]
	[InlineData("6*7", "42")]
	[InlineData("9/2", "4,5")]
	[InlineData("2+3*4", "14")]
	[InlineData("1.5+1.5", "3")]
	public void Calculer_ProduitLeResultatAttendu(string saisie, string attendu)
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, saisie);
		moteur.Calculer();

		Assert.Equal(attendu, moteur.DisplayText);
		Assert.False(moteur.HasError);
	}

	[Fact]
	public void Calculer_AccepteLesOperateursAffiches()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirChiffre('8');
		moteur.SaisirOperateur(CalculatorOperator.Soustraction);
		moteur.SaisirChiffre('3');
		moteur.Calculer();

		Assert.Equal("5", moteur.DisplayText);
		Assert.Equal("8 - 3", moteur.Historique[0].Expression);
	}

	[Fact]
	public void SaisirOperateur_RemplaceLOperateurEnAttente()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "8+");
		moteur.SaisirOperateur(CalculatorOperator.Multiplication);
		moteur.SaisirChiffre('3');
		moteur.Calculer();

		Assert.Equal("24", moteur.DisplayText);
	}

	[Fact]
	public void SaisirOperateur_IgnoreLesOperateursEnDebut()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirOperateur(CalculatorOperator.Addition);

		Assert.Equal(string.Empty, moteur.ExpressionText);
	}

	[Fact]
	public void Calculer_RefuseUneExpressionIncomplete()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5+");
		moteur.Calculer();

		Assert.True(moteur.HasError);
		Assert.Equal(CalculatorError.ExpressionInvalide, moteur.Erreur);
		Assert.Equal("Expression incomplète", moteur.StatusText);
	}

	[Fact]
	public void Calculer_DetecteLaDivisionParZero()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5/0");
		moteur.Calculer();

		Assert.True(moteur.HasError);
		Assert.Equal(CalculatorError.DivisionParZero, moteur.Erreur);
		Assert.Equal("Division par zéro impossible", moteur.StatusText);
	}

	[Fact]
	public void UneErreurEstEffaceeALaSaisieSuivante()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5/0");
		moteur.Calculer();
		Assert.True(moteur.HasError);

		moteur.ToutEffacer();
		Saisir(moteur, "7");

		Assert.False(moteur.HasError);
		Assert.Equal(string.Empty, moteur.StatusText);
	}

	[Fact]
	public void Calculer_EnchainementPoursuitSurLeResultat()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "2+3");
		moteur.Calculer();
		moteur.SaisirOperateur(CalculatorOperator.Multiplication);
		Saisir(moteur, "4");
		moteur.Calculer();

		Assert.Equal("20", moteur.DisplayText);
	}

	[Fact]
	public void SaisirChiffre_ApresUnResultatRedemarre()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "2+3");
		moteur.Calculer();
		moteur.SaisirChiffre('9');

		Assert.Equal("9", moteur.DisplayText);
	}

	// ------------------------------------------------------------------
	// Effacements
	// ------------------------------------------------------------------

	[Fact]
	public void EffacerLeDernierCaractere_RetireUnChiffre()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "123");
		moteur.EffacerLeDernierCaractere();

		Assert.Equal("12", moteur.DisplayText);
	}

	[Fact]
	public void EffacerLeDernierCaractere_RetireUnOperateurIsole()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "12+");
		moteur.EffacerLeDernierCaractere();

		Assert.Equal("12", moteur.DisplayText);
	}

	[Fact]
	public void EffacerLeDernierCaractere_NeVidePasTotalement()
	{
		CalculatorEngine moteur = Moteur();
		moteur.EffacerLeDernierCaractere();

		Assert.Equal("0", moteur.DisplayText);
	}

	[Fact]
	public void EffacerLEntree_ConserveLOperateur()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "12+34");
		moteur.EffacerLEntree();
		moteur.SaisirChiffre('5');
		moteur.Calculer();

		Assert.Equal("17", moteur.DisplayText);
	}

	[Fact]
	public void ToutEffacer_ReinitialiseLEtat()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5/0");
		moteur.Calculer();
		moteur.MemoireAjouter();
		moteur.ToutEffacer();

		Assert.Equal("0", moteur.DisplayText);
		Assert.Equal(string.Empty, moteur.ExpressionText);
		Assert.False(moteur.HasError);
		Assert.False(moteur.IsMemoryUsed);
	}

	// ------------------------------------------------------------------
	// Pourcentage et signe
	// ------------------------------------------------------------------

	[Fact]
	public void Pourcentage_DiviseParCent()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "50");
		moteur.AppliquerPourcentage();

		Assert.Equal("0,5", moteur.DisplayText);
	}

	[Fact]
	public void Pourcentage_SuitUneAdditionEnCours()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "200+10");
		moteur.AppliquerPourcentage();
		moteur.Calculer();

		Assert.Equal("200,1", moteur.DisplayText);
	}

	[Fact]
	public void Pourcentage_SuitUneMultiplicationEnCours()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "200*10");
		moteur.AppliquerPourcentage();
		moteur.Calculer();

		Assert.Equal("20", moteur.DisplayText);
	}

	[Fact]
	public void ChangerLeSigne_InspereLeTermeEnCours()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5");
		moteur.ChangerLeSigne();

		Assert.Equal("-5", moteur.DisplayText);
	}

	[Fact]
	public void ChangerLeSigne_AgitApresUnOperateur()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "8-3");
		moteur.ChangerLeSigne();
		moteur.Calculer();

		Assert.Equal("11", moteur.DisplayText);
	}

	[Fact]
	public void ChangerLeSigne_AgitSurLeResultat()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5+5");
		moteur.Calculer();
		moteur.ChangerLeSigne();

		Assert.Equal("-10", moteur.DisplayText);
	}

	[Fact]
	public void ChangerLeSigne_PeutEtreAnnule()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirChiffre('7');
		moteur.ChangerLeSigne();
		moteur.ChangerLeSigne();

		Assert.Equal("7", moteur.DisplayText);
	}

	[Fact]
	public void ChangerLeSigne_AgitSurUnTermeNegatifApresOperateur()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5*");
		moteur.SaisirChiffre('3');
		moteur.ChangerLeSigne();
		moteur.Calculer();

		Assert.Equal("-15", moteur.DisplayText);
	}

	// ------------------------------------------------------------------
	// Fonctions
	// ------------------------------------------------------------------

	[Fact]
	public void Carre_EleveAuCarre()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "9");
		moteur.AppliquerFonction(CalculatorFunction.Carre);

		Assert.Equal("81", moteur.DisplayText);
	}

	[Fact]
	public void RacineCarree_ExtraitLaRacine()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "81");
		moteur.AppliquerFonction(CalculatorFunction.RacineCarree);

		Assert.Equal("9", moteur.DisplayText);
	}

	[Fact]
	public void RacineCarree_RefuseUnNegatif()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirChiffre('9');
		moteur.ChangerLeSigne();
		moteur.AppliquerFonction(CalculatorFunction.RacineCarree);

		Assert.True(moteur.HasError);
		Assert.Equal(CalculatorError.RacineNegative, moteur.Erreur);
	}

	[Fact]
	public void Inverse_CalculeLeReciproque()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "4");
		moteur.AppliquerFonction(CalculatorFunction.Inverse);

		Assert.Equal("0,25", moteur.DisplayText);
	}

	[Fact]
	public void Inverse_DeZeroSignaleLaDivisionParZero()
	{
		CalculatorEngine moteur = Moteur();
		moteur.SaisirChiffre('0');
		moteur.AppliquerFonction(CalculatorFunction.Inverse);

		Assert.True(moteur.HasError);
		Assert.Equal(CalculatorError.DivisionParZero, moteur.Erreur);
	}

	[Fact]
	public void Inverse_SansNombreSignaleUneFonctionIndisponible()
	{
		CalculatorEngine moteur = Moteur();
		moteur.AppliquerFonction(CalculatorFunction.Inverse);

		Assert.True(moteur.HasError);
		Assert.Equal(CalculatorError.FonctionIndisponible, moteur.Erreur);
	}

	[Fact]
	public void ConstantePi_EstDisponibleSansSaisie()
	{
		CalculatorEngine moteur = Moteur();
		moteur.AppliquerFonction(CalculatorFunction.Pi);
		moteur.Calculer();

		Assert.Equal("3,14159265359", moteur.DisplayText);
	}

	[Fact]
	public void ConstanteE_EstDisponibleSansSaisie()
	{
		CalculatorEngine moteur = Moteur();
		moteur.AppliquerFonction(CalculatorFunction.E);
		moteur.Calculer();

		Assert.Equal("2,71828182846", moteur.DisplayText);
	}

	[Fact]
	public void Fonction_RequiertUnNombreSaufConstante()
	{
		CalculatorEngine moteur = Moteur();
		moteur.AppliquerFonction(CalculatorFunction.Carre);

		Assert.True(moteur.HasError);
		Assert.Equal(CalculatorError.FonctionIndisponible, moteur.Erreur);
	}

	[Fact]
	public void Fonction_AgitEnMilieuDExpression()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "2+9");
		moteur.AppliquerFonction(CalculatorFunction.Carre);
		moteur.Calculer();

		Assert.Equal("83", moteur.DisplayText);
	}

	// ------------------------------------------------------------------
	// Mémoire
	// ------------------------------------------------------------------

	[Fact]
	public void MemoireAjouteEtRappelle()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "42");
		moteur.MemoireAjouter();

		Assert.True(moteur.IsMemoryUsed);
		Assert.Equal("42", moteur.MemoryText);

		moteur.ToutEffacer();
		moteur.MemoireRappeler();
		moteur.Calculer();

		Assert.Equal("42", moteur.DisplayText);
	}

	[Fact]
	public void MemoireCumulLesAjouts()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "10");
		moteur.MemoireAjouter();
		moteur.ToutEffacer();
		Saisir(moteur, "5");
		moteur.MemoireAjouter();

		Assert.Equal("15", moteur.MemoryText);
	}

	[Fact]
	public void MemoireEffaceVideLaValeur()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "42");
		moteur.MemoireAjouter();
		moteur.MemoireEffacer();

		Assert.False(moteur.IsMemoryUsed);
		Assert.Equal("0", moteur.MemoryText);
	}

	[Fact]
	public void MemoireRappelerSansValeurSignaleUneErreur()
	{
		CalculatorEngine moteur = Moteur();
		moteur.MemoireRappeler();

		Assert.True(moteur.HasError);
	}

	[Fact]
	public void MemoireAjoutSansNombreSignaleUneErreur()
	{
		CalculatorEngine moteur = Moteur();
		moteur.MemoireAjouter();

		Assert.True(moteur.HasError);
	}

	[Fact]
	public void MemoireRappeler_PourSMultiplierEnCours()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "3");
		moteur.MemoireAjouter();
		moteur.ToutEffacer();
		Saisir(moteur, "2+");
		moteur.MemoireRappeler();
		moteur.Calculer();

		Assert.Equal("5", moteur.DisplayText);
	}

	// ------------------------------------------------------------------
	// Historique
	// ------------------------------------------------------------------

	[Fact]
	public void Historique_ContientLesCalculs()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "2+2");
		moteur.Calculer();

		Assert.Single(moteur.Historique);
		Assert.Equal("2 + 2", moteur.Historique[0].Expression);
		Assert.Equal("4", moteur.Historique[0].ResultatTexte);
	}

	[Fact]
	public void Historique_PlaceLePlusRecentEnTete()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "1+1");
		moteur.Calculer();
		Saisir(moteur, "2+2");
		moteur.Calculer();

		Assert.Equal(2, moteur.Historique.Count);
		Assert.Equal("2 + 2", moteur.Historique[0].Expression);
	}

	[Fact]
	public void Historique_NEstPasAjouteEnCasDErreur()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "5/0");
		moteur.Calculer();

		Assert.Empty(moteur.Historique);
	}

	[Fact]
	public void EffacerHistorique_LeVide()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "1+1");
		moteur.Calculer();
		moteur.EffacerHistorique();

		Assert.Empty(moteur.Historique);
	}

	[Fact]
	public void RestaurerDepuisHistorique_RechargeLeResultat()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "6*7");
		moteur.Calculer();
		CalculationHistoryEntry entree = moteur.Historique[0];

		moteur.ToutEffacer();
		moteur.RestaurerDepuisHistorique(entree);

		Assert.Equal("42", moteur.DisplayText);
	}

	[Fact]
	public void Historique_EstBorneA50Entrees()
	{
		CalculatorEngine moteur = Moteur();

		for (int i = 0; i < CalculatorEngine.HistoriqueMax + 10; i++)
		{
			moteur.ToutEffacer();
			moteur.SaisirChiffre('1');
			moteur.SaisirOperateur(CalculatorOperator.Addition);
			moteur.SaisirChiffre('1');
			moteur.Calculer();
		}

		Assert.Equal(CalculatorEngine.HistoriqueMax, moteur.Historique.Count);
	}

	// ------------------------------------------------------------------
	// Persistance et notifications
	// ------------------------------------------------------------------

	[Fact]
	public void LHistoriqueEstReluAuDemarrage()
	{
		InMemoryHistoryStorage stockage = new();
		CalculatorEngine premier = new(stockage);
		premier.SaisirChiffre('8');
		premier.SaisirOperateur(CalculatorOperator.Addition);
		premier.SaisirChiffre('8');
		premier.Calculer();

		CalculatorEngine second = new(stockage);

		Assert.Single(second.Historique);
		Assert.Equal("8 + 8", second.Historique[0].Expression);
	}

	[Fact]
	public void EffacerHistorique_EffaceLeStockage()
	{
		InMemoryHistoryStorage stockage = new();
		CalculatorEngine moteur = new(stockage);
		moteur.SaisirChiffre('1');
		moteur.SaisirOperateur(CalculatorOperator.Addition);
		moteur.SaisirChiffre('1');
		moteur.Calculer();
		moteur.EffacerHistorique();

		Assert.Empty(stockage.Load());
	}

	[Fact]
	public void UnStockageDefaillantNInterromptPasLeCalcul()
	{
		CalculatorEngine moteur = new(new StockageDefaillant());
		moteur.SaisirChiffre('2');
		moteur.SaisirOperateur(CalculatorOperator.Addition);
		moteur.SaisirChiffre('2');
		moteur.Calculer();

		Assert.Equal("4", moteur.DisplayText);
	}

	[Fact]
	public void StateChanged_EstDeclencheALaSaisie()
	{
		CalculatorEngine moteur = Moteur();
		int appels = 0;
		moteur.StateChanged += (_, _) => appels++;

		moteur.SaisirChiffre('1');
		moteur.SaisirChiffre('2');
		moteur.Calculer();

		Assert.Equal(3, appels);
	}

	[Fact]
	public void SansStockageLHistoriqueResteEnMemeoire()
	{
		CalculatorEngine moteur = new();
		moteur.SaisirChiffre('3');
		moteur.SaisirOperateur(CalculatorOperator.Addition);
		moteur.SaisirChiffre('4');
		moteur.Calculer();

		Assert.Single(moteur.Historique);
	}

	[Fact]
	public void ConstantesDuMoteurSontExposees()
	{
		Assert.Equal(15, CalculatorEngine.LongueurMaxTerme);
		Assert.Equal(50, CalculatorEngine.HistoriqueMax);
	}

	// ------------------------------------------------------------------
	// Presse-papiers
	// ------------------------------------------------------------------

	[Fact]
	public void CollerUnNombreRemplitLaSaisie()
	{
		CalculatorEngine moteur = Moteur();

		Assert.True(moteur.CollerTexte("42"));
		Assert.Equal("42", moteur.DisplayText);
	}

	[Fact]
	public void CollerUneExpressionPermetDeCalculer()
	{
		CalculatorEngine moteur = Moteur();

		Assert.True(moteur.CollerTexte("7*6"));
		moteur.Calculer();

		Assert.Equal("42", moteur.DisplayText);
		Assert.Single(moteur.Historique);
	}

	[Fact]
	public void CollerUtiliseLesSymbolesTypographiques()
	{
		CalculatorEngine moteur = Moteur();

		// « × », « ÷ » et « − » sont les symboles affichés par la calculatrice.
		Assert.True(moteur.CollerTexte("12×5−10÷2"));
		moteur.Calculer();

		Assert.Equal("55", moteur.DisplayText);
	}

	[Fact]
	public void CollerAccepteLaVirguleDecimaleFrancaise()
	{
		CalculatorEngine moteur = Moteur();

		Assert.True(moteur.CollerTexte("1,5+2,5"));
		moteur.Calculer();

		Assert.Equal("4", moteur.DisplayText);
	}

	[Fact]
	public void CollerIgnoreLesEspacesDeMilliers()
	{
		CalculatorEngine moteur = Moteur();

		// Espace normale, espace insécable et espace fine insécable.
		Assert.True(moteur.CollerTexte("1 234 567 890,25"));
		moteur.Calculer();

		Assert.Equal("1234567890,25", moteur.DisplayText);
	}

	[Fact]
	public void CollerSupprimeLesCaracteresIncompris()
	{
		CalculatorEngine moteur = Moteur();

		// Lettres, parenthèses et ponctuation sont écartées : il doit rester
		// une expression valide, sinon « = » échouerait.
		Assert.True(moteur.CollerTexte("Prix : (12+8) euros !"));
		moteur.Calculer();

		Assert.Equal("20", moteur.DisplayText);
	}

	[Fact]
	public void CollerRemplaceLaSaisieEnCours()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "123");

		Assert.True(moteur.CollerTexte("7"));
		Assert.Equal("7", moteur.DisplayText);
	}

	[Fact]
	public void CollerRemplaceUnResultatAffiche()
	{
		CalculatorEngine moteur = Moteur();
		Saisir(moteur, "3+4");
		moteur.Calculer();
		Assert.Equal("7", moteur.DisplayText);

		Assert.True(moteur.CollerTexte("99"));
		Assert.Equal("99", moteur.DisplayText);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("bonjour")]
	[InlineData("€")]
	public void CollerUnTexteInexploitableSignaleUneErreur(string? texte)
	{
		CalculatorEngine moteur = Moteur();

		Assert.False(moteur.CollerTexte(texte));
		Assert.True(moteur.HasError);
		Assert.Equal(CalculatorError.ExpressionInvalide, moteur.Erreur);
	}

	[Fact]
	public void CollerUnNombreTropLongEstTronqueCommeUneSaisie()
	{
		CalculatorEngine moteur = Moteur();

		// La longueur maximale d'un terme s'applique aussi au collage : seul
		// l'expression brute est vérifiée, l'affichage passant en notation
		// scientifique pour un nombre aussi grand.
		Assert.True(moteur.CollerTexte("1234567890123456789"));
		Assert.Equal("123456789012345", moteur.Expression);
	}

	[Fact]
	public void CollerNEnregistrePasDeCalculDansLHistorique()
	{
		CalculatorEngine moteur = Moteur();

		moteur.CollerTexte("3+3");

		// Coller remplit l'expression ; seul « = » l'enregistre.
		Assert.Empty(moteur.Historique);
	}

	private sealed class StockageDefaillant : IHistoryStorage
	{
		public IReadOnlyList<CalculationHistoryEntry> Load() => throw new InvalidOperationException("panne");

		public void Save(IEnumerable<CalculationHistoryEntry> entrees) => throw new InvalidOperationException("panne");

		public void Clear() => throw new InvalidOperationException("panne");
	}
}

