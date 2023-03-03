using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[IgnoreTestCase ("Test relies on checked-in binaries: https://github.com/dotnet/runtime/issues/78344")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Reference ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll")]
	[ReferenceDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb")]

	[SetupCompileBefore ("LibraryWithCompilerDefaultSymbols.dll", new[] { "Dependencies/LibraryWithCompilerDefaultSymbols.cs" }, additionalArguments: "/debug:full")]
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: "/debug:portable", compilerToUse: "csc")]
	[SetupCompileBefore ("LibraryWithEmbeddedPdbSymbols.dll", new[] { "Dependencies/LibraryWithEmbeddedPdbSymbols.cs" }, additionalArguments: "/debug:embedded", compilerToUse: "csc")]

	[SetupCompileArgument ("/debug:full")]
	[SetupLinkerLinkSymbols ("true")]

	[KeptSymbols ("test.exe")]
#if WIN32
	[KeptSymbols ("LibraryWithPdb.dll")]
#else
	[RemovedSymbols ("LibraryWithPdb.dll")]
#endif
	[KeptSymbols ("LibraryWithCompilerDefaultSymbols.dll")]
	[KeptSymbols ("LibraryWithEmbeddedPdbSymbols.dll")]
	[KeptSymbols ("LibraryWithPortablePdbSymbols.dll")]

	[KeptMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "NotUsed()")]

	[KeptMemberInAssembly ("LibraryWithCompilerDefaultSymbols.dll", typeof (LibraryWithCompilerDefaultSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithCompilerDefaultSymbols.dll", typeof (LibraryWithCompilerDefaultSymbols), "NotUsed()")]

	[KeptMemberInAssembly ("LibraryWithEmbeddedPdbSymbols.dll", typeof (LibraryWithEmbeddedPdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithEmbeddedPdbSymbols.dll", typeof (LibraryWithEmbeddedPdbSymbols), "NotUsed()")]

	[KeptMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "NotUsed()")]
	public class ReferencesWithMixedSymbolTypesAndSymbolLinkingEnabled
	{
		static void Main ()
		{
			// Use some stuff so that we can verify that the linker output correct results
			SomeMethod ();
			LibraryWithCompilerDefaultSymbols.SomeMethod ();
			LibraryWithPdb.SomeMethod ();
			LibraryWithEmbeddedPdbSymbols.SomeMethod ();
			LibraryWithPortablePdbSymbols.SomeMethod ();
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
