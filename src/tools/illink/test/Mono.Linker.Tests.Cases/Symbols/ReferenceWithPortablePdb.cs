﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: new[] { "/debug:portable" }, compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("false")]

	[RemovedSymbols ("LibraryWithPortablePdbSymbols.dll")]

	[KeptMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "NotUsed()")]
	class ReferenceWithPortablePdb
	{
		static void Main ()
		{
			LibraryWithPortablePdbSymbols.SomeMethod ();
		}
	}
}
