using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class UnreachableBlockTests : LinkerTestBase
	{
		protected override string TestSuiteName => "UnreachableBlock";

		[Fact]
		public Task TryFilterBlocks ()
		{
			return RunTest (allowMissingWarnings: true);
		}
	}
}