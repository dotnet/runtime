using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class LinkAttributesTests : LinkerTestBase
	{

		[Fact]
		public Task AssemblyLevelLinkerAttributeRemoval ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkAttributesInReferencedAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EmbeddedLinkAttributesInReferencedAssembly_AssemblyLevel ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FeatureAttributeRemovalInCopyAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkAttributeErrorCases ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LinkerAttributeRemovalAndPreserveAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OverrideAttributeRemoval ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypedArgumentsErrors ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
