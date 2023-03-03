using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Attributes
{
	public sealed partial class DebuggerTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes.Debugger";

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
		public Task DebuggerDisplayAttributeOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnTypeCopy ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnTypeThatIsNotUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DebuggerDisplayAttributeOnTypeWithNonExistentMethod ()
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