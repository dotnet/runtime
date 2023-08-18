﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Individual.Dependencies;

namespace Mono.Linker.Tests.Cases.References.Individual
{
	[ExpectNonZeroExitCode (1)]
	[NoLinkedOutput]
	[SetupCompileBefore ("library1.dll", new[] { "Dependencies/CanSkipUnresolved_Library.cs" })]
	[SetupCompileAfter ("library1.dll", new[] { "Dependencies/CanSkipUnresolved_Library.cs" }, defines: new[] { "EXCLUDE_STUFF" })]
	public class CanSkipUnresolved
	{
		static void Main ()
		{
			var t1 = new CanSkipUnresolved_Library.TypeWithMissingMethod ();
			t1.GoingToBeMissing ();

			var t2 = new CanSkipUnresolved_Library.TypeThatWillBeMissing ();
		}
	}
}
