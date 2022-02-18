using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class TestFrameworkTests : LinkerTestBase
	{

		protected override string TestSuiteName => "TestFramework";

		[Fact]
		public Task CanCheckInitializersByIndex ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanCompileReferencesUsingTypes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanCompileReferencesWithResources ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanCompileReferencesWithResourcesWithCsc ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanCompileReferencesWithResourcesWithMcs ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanSandboxDependenciesUsingType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanVerifyInterfacesOnTypesInAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VerifyAttributesInAssemblyWorks ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VerifyAttributesInAssemblyWorksWithStrings ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VerifyExpectModifiedAttributesWork ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task VerifyResourceInAssemblyAttributesBehavior ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}