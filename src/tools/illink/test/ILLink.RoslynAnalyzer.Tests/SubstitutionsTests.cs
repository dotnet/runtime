using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class SubstitutionsTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Substitutions";

		[Fact]
		public Task FeatureGuardSubstitutions ()
		{
			return RunTest ();
		}
	}
}
