
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop
{
	public sealed class InternalCallsTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Interop/InternalCalls";

		[Fact]
		public Task UnusedDefaultConstructorIsRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedFieldsOfTypesAreNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedFieldsOfTypesWhenHasThisAreNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task DefaultConstructorOfReturnTypeIsNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedDefaultConstructorOfTypePassedByRefIsNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedFieldsOfTypesPassedByRefAreNotRemoved ()
		{
			return RunTest ();
		}
	}
}