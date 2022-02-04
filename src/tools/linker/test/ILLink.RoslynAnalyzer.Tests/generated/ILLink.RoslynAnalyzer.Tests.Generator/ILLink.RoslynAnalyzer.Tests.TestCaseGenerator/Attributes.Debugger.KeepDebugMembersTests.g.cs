using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Attributes.Debugger
{
	public sealed partial class KeepDebugMembersTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes.Debugger.KeepDebugMembers";

		[Fact]
		public Task DebuggerDisplayAttributeOnAssemblyUsingTarget ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnAssemblyUsingTargetOnUnusedType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInSameAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameOfGenericTypeInOtherAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameOfNestedTypeInOtherAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnGenerics ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnTypeThatIsNotUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayOnTypeWithCallToExtensionMethodOnFieldType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayOnTypeWithCallToMethodOnFieldType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerTypeProxyAttributeOnAssemblyUsingTarget ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerTypeProxyAttributeOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}