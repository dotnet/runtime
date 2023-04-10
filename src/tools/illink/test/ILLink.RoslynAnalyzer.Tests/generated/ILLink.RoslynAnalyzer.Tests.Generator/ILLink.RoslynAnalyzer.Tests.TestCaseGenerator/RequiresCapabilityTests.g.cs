using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class RequiresCapabilityTests : LinkerTestBase
	{

		[Fact]
		public Task RequiresInLibraryAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}