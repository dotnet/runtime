using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ReproTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Repro";

		[Fact]
		public Task Program ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}