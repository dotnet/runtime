using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[Reference ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll")]
	[ReferenceDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb")]
	[SetupLinkerLinkSymbols( "true")]
	[SetupLinkerArgument ("--deterministic")]

#if WIN32
	[KeptSymbols ("LibraryWithPdb.dll")]
#else
	[RemovedSymbols ("LibraryWithPdb.dll")]
#endif
	[KeptMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "NotUsed()")]
	public class ReferenceWithPdbAndSymbolLinkingEnabledAndDeterministicMvid {
		static void Main ()
		{
			// Use some stuff so that we can verify that the linker output correct results
			SomeMethod();
			LibraryWithPdb.SomeMethod ();
		}

		[Kept]
		static void SomeMethod ()
		{
		}

		static void NotUsed ()
		{
		}
	}
}