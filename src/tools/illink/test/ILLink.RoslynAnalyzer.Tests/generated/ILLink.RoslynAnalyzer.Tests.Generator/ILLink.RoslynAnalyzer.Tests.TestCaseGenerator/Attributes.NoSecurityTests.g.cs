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

		[Fact]
		public Task SecurityAttributesOnUsedMethodAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SecurityAttributesOnUsedTypeAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}