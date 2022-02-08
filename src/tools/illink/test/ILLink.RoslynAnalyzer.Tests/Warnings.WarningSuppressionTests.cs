using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Warnings
{
	public sealed partial class WarningSuppressionTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Warnings.WarningSuppression";

		[Fact]
		public Task SuppressWarningsInCompilerGeneratedCode ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SuppressWarningsInMembersAndTypesUsingTarget ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2579")]
		public Task SuppressWarningsViaXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}
	}
}