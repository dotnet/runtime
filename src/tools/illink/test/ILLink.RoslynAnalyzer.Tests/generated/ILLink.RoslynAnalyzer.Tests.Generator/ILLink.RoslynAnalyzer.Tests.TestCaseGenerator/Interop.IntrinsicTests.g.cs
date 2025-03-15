using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop
{
	public sealed partial class IntrinsicTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Interop.Intrinsic";

		[Fact]
		public Task OutTypesAreMarkedInstantiated ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
