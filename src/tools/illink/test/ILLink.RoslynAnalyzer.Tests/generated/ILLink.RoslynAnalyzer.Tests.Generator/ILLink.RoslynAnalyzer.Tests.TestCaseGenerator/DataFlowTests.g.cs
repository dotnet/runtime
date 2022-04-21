using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class DataFlowTests : LinkerTestBase
	{

		[Fact]
		public Task MethodByRefParameterDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodByRefReturnDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodOutParameterDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnsafeDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}