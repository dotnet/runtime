// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [ExpectedNoWarnings]
    [SkipKeptItemsValidation]
    [SetupCompileArgument("/optimize+")]
    [SetupCompileArgument("/main:Mono.Linker.Tests.Cases.DataFlow.CompilerGeneratedTypesReleaseNet90")]
    [SandboxDependency("CompilerGeneratedTypes.cs")]
    // Without a TargetFramework attribute, the linker uses the pre-.NET10 behavior
    // which uses heuristics to understand compiler-generated types
    [GenerateTargetFrameworkAttribute(false)]
    class CompilerGeneratedTypesReleaseNet90
    {
        // This test just links the CompilerGeneratedTypes test in the Release configuration
        // without setting the TargetFramework attribute, to test the pre-.NET10 behavior of
        // the linker's compiler-generated state machine handling.
        public static void Main()
        {
            CompilerGeneratedTypes.Main();
        }
    }
}
