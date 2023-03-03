using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class UnreachableBlockTests : LinkerTestBase
	{

		[Fact]
		public Task BodiesWithSubstitutions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ComplexConditions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ComplexConditionsOptimized ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DataFlowRelated ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DeadVariables ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EndScopeOnMethoEnd ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InstanceMethodSubstitutions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodArgumentPropagation ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodWithParametersSubstitutions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MultiStageRemoval ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReplacedJumpTarget ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReplacedReturns ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ResultInliningNotPossible ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleConditionalProperty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SizeOfInConditions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TryCatchBlocks ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TryFinallyBlocks ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UninitializedLocals ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task WorksWithDynamicAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}