using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class TopLevelStatementsTests : LinkerTestBase
	{

		[Fact]
		public Task BasicKeptValidation ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
