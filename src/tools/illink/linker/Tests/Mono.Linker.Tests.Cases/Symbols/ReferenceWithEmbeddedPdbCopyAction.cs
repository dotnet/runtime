using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[IgnoreTestCase ("Will fix in follow on PR.  Fails due to embedded symbols not being removed")]
	[SetupCompileBefore ("LibraryWithEmbeddedPdbSymbols.dll", new[] { "Dependencies/LibraryWithEmbeddedPdbSymbols.cs" }, additionalArguments: "/debug:embedded", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("false")]
	[SetupLinkerAction ("copy", "LibraryWithEmbeddedPdbSymbols")]

	[RemovedSymbols ("LibraryWithEmbeddedPdbSymbols.dll")]
	
	// Copying with symbol linking off is a little more complex for embedded pdbs.
	// Do a little extra asserting here to make sure the assembly wasn't accidentally linked
	[KeptMemberInAssembly ("LibraryWithEmbeddedPdbSymbols.dll", typeof (LibraryWithEmbeddedPdbSymbols), "NotUsed()")]
	public class ReferenceWithEmbeddedPdbCopyAction {
		static void Main ()
		{
			LibraryWithEmbeddedPdbSymbols.SomeMethod ();
		}
	}
}