using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Attributes
{
	public sealed partial class StructLayoutTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes.StructLayout";

		[Fact]
		public Task AutoClass ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExplicitClass ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SequentialClass ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}