using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class SealerTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Sealer";

		[Fact]
		public Task MethodsDevirtualization ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypesCanBeSealed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}