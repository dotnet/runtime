using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class RecursiveInterfacesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.RecursiveInterfaces";

		[Fact]
		public Task GenericInterfaceImplementedRecursively ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceImplementedRecursively ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfRecursiveInterfaceIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RecursiveInterfaceKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
