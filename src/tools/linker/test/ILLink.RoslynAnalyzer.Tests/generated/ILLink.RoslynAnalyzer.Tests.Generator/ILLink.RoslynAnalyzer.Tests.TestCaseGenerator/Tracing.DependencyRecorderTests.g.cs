using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Tracing
{
	public sealed partial class DependencyRecorderTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Tracing.DependencyRecorder";

		[Fact]
		public Task BasicDependencies ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}