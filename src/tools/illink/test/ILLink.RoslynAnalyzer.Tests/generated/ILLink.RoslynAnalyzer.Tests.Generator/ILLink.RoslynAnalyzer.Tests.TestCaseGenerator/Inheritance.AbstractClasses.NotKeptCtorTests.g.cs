using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.AbstractClasses
{
	public sealed partial class NotKeptCtorTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.AbstractClasses.NotKeptCtor";

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly4 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly5 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly6 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}