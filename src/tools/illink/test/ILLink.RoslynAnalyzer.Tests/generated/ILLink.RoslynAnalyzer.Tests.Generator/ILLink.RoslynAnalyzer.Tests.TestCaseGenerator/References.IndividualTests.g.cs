using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.References
{
	public sealed partial class IndividualTests : LinkerTestBase
	{

		protected override string TestSuiteName => "References.Individual";

		[Fact]
		public Task CanSkipUnresolved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}