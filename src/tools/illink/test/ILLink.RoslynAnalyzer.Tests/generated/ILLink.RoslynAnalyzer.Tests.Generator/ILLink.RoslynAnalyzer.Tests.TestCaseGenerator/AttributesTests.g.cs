using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class AttributesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes";

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
		public Task MarshalAsCustomMarshalerInterface ()
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