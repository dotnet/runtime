using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class LibrariesTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Libraries";

		[Fact]
		public Task CanLinkPublicApisOfLibrary ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CopyUsedAssemblyWithMainEntryRoot ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CopyUsedAssemblyWithPublicRoots ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DefaultLibraryLinkBehavior ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task LibraryWithUnresolvedInterfaces ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootAllLibraryBehavior ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootAllLibraryCopyBehavior ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootLibrary ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootLibraryInternalsWithIVT ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootLibraryVisibleAndDescriptor ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootLibraryVisibleForwarders ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootLibraryVisibleForwardersWithoutReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UserAssemblyActionWorks ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
