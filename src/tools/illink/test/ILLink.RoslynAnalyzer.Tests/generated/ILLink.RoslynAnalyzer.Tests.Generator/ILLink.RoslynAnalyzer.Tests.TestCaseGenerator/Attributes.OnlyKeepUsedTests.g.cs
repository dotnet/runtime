using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Attributes
{
	public sealed partial class OnlyKeepUsedTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes.OnlyKeepUsed";

		[Fact]
		public Task AttributeDefinedAndUsedInOtherAssemblyIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeUsedByAttributeIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanLinkCoreLibrariesWithOnlyKeepUsedAttributes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ComAttributesArePreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ContextStaticIsPreservedOnField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CoreLibraryUnusedAssemblyAttributesAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CoreLibraryUsedAssemblyAttributesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FixedLengthArrayAttributesArePreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodWithUnmanagedConstraint ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NullableOnConstraintsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NullableOnConstraintsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ThreadStaticIsPreservedOnField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeOnGenericParameterIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeOnReturnTypeIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributePreservedViaLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeTypeOnAssemblyIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeTypeOnEventIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeTypeOnMethodIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeTypeOnModuleIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeTypeOnParameterIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeTypeOnPropertyIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeTypeOnTypeIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAttributeWithTypeForwarderIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedDerivedAttributeType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAttributeTypeOnAssemblyIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAttributeTypeOnEventIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAttributeTypeOnMethodIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAttributeTypeOnModuleIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAttributeTypeOnParameterIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAttributeTypeOnPropertyIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedAttributeTypeOnTypeIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
