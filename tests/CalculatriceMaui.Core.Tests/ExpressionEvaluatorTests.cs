namespace CalculatriceMaui.Core.Tests;

/// <summary>Évaluation d'expressions et priorités opératoires.</summary>
public class ExpressionEvaluatorTests
{
	[Theory]
	[InlineData("1+1", 2d)]
	[InlineData("2+3*4", 14d)]
	[InlineData("2*3+4", 10d)]
	[InlineData("100/4/5", 5d)]
	[InlineData("10-2-3", 5d)]
	[InlineData("2+3-4", 1d)]
	[InlineData("1.5*2", 3d)]
	[InlineData("0.1+0.2", 0.3d)]
	[InlineData("-5+3", -2d)]
	[InlineData("-5*2", -10d)]
	[InlineData("5*-3", -15d)]
	[InlineData("5*+3", 15d)]
	[InlineData("+7", 7d)]
	[InlineData("3*-2+1", -5d)]
	[InlineData("1+2*3-4/2", 5d)]
	[InlineData("  7 + 3  ", 10d)]
	public void TryEvaluate_RespecteLesPriorites(string expression, double attendu)
	{
		bool obtenu = ExpressionEvaluator.TryEvaluate(expression, out double resultat, out CalculatorError erreur);

		Assert.True(obtenu);
		Assert.Equal(attendu, resultat, 10);
		Assert.Equal(CalculatorError.Aucune, erreur);
	}

	[Theory]
	[InlineData("1/0")]
	[InlineData("5/0+1")]
	[InlineData("0/0")]
	public void TryEvaluate_DetecteLaDivisionParZero(string expression)
	{
		bool obtenu = ExpressionEvaluator.TryEvaluate(expression, out _, out CalculatorError erreur);

		Assert.False(obtenu);
		Assert.Equal(CalculatorError.DivisionParZero, erreur);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("+")]
	[InlineData("-")]
	[InlineData("1+")]
	[InlineData("*3")]
	[InlineData("/3")]
	[InlineData("1++")]
	[InlineData("1..2")]
	[InlineData("(1+2)")]
	[InlineData("2^3")]
	[InlineData("abc")]
	public void TryEvaluate_RefuseLesExpressionsInvalides(string expression)
	{
		bool obtenu = ExpressionEvaluator.TryEvaluate(expression, out _, out CalculatorError erreur);

		Assert.False(obtenu);
		Assert.NotEqual(CalculatorError.Aucune, erreur);
	}

	[Fact]
	public void TryEvaluate_RefuseUneEntreeNulle()
	{
		Assert.False(ExpressionEvaluator.TryEvaluate(null, out _, out _));
	}

	[Fact]
	public void TryEvaluate_SignaleLeDebordement()
	{
		// 1 suivi de 308 zéros, multiplié par 10 : au-delà de double.MaxValue.
		string grand = "1" + new string('0', 308);

		bool obtenu = ExpressionEvaluator.TryEvaluate(grand + "*10", out _, out CalculatorError erreur);

		Assert.False(obtenu);
		Assert.Equal(CalculatorError.Debordement, erreur);
	}

	[Fact]
	public void TryEvaluate_AccepteLesGrandesValeurs()
	{
		Assert.True(ExpressionEvaluator.TryEvaluate("99999999*99999999", out double resultat, out _));
		Assert.Equal(9999999800000001d, resultat, 0);
	}

	[Theory]
	[InlineData("2+2", 4d)]
	[InlineData("10/4", 2.5d)]
	[InlineData("0-3", -3d)]
	public void TryEvaluate_NEstPasSensibleALaCulture(string expression, double attendu)
	{
		Assert.True(ExpressionEvaluator.TryEvaluate(expression, out double resultat, out _));
		Assert.Equal(attendu, resultat, 10);
	}
}
