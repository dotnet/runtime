using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class CommandLineTests : LinkerTestBase
	{

		protected override string TestSuiteName => "CommandLine";

		[Fact]
		public Task AddCustomStep ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomStepData ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InvalidArguments ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ResponseFilesWork ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}