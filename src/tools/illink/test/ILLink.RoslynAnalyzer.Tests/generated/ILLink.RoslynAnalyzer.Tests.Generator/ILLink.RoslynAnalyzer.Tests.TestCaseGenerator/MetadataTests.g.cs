using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class MetadataTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Metadata";

		[Fact]
		public Task DebuggerDisplayNamesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NamesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NamesAreRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootAllAssemblyNamesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootDescriptorNamesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootLibraryAssemblyNamesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RootVisibleAssemblyNamesAreKept ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}
