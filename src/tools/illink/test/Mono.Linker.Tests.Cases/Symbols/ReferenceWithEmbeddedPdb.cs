using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileBefore ("LibraryWithEmbeddedPdbSymbols.dll", new[] { "Dependencies/LibraryWithEmbeddedPdbSymbols.cs" }, additionalArguments: "/debug:embedded", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("false")]

	[RemovedSymbols ("LibraryWithEmbeddedPdbSymbols.dll")]

	[KeptMemberInAssembly ("LibraryWithEmbeddedPdbSymbols.dll", typeof (LibraryWithEmbeddedPdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithEmbeddedPdbSymbols.dll", typeof (LibraryWithEmbeddedPdbSymbols), "NotUsed()")]
	class ReferenceWithEmbeddedPdb
	{
		static void Main ()
		{
			LibraryWithEmbeddedPdbSymbols.SomeMethod ();
		}
	}
}
