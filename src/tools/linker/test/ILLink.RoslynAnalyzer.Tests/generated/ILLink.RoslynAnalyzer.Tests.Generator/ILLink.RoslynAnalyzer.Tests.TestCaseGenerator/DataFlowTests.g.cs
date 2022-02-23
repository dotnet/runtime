using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class DataFlowTests : LinkerTestBase
	{

		[Fact]
		public Task MethodOutParameterDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}