using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class CodeOptimizationsSettingsTests
    {
		[Test]
		public void GlobalSettingsOnly ()
		{
			CodeOptimizationsSettings cos = new CodeOptimizationsSettings (CodeOptimizations.BeforeFieldInit);
			Assert.AreEqual (CodeOptimizations.BeforeFieldInit, cos.Global);
			Assert.That (cos.IsEnabled (CodeOptimizations.BeforeFieldInit, "any"));
			Assert.False (cos.IsEnabled (CodeOptimizations.ClearInitLocals, "any"));
		}

		[Test]
		public void OneAssemblyIsExcluded ()
		{
			CodeOptimizationsSettings cos = new CodeOptimizationsSettings (CodeOptimizations.BeforeFieldInit);
			cos.Disable (CodeOptimizations.BeforeFieldInit, "testasm.dll");

			Assert.AreEqual (CodeOptimizations.BeforeFieldInit, cos.Global);
			Assert.That (cos.IsEnabled (CodeOptimizations.BeforeFieldInit, "any"));
			Assert.False (cos.IsEnabled (CodeOptimizations.ClearInitLocals, "asny"));
			Assert.False (cos.IsEnabled (CodeOptimizations.BeforeFieldInit, "testasm.dll"));
		}

		[Test]
		public void ExcludedThenIncluded ()
		{
			CodeOptimizationsSettings cos = new CodeOptimizationsSettings (CodeOptimizations.BeforeFieldInit);
			cos.Disable (CodeOptimizations.BeforeFieldInit, "testasm.dll");
			cos.Enable (CodeOptimizations.OverrideRemoval | CodeOptimizations.BeforeFieldInit, "testasm.dll");

			Assert.AreEqual (CodeOptimizations.BeforeFieldInit, cos.Global);
			Assert.That (cos.IsEnabled (CodeOptimizations.BeforeFieldInit, "any"));
			Assert.False (cos.IsEnabled (CodeOptimizations.OverrideRemoval, "any"));

			Assert.False (cos.IsEnabled (CodeOptimizations.ClearInitLocals, "any"));
			Assert.That (cos.IsEnabled (CodeOptimizations.BeforeFieldInit, "testasm.dll"));
		}
	}
}