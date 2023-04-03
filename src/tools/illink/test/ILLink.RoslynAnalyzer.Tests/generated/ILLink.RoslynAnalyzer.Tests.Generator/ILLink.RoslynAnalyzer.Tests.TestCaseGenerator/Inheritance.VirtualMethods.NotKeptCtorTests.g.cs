using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.VirtualMethods
{
	public sealed partial class NotKeptCtorTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.VirtualMethods.NotKeptCtor";

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}