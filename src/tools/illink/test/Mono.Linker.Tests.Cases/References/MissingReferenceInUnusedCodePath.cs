// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References;

[ExpectNonZeroExitCode (1)]
[NoLinkedOutput]
[SetupCompileBefore ("missing.dll", new[] { "Dependencies/MissingAssembly.cs" })]
[DeleteBefore ("missing.dll")]
[SetupLinkerArgument ("--skip-unresolved", "false")]
public class MissingReferenceInUnusedCodePath
{
	public static void Main ()
	{
	}

	private static void UnusedPath ()
	{
		typeof (MissingAssembly).ToString ();
	}
}
