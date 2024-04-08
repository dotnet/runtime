using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class TopLevelStatementsTests : LinkerTestBase
	{
		protected override string TestSuiteName => "TopLevelStatements";

		[Fact]
		public Task BasicDataFlow ()
		{
			return RunTest ();
		}
	}
}
