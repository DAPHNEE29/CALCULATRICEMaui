namespace CalculatriceMaui.Core.Tests;

/// <summary>Mise en forme des nombres et lecture de l'expression en cours.</summary>
public class NumberFormatterTests
{
	[Theory]
	[InlineData(0d, "0")]
	[InlineData(1d, "1")]
	[InlineData(-1d, "-1")]
	[InlineData(42d, "42")]
	[InlineData(2.5d, "2,5")]
	[InlineData(-2.5d, "-2,5")]
	[InlineData(0.5d, "0,5")]
	[InlineData(1000d, "1000")]
	[InlineData(-1000d, "-1000")]
	[InlineData(3.14159265358979d, "3,14159265359")]
	[InlineData(0.1d + 0.2d, "0,3")]
	public void Format_UtiliseLaVirguleDecimale(double valeur, string attendu)
	{
		Assert.Equal(attendu, NumberFormatter.Format(valeur));
	}

	[Fact]
	public void Format_RetombeSurErreurPourLesValeursNonFinies()
	{
		Assert.Equal("Erreur", NumberFormatter.Format(double.NaN));
		Assert.Equal("Erreur", NumberFormatter.Format(double.PositiveInfinity));
		Assert.Equal("Erreur", NumberFormatter.Format(double.NegativeInfinity));
	}

	[Fact]
	public void Format_UtiliseLaNotationScientifiqueAuxExtremes()
	{
		string grand = NumberFormatter.Format(1e15);
		string petit = NumberFormatter.Format(1e-12);

		Assert.Contains("e", grand, StringComparison.Ordinal);
		Assert.Contains("e", petit, StringComparison.Ordinal);
		Assert.DoesNotContain(".", petit, StringComparison.Ordinal);
	}

	[Fact]
	public void Format_NetoyieLesZerosInutiles()
	{
		Assert.Equal("3", NumberFormatter.Format(3.0000000000001d));
		Assert.Equal("2,25", NumberFormatter.Format(2.250000d));
	}

	[Theory]
	[InlineData("12", true, 12d)]
	[InlineData("12.5", true, 12.5d)]
	[InlineData("-3.75", true, -3.75d)]
	[InlineData(".5", true, 0.5d)]
	[InlineData("", false, 0d)]
	[InlineData(null, false, 0d)]
	[InlineData("abc", false, 0d)]
	public void TryParseNombre_UtiliseLePointDecimalInvariant(string? texte, bool attendu, double valeur)
	{
		bool obtenu = NumberFormatter.TryParseNombre(texte, out double resultat);

		Assert.Equal(attendu, obtenu);

		if (attendu)
		{
			Assert.Equal(valeur, resultat, 10);
		}
	}

	[Theory]
	[InlineData("12+3", "3")]
	[InlineData("12-3", "3")]
	[InlineData("12*3.5", "3.5")]
	[InlineData("12/3", "3")]
	[InlineData("12+3-", null)]
	[InlineData("", null)]
	[InlineData("12", "12")]
	public void NombreEnCoursBrut_ExtraitLeDernierTerme(string expression, string? attendu)
	{
		Assert.Equal(attendu, NumberFormatter.NombreEnCoursBrut(expression));
	}

	[Fact]
	public void NombreEnCoursBrut_GardeLeSigneUnaire()
	{
		Assert.Equal("-3", NumberFormatter.NombreEnCoursBrut("5*-3"));
		Assert.Equal("3", NumberFormatter.NombreEnCoursBrut("5-3"));
	}

	[Theory]
	[InlineData('+', true)]
	[InlineData('-', true)]
	[InlineData('*', true)]
	[InlineData('/', true)]
	[InlineData('0', false)]
	[InlineData('.', false)]
	public void EstOperateur_ReconnaitLesQuatreOperateurs(char caractere, bool attendu)
	{
		Assert.Equal(attendu, NumberFormatter.EstOperateur(caractere));
	}

	[Theory]
	[InlineData("-5", 0, true)]
	[InlineData("5-3", 1, false)]
	[InlineData("5*-", 2, true)]
	[InlineData("5+", 1, false)]
	[InlineData("5*", 1, false)]
	[InlineData("5/", 1, false)]
	public void EstOperateurUnaire_DetecteLesSignesSansOperateurPrecedent(string expression, int index, bool attendu)
	{
		Assert.Equal(attendu, NumberFormatter.EstOperateurUnaire(expression, index));
	}

	[Theory]
	[InlineData("", "")]
	[InlineData(null, "")]
	public void Prettify_GereLesEntreesVides(string? expression, string attendu)
	{
		Assert.Equal(attendu, NumberFormatter.Prettify(expression));
	}

	[Theory]
	[InlineData("12+3", "12 + 3")]
	[InlineData("12-3", "12 - 3")]
	[InlineData("12*3", "12 × 3")]
	[InlineData("12/3", "12 ÷ 3")]
	[InlineData("2+3*4", "2 + 3 × 4")]
	[InlineData("2.5+1.25", "2,5 + 1,25")]
	public void Prettify_EntoureLesOperateursBinaires(string expression, string attendu)
	{
		Assert.Equal(attendu, NumberFormatter.Prettify(expression));
	}

	[Fact]
	public void Prettify_EspaceLesSignesUnaires()
	{
		Assert.Equal("5 × -3", NumberFormatter.Prettify("5*-3"));
		Assert.Equal("-3 × 2", NumberFormatter.Prettify("-3*2"));
	}

	[Fact]
	public void Prettify_GardeLaVirguleEnCoursDeSaisie()
	{
		Assert.Equal("0,", NumberFormatter.Prettify("0."));
		Assert.Equal("1 + 0,5", NumberFormatter.Prettify("1+0.5"));
	}

	[Fact]
	public void Prettify_NAjoutePasDEspaceEnFin()
	{
		Assert.Equal("12 +", NumberFormatter.Prettify("12+"));
	}
}
