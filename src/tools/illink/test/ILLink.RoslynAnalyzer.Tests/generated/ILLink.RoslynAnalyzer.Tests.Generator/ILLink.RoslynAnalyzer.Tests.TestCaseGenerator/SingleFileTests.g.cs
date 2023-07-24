using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class SingleFileTests : LinkerTestBase
	{

		protected override string TestSuiteName => "SingleFile";

		[Fact]
		public Task SingleFileIntrinsics ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}