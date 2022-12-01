using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Warnings
{
	public sealed partial class IndividualTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Warnings.Individual";

		[Fact]
		public Task CanGenerateWarningSuppressionFileCSharp ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanGenerateWarningSuppressionFileXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task WarningsAreSorted ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}