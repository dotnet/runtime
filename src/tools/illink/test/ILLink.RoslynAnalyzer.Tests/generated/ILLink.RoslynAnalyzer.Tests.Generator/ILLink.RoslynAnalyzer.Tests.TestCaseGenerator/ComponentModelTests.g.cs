using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ComponentModelTests : LinkerTestBase
	{

		protected override string TestSuiteName => "ComponentModel";

		[Fact]
		public Task CustomTypeConvertor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeConverterOnMembers ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeDescriptionProviderAttributeOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}