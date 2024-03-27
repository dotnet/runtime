using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class DefaultInterfaceMethodsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.DefaultInterfaceMethods";

		[Fact]
		public Task DefaultInterfaceMethodCallIntoClass ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericDefaultInterfaceMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceWithAttributeOnImplementation ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MostSpecificDefaultImplementationKeptInstance ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MostSpecificDefaultImplementationKeptStatic ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SimpleDefaultInterfaceMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticDefaultInterfaceMethodOnStruct ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedDefaultInterfaceImplementation ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
