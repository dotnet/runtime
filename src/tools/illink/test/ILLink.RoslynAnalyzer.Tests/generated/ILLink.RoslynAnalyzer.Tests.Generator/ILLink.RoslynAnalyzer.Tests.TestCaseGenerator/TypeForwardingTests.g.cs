using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class TypeForwardingTests : LinkerTestBase
	{

		protected override string TestSuiteName => "TypeForwarding";

		[Fact]
		public Task AttributeArgumentForwarded ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeArgumentForwardedWithCopyAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeEnumArgumentForwarded ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeEnumArgumentForwardedCopyUsedWithSweptForwarder ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributesScopeUpdated ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MissingTargetReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MultiForwardedTypesWithCopyUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MultiForwardedTypesWithLink ()
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
		public Task TypeForwarderOnlyAssemblyCanBePreservedViaLinkXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeForwardersModifiers ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeForwardersRewrite ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedForwarderWithAssemblyCopyIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedForwarderWithAssemblyCopyUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedForwarderWithAssemblyLinked ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedForwarderWithAssemblyLinkedAndFacadeCopy ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAndUnusedForwarderReferencedFromCopyUsedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAndUnusedForwarderWithAssemblyCopy ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderAndUnusedReference ()
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

		[Fact]
		public Task UsedForwarderInGenericIsDynamicallyAccessedWithAssemblyCopyUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderIsDynamicallyAccessedWithAssemblyCopyUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderIsRemovedWhenLink ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderWithAssemblyCopy ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderWithAssemblyCopyUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderWithAssemblyCopyUsedAndForwarderLibraryKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedForwarderWithAssemblyCopyUsedAndUnusedReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedTransitiveForwarderInCopyAssemblyIsDynamicallyAccessed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedTransitiveForwarderInCopyUsedAssemblyIsDynamicallyAccessed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedTransitiveForwarderIsDynamicallyAccessed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedTransitiveForwarderIsResolvedAndFacadeRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedTransitiveForwarderIsResolvedAndFacadeRemovedInCopyAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}