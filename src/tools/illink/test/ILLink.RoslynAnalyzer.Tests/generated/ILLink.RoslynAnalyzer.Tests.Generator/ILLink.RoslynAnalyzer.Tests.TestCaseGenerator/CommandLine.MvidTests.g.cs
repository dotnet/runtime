using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.CommandLine
{
	public sealed partial class MvidTests : LinkerTestBase
	{

		protected override string TestSuiteName => "CommandLine.Mvid";

		[Fact]
		public Task DefaultMvidBehavior ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DeterministicMvidWorks ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NewMvidWorks ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RetainMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}