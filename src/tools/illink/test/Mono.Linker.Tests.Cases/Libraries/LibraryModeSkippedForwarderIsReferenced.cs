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
    [SetupLinkerDefaultAction("skip")]
    [SetupLinkerAction("link", "test")]

    [SetupCompileBefore("Forwarder.dll", new[] { "../TypeForwarding/Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

    // After compiling the test case we then replace the reference impl with implementation + type forwarder
    [SetupCompileAfter("Implementation.dll", new[] { "../TypeForwarding/Dependencies/ImplementationLibrary.cs" })]
    [SetupCompileAfter("Forwarder.dll", new[] { "../TypeForwarding/Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

    // The library keeps referencing the facade it was compiled against since the facade is kept as it is
    [KeptReferencesInAssembly("test.dll", new[] { "System.Runtime", "Mono.Linker.Tests.Cases.Expectations", "Forwarder" })]
    public class LibraryModeSkippedForwarderIsReferenced
    {
        [Kept]
        public LibraryModeSkippedForwarderIsReferenced()
        {
        }

        [Kept]
        public static void Main()
        {
            new ImplementationLibrary().GetSomeValue();
        }
    }
}
