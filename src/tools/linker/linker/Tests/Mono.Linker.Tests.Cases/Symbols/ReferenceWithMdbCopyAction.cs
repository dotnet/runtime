using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[Reference ("LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll", "input/LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll.mdb", "input/LibraryWithMdb.dll.mdb")]
	[SetupLinkerLinkSymbols ("false")]
	[SetupLinkerAction ("copy", "LibraryWithMdb")]

	[RemovedSymbols ("LibraryWithMdb.dll")]
	public class ReferenceWithMdbCopyAction {
		static void Main ()
		{
			LibraryWithMdb.SomeMethod ();
		}
	}
}