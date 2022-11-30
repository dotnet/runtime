using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class TypeForwardingTests : LinkerTestBase
	{

		protected override string TestSuiteName => "TypeForwarding";

		[Fact]
		public Task MissingTargetReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SecurityAttributeScope ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeForwardedIsUpdatedForMissingType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeForwarderOnlyAssembliesRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByPreserveDependency ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByUsedCustomAttribute ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByUsedField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByUsedInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByUsedMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByUsedNestedType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByUsedProperty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderInCopyAssemblyKeptByUsedTypeAsGenericArg ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}