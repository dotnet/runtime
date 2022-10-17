using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Tracing
{
	public sealed partial class IndividualTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Tracing.Individual";

		[Fact]
		public Task CanDumpDependenciesToUncompressedDgml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanDumpDependenciesToUncompressedXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanEnableDependenciesDump ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanEnableReducedTracing ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}