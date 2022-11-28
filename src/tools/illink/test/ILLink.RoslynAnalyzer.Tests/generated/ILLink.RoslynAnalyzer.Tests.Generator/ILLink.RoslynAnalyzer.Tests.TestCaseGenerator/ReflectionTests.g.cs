using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ReflectionTests : LinkerTestBase
	{

		[Fact]
		public Task AssemblyImportedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyImportedViaReflectionWithDerivedType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyImportedViaReflectionWithReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AsType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CoreLibMessages ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ObjectGetTypeLibraryMode ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ParametersUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RunClassConstructorUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeHierarchyLibraryModeSuppressions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnderlyingSystemType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedViaReflectionIntegrationTest ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}