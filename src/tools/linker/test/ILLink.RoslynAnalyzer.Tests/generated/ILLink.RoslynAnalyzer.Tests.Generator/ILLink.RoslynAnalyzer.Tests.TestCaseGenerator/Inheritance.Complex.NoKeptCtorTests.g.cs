using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Complex
{
	public sealed partial class NoKeptCtorTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Complex.NoKeptCtor";

		[Fact]
		public Task OverrideOfAbstractAndInterfaceMethodWhenInterfaceRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractAndInterfaceMethodWhenInterfaceRemoved2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}