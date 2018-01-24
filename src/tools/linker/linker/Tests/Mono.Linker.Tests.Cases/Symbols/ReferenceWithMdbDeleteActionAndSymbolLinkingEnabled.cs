using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[Reference ("LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll", "input/LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll.mdb", "input/LibraryWithMdb.dll.mdb")]
	[SetupLinkerLinkSymbols ("true")]

	[RemovedAssembly ("LibraryWithMdb.dll")]
	[RemovedSymbols ("LibraryWithMdb.dll")]
	public class ReferenceWithMdbDeleteActionAndSymbolLinkingEnabled {
		static void Main ()
		{
		}

		/// <summary>
		/// By not using this method we will cause the linker to delete the reference
		/// </summary>
		static void UnusedCodePath ()
		{
			LibraryWithMdb.SomeMethod ();
		}
	}
}