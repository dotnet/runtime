using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[IgnoreTestCase ("Test relies on checked-in binaries: https://github.com/dotnet/runtime/issues/78344")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Reference ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll")]
	[ReferenceDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb")]
	[SetupLinkerLinkSymbols ("false")]
	[SetupLinkerAction ("copy", "LibraryWithPdb")]

	[RemovedSymbols ("LibraryWithPdb.dll")]
	public class ReferenceWithPdbCopyAction
	{
		static void Main ()
		{
			LibraryWithPdb.SomeMethod ();
		}
	}
}