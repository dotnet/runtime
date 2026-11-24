// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
    [IgnoreTestCase("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
    [KeptAttributeAttribute(typeof(IgnoreTestCaseAttribute), By = Tool.Trimmer)]
    [SetupLinkerArgument("-a", "test", "library")]

    [SetupCompileBefore("Forwarder.dll", new[] { "../TypeForwarding/Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

    // After compiling the test case we then replace the reference impl with implementation + type forwarder
    [SetupCompileAfter("Implementation.dll", new[] { "../TypeForwarding/Dependencies/ImplementationLibrary.cs" })]
    [SetupCompileAfter("Forwarder.dll", new[] { "../TypeForwarding/Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

    // References from a library keep the assemblies it was compiled against only if those are kept as they are, so a
    // facade which is trimmed away still has its references rewritten to the implementation
    [RemovedAssembly("Forwarder.dll")]
    [RemovedAssemblyReference("test", "Forwarder")]
    [KeptMemberInAssembly("Implementation.dll", typeof(ImplementationLibrary), "GetSomeValue()")]
    public class LibraryModeForwarderIsRemoved
    {
        [Kept]
        public LibraryModeForwarderIsRemoved()
        {
        }

        [Kept]
        public static void Main()
        {
            new ImplementationLibrary().GetSomeValue();
        }
    }
}
