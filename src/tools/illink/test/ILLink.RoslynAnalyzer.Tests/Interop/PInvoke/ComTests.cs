
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop.PInvoke
{
	public sealed class ComTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Interop/PInvoke/Com";

		[Fact]
		public Task DefaultConstructorOfParameterIsRemoved ()
		{
            return RunTest();
		}

		[Fact]
		public Task DefaultConstructorOfReturnTypeIsRemoved ()
		{
            return RunTest();
		}

		[Fact]
		public Task FieldsOfParameterAreRemoved ()
		{
            return RunTest();
		}

		[Fact]
		public Task FieldsOfReturnTypeAreRemoved ()
		{
            return RunTest();
		}

		[Fact]
		public Task FieldsOfThisAreRemoved ()
		{
            return RunTest();
		}
	}
}