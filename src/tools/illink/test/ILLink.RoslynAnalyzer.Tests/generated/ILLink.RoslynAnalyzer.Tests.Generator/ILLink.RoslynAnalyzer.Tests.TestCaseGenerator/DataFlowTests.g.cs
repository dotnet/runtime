using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class DataFlowTests : LinkerTestBase
	{

		[Fact]
		public Task ExponentialDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericParameterDataFlowMarking ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodByRefParameterDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodOutParameterDataFlow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticInterfaceMethodDataflow ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeInfoIntrinsics ()
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
