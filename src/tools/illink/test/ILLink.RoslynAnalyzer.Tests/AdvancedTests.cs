using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class AdvancedTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Advanced";

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2415")]
		public Task TypeCheckRemoval ()
		{
			return RunTest (allowMissingWarnings: true);
		}
	}
}