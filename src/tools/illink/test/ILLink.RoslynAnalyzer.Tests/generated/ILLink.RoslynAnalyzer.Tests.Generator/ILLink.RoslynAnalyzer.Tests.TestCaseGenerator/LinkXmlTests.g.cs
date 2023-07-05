using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class LinkXmlTests : LinkerTestBase
	{

		protected override string TestSuiteName => "LinkXml";

		[Fact]
		public Task AssemblyWithPreserveAll ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanPreserveAnExportedType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanPreserveExcludedFeatureCom ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanPreserveExportedTypesUsingRegex ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanPreserveNamespace ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanPreserveTypesUsingRegex ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFromCopyAssemblyIsProcessed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlUnresolvedReferencesAreReported ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkXmlErrorCases ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveIndividualMembersOfNonRequiredType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveSecondLevelMethodsOfNonRequiredType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeWithPreserveFieldsHasBackingFieldsOfPropertiesRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedAssemblyWithNoDefinedPreserveHasAllTypesPreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedEventPreservedByLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedFieldPreservedByLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedGenericTypeWithPreserveAllHasAllMembersPreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedInterfaceTypeOnTypeWithPreserveAllIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedInterfaceTypeOnTypeWithPreserveNothingIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedMethodPreservedByLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedNestedTypePreservedByLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedNonRequiredTypeIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedPropertyPreservedByLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeDeclarationPreservedByLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeIsPresservedWhenEntireAssemblyIsPreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypePreservedByLinkXmlIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypePreservedByLinkXmlWithCommentIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithNoDefinedPreserveHasAllMembersPreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveAllHasAllMembersPreserved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveFieldsHasMethodsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveMethodsHasFieldsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveNothingAndPreserveMembers ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedTypeWithPreserveNothingHasMembersRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedNonRequiredExportedTypeIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedNonRequiredExportedTypeIsKeptWhenRooted ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedNonRequiredTypeIsKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedNonRequiredTypeIsKeptWithSingleMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}