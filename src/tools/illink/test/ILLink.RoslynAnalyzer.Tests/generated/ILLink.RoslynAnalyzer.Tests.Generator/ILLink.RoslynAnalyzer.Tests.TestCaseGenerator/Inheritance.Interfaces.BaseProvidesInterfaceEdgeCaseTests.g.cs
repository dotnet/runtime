using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class BaseProvidesInterfaceEdgeCaseTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.BaseProvidesInterfaceEdgeCase";

		[Fact]
		public Task BaseProvidesInterfaceMethodCircularReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}