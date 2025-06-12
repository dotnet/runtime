using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class AttributesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes";

		[Fact]
		public Task AssemblyAttributeAccessesMembers ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyAttributeIsRemovedIfOnlyTypesUsedInAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyAttributeKeptInComplexCase ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnAssemblyIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnAssemblyIsKeptIfDeclarationIsSkipped ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnParameterInUsedMethodIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnPreservedTypeIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnPreservedTypeWithTypeUsedInConstructorIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnPreservedTypeWithTypeUsedInDifferentNamespaceIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnPreservedTypeWithTypeUsedInFieldIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnPreservedTypeWithTypeUsedInPropertySetterIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnPreservedTypeWithUsedSetter ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnUsedFieldIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnUsedMethodIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AttributeOnUsedPropertyIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task BoxedValues ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CoreLibraryAssemblyAttributesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FixedLengthArrayAttributesArePreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task GenericAttributes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task IVTUnused ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task IVTUnusedKeptWhenKeepingUsedAttributesOnly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task IVTUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MarshalAsCustomMarshaler ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MarshalAsCustomMarshalerInterface ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SecurityAttributesOnUsedMethodAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SecurityAttributesOnUsedTypeAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedInObjectArrayConstructorArgumentOnAttributeIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeWithDynamicInterfaceCastableImplementationAttributeIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
