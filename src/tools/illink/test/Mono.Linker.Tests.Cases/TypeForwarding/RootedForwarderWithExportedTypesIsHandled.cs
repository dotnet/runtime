// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
    // Actions:
    // link - This assembly (default), Forwarder.dll and Implementation.dll
    // Rooting Forwarder.dll with -a (link action) while it is itself a type forwarder:
    // MarkExportedTypes marks the exported type entries without TypeReferenceMarker running,
    // and the implementation assembly is still preserved because the forwarded type is used.
    [SetupLinkerArgument("-a", "Forwarder")]

    [SetupCompileBefore("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

    // After compiling the test case we then replace the reference impl with implementation + type forwarder
    [SetupCompileAfter("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
    [SetupCompileAfter("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

    [KeptAssembly("Forwarder.dll")]
    [KeptTypeInAssembly("Forwarder.dll", typeof(ImplementationLibrary))]
    [RemovedAssemblyReference("test", "Forwarder")]
    [KeptMemberInAssembly("Implementation.dll", typeof(ImplementationLibrary), "GetSomeValue()")]
    class RootedForwarderWithExportedTypesIsHandled
    {
        static void Main()
        {
            new ImplementationLibrary().GetSomeValue();
        }
    }
}
