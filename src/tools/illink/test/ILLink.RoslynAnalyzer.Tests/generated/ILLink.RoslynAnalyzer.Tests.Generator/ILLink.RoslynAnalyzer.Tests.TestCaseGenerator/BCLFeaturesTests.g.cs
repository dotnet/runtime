using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class BCLFeaturesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "BCLFeatures";

		[Fact]
		public Task SerializationCtors ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}