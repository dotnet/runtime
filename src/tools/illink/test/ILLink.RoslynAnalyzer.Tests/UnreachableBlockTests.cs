using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class UnreachableBlockTests : LinkerTestBase
	{
		protected override string TestSuiteName => "UnreachableBlock";

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2415")]
		public Task TryFilterBlocks ()
		{
			return RunTest (allowMissingWarnings: true);
		}
	}
}