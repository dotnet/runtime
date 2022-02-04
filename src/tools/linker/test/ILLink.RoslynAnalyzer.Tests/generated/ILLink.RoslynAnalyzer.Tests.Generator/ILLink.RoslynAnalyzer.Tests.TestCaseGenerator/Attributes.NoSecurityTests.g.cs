using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Attributes
{
	public sealed partial class NoSecurityTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes.NoSecurity";

		[Fact]
		public Task CoreLibrarySecurityAttributeTypesAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}