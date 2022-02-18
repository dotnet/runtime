using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class LoggingTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Logging";

		[Fact]
		public Task CommonLogs ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SourceLines ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}