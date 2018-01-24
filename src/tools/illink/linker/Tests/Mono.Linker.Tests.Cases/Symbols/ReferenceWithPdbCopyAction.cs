using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[Reference ("LibraryWithPdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll", "input/LibraryWithPdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb", "input/LibraryWithPdb.pdb")]
	[SetupLinkerLinkSymbols ("false")]
	[SetupLinkerAction ("copy", "LibraryWithPdb")]

	[RemovedSymbols ("LibraryWithPdb.dll")]
	public class ReferenceWithPdbCopyAction {
		static void Main ()
		{
			LibraryWithPdb.SomeMethod ();
		}
	}
}