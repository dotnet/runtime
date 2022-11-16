using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance
{
	public sealed partial class VirtualMethodsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.VirtualMethods";

		[Fact]
		public Task BaseIsInSkipAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task HarderToDetectUnusedVirtualMethodGetsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NeverInstantiatedTypeWithBaseInCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractInUnmarkedClassIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithOverrideOfVirtualMethodIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedVirtualMethodRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedOverrideOfVirtualMethodIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedTypeWithOverrideOfVirtualMethodHasOverrideKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VirtualMethodGetsPreservedIfBaseMethodGetsInvoked ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VirtualMethodGetsStrippedIfImplementingMethodGetsInvokedDirectly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}