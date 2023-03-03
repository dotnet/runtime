using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class FeatureSettingsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "FeatureSettings";

		[Fact]
		public Task FeatureDescriptors ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FeatureSubstitutions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FeatureSubstitutionsInvalid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FeatureSubstitutionsNested ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}