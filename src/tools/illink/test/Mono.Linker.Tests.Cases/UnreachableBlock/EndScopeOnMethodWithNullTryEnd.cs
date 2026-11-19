// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
    [SkipUnresolved(true)]
    [Define("IL_ASSEMBLY_AVAILABLE")]
    [SetupCompileBefore("library.dll", new[] { "Dependencies/EndScopeOnMethod.il" })]
    [SetupLinkerArgument("--enable-opt", "ipconstprop")]
    [KeptMemberInAssembly("library.dll", "Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod",
        "TryEndingAtMethodEnd()", "UnreachableTryEndingAtMethodEnd()", "HandlerWasReached()", Tool = Tool.Trimmer)]
    [RemovedMemberInAssembly("library.dll", "Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod",
        "UnreachableHandlerWasReached()", Tool = Tool.Trimmer)]
    public class EndScopeOnMethodWithNullTryEnd
    {
        public static void Main()
        {
#if IL_ASSEMBLY_AVAILABLE
            Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod.TryEndingAtMethodEnd();
            Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod.UnreachableTryEndingAtMethodEnd();
#endif
        }
    }
}
