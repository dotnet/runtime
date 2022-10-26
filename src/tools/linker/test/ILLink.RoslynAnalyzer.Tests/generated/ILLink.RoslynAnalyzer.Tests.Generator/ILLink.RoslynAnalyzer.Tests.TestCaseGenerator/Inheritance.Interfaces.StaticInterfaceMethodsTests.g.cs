using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class StaticInterfaceMethodsTests : LinkerTestBase
	{

		[Fact]
		public Task BaseProvidesInterfaceMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}