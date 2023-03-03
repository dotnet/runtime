using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.References
{
	public sealed partial class DependenciesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "References.Dependencies";

		[Fact]
		public Task ReferenceWithEntryPoint_Lib ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}