using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class PreserveDependenciesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "PreserveDependencies";

		[Fact]
		public Task PreserveDependencyDeprecated ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyErrorCases ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyFromCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyKeptOption ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyMemberSignatureWildcard ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyMethodInAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyMethodInNonReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyMethodInNonReferencedAssemblyChained ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyMethodInNonReferencedAssemblyChainedReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyMethodInNonReferencedAssemblyWithEmbeddedXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyOnUnusedMethodInNonReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithEmbeddedXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}