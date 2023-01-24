using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class MetadataTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Metadata";

		[Fact]
		public Task NamesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NamesAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}