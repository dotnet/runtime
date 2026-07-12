// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

// The test assembly references "conditional.dll" only by the assembly name string.
// All TypeMap entries in conditional.dll are conditional on AllConditionalTrimTarget,
// which is never marked. So conditional.dll ends up with no surviving TypeMap entries.
// The fix should cause this TypeMapAssemblyTarget attribute to be removed.
// (No KeptAttributeAttribute here - the test framework verifies the attribute is NOT in the linked output.)
[assembly: TypeMapAssemblyTarget<AllConditionalGroupType>("conditional")]

namespace Mono.Linker.Tests.Cases.Reflection
{
    // Compile the group-type assembly first so both the test assembly and conditional.dll can reference it.
    // Compile conditional.dll second with addAsReference:false so the test assembly has no compile-time
    // dependency on it (only the string reference in TypeMapAssemblyTarget).
    [SetupCompileBefore("allconditionalgroup.dll", new[] { "Dependencies/TypeMapAllConditionalGroupDep.cs" })]
    [SetupCompileBefore("conditional.dll", new[] { "Dependencies/TypeMapAllConditionalEntriesDep.cs" },
        references: new[] { "allconditionalgroup.dll" }, addAsReference: false)]
    [SetupLinkerAction("link", "System.Private.CoreLib")] // Needed to apply embedded XML (RemoveAttributeInstances)
    [SetupLinkerArgument("--ignore-link-attributes", "false")]
    [Kept]
    class TypeMapAssemblyTargetRemovedWhenAllEntriesTrimmed
    {
        [Kept]
        static void Main()
        {
            // Use the group so the trimmer processes the TypeMapAssemblyTarget attribute.
            _ = TypeMapping.GetOrCreateExternalTypeMapping<AllConditionalGroupType>();
        }
    }
}
