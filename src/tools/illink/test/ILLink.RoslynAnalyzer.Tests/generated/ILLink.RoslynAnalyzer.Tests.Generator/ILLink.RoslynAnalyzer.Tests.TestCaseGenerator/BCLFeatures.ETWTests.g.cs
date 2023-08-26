using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.BCLFeatures
{
	public sealed partial class ETWTests : LinkerTestBase
	{

		protected override string TestSuiteName => "BCLFeatures.ETW";

		[Fact]
		public Task CustomEventSource ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomLibraryEventSource ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
