﻿using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Reference ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll")]
	[ReferenceDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll.mdb")]
	[SetupLinkerLinkSymbols ("false")]

	[RemovedSymbols ("LibraryWithMdb.dll")]

	[KeptMemberInAssembly ("LibraryWithMdb.dll", typeof (LibraryWithMdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithMdb.dll", typeof (LibraryWithMdb), "NotUsed()")]
	public class ReferenceWithMdb
	{
		static void Main ()
		{
			// Use some stuff so that we can verify that the linker output correct results
			SomeMethod ();

			LibraryWithMdb.SomeMethod ();
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
