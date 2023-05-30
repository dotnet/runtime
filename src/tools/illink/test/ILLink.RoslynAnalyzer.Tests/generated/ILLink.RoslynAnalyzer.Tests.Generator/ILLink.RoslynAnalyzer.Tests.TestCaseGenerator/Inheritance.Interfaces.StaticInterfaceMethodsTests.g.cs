using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class StaticInterfaceMethodsTests : LinkerTestBase
	{

		[Fact]
		public Task OverrideInCopyAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideInSaveAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VarianceBasic ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}