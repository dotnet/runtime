// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine;

#if DEBUG
[IgnoreTestCase("This test will trip a Debug.Assert which means letting the test run would lead to a failure")]
#endif
[SetupLinkerArgument("-a", "test.exe", "entrypoint")]
[SetupLinkerArgument("-a", "../input/test.exe", "entrypoint")]
public class DuplicateRootAssembly
{
    public static void Main()
    {

    }
}
