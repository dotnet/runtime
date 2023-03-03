using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ResourcesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Resources";

		[Fact]
		public Task EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileIsNotProcessedIfNameDoesNotMatchAnAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileIsNotProcessedWithIgnoreDescriptors ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileIsNotProcessedWithIgnoreDescriptorsAndRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileIsProcessed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileIsProcessedAndKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileIsProcessedIfNameMatchesAnAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkXmlFileWithTypePreserve ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NonLinkerEmbeddedResourceHasNoImpact ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}