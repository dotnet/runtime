using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Statics
{
	public sealed partial class DisableBeforeFieldInitTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Statics.DisableBeforeFieldInit";

		[Fact]
		public Task UnusedStaticFieldInitializer ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}