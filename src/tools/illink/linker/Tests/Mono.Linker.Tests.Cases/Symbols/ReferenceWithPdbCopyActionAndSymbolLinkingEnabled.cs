using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[Reference ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll")]
	[ReferenceDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb")]
	[SetupLinkerLinkSymbols ("true")]
	[SetupLinkerAction ("copy", "LibraryWithPdb")]

	[KeptSymbols ("LibraryWithPdb.dll")]
	public class ReferenceWithPdbCopyActionAndSymbolLinkingEnabled {
		static void Main ()
		{
			LibraryWithPdb.SomeMethod ();
		}
	}
}