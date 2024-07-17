using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class WarningsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Warnings";

		[Fact (Skip = "Analyzers are disabled entirely by SuppressTrimAnalysisWarnings or SuppressAotAnalysisWarnings")]
		public Task CanDisableWarningsByCategory ()
		{
			return RunTest ();
		}
	}
}
