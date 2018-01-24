using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[IgnoreTestCase ("Will fix in follow on PR.  Fails due to test project not being setup to reference and build Mono.Cecil.Mdb")]
	[Reference ("LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll", "input/LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll.mdb", "input/LibraryWithMdb.dll.mdb")]
	[SetupLinkerLinkSymbols ("true")]
	[SetupLinkerAction ("copy", "LibraryWithMdb")]

	[KeptSymbols ("LibraryWithMdb.dll")]
	public class ReferenceWithMdbCopyActionAndSymbolLinkingEnabled {
		static void Main ()
		{
			LibraryWithMdb.SomeMethod ();
		}
	}
}