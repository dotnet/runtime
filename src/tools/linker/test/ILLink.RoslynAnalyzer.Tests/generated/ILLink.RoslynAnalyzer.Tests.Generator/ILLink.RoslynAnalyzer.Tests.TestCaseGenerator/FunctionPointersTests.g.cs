using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class FunctionPointersTests : LinkerTestBase
	{

		protected override string TestSuiteName => "FunctionPointers";

		[Fact]
		public Task CanCompileInterfaceWithFunctionPointerParameter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanCompileMethodWithFunctionPointerParameter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}