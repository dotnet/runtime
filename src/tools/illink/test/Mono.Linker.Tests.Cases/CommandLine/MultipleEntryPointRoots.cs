// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine;

[ExpectNonZeroExitCode(1)]
[SetupCompileBefore("other.exe", new[] { "Dependencies/MultipleEntryPointRoots_Lib.cs" })]
[SetupLinkerArgument("-a", "test.exe", "entrypoint")]
[SetupLinkerArgument("-a", "other.exe", "entrypoint")]
[LogContains("IL1048")]
[NoLinkedOutput]
public class MultipleEntryPointRoots
{
    public static void Main()
    {
    }
}
