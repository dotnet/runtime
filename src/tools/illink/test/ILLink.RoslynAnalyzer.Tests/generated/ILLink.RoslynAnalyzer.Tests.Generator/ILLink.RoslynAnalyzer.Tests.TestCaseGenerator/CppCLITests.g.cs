using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class CppCLITests : LinkerTestBase
	{

		protected override string TestSuiteName => "CppCLI";

		[Fact]
		public Task CppCLIAssemblyIsAnalyzed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NonCopyActionWarnOnCppCLI ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}