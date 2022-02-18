using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class DynamicDependenciesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "DynamicDependencies";

		[Fact]
		public Task DynamicDependencyField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyFromAttributeXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyFromAttributeXmlOnNonReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyFromCopiedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyKeptOption ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMemberSignatureWildcard ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMemberTypes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMethodInAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMethodInNonReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMethodInNonReferencedAssemblyChained ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMethodInNonReferencedAssemblyChainedReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMethodInNonReferencedAssemblyWithEmbeddedXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyMethodInNonReferencedAssemblyWithSweptReferences ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyOnUnusedMethodInNonReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithEmbeddedXml ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}