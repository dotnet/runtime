using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class SymbolsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Symbols";

		[Fact]
		public Task AssemblyWithDefaultSymbols ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyWithDefaultSymbolsAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferencesWithMixedSymbolTypes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferencesWithMixedSymbolTypesAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferencesWithMixedSymbolTypesWithMdbAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdb ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdbAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdbAndSymbolLinkingEnabledAndDeterministicMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdbAndSymbolLinkingEnabledAndNewMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdbCopyAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdbCopyActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdbDeleteAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithEmbeddedPdbDeleteActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdb ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdbAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdbAndSymbolLinkingEnabledAndDeterministicMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdbAndSymbolLinkingEnabledAndNewMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdbCopyAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdbCopyActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdbDeleteAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithMdbDeleteActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdb ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdbAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdbAndSymbolLinkingEnabledAndDeterministicMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdbAndSymbolLinkingEnabledAndNewMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdbCopyAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdbCopyActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdbDeleteAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPdbDeleteActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdb ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdbAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdbAndSymbolLinkingEnabledAndDeterministicMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdbAndSymbolLinkingEnabledAndNewMvid ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdbCopyAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdbCopyActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdbDeleteAction ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ReferenceWithPortablePdbDeleteActionAndSymbolLinkingEnabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}