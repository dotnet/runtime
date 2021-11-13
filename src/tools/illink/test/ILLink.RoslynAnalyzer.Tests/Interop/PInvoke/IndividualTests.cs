
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop.PInvoke
{
	public class IndividualTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Interop/PInvoke/Individual";

		[Fact]
		public Task CanOutputPInvokes ()
		{
			return RunTest ();
		}
	}
}