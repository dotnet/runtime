// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
    // Actions:
    // link - This assembly (default), Forwarder.dll and Implementation.dll
    // Rooting the test assembly with -a should not cause Forwarder.dll to be preserved,
    // because type references are rewritten to point directly to Implementation.dll.
    [SetupLinkerArgument("-a", "test")]

    [SetupCompileBefore("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

    // After compiling the test case we then replace the reference impl with implementation + type forwarder
    [SetupCompileAfter("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
    [SetupCompileAfter("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

    [RemovedAssembly("Forwarder.dll")]
    [RemovedAssemblyReference("test", "Forwarder")]
    [KeptMemberInAssembly("Implementation.dll", typeof(ImplementationLibrary), "GetSomeValue()")]
    [KeptMember(".ctor()")]
    class UsedForwarderIsRemovedWhenReferencedByRootedAssembly
    {
        static void Main()
        {
            new ImplementationLibrary().GetSomeValue();
        }
    }
}
