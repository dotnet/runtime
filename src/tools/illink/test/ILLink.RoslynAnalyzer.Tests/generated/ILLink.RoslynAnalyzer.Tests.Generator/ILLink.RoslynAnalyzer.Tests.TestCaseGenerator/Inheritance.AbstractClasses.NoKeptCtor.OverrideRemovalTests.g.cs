using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.AbstractClasses.NoKeptCtor
{
	public sealed partial class OverrideRemovalTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval";

		[Fact]
		public Task CanDisableOverrideRemoval ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfAbstractIsKeptNonEmpty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfVirtualCanBeRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfVirtualCanBeRemoved2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideOfVirtualCanBeRemoved3 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideThatAlsoFulfilsInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreservesOverriddenMethodOverrideOfUsedVirtualStillRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreservesOverriddenMethodOverrideOfUsedVirtualStillRemoved2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}