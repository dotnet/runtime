using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance
{
	public sealed partial class InterfacesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces";

		[Fact]
		public Task CanDisableUnusedInterfaces ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceImplementedRecursively ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceOnUninstantiatedTypeRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceVariants ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task InterfaceWithoutNewSlot ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
