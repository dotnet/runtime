// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("copy", "lib")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[LogDoesNotContain ("IL2026")]
	[LogDoesNotContain ("IL3002")]
	[LogDoesNotContain ("IL2027")]
	public class RequiresCapabilityFromCopiedAssembly
	{
		public static void Main ()
		{
			Test ();
		}

		[Kept]
		static void Test ()
		{
			var x = new RequiresInCopyAssembly ();
		}
	}
}
