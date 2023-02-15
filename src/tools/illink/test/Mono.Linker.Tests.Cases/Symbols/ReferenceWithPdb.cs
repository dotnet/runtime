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

	[RemovedSymbols ("LibraryWithPdb.dll")]

	[KeptMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "NotUsed()")]
	public class ReferenceWithPdb
	{
		static void Main ()
		{
			// Use some stuff so that we can verify that the linker output correct results
			SomeMethod ();
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