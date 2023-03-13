using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Warnings
{
	public sealed partial class DependenciesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Warnings.Dependencies";

		[Fact]
		public Task TriggerWarnings_Lib ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TriggerWarnings_TrimmableLib ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}