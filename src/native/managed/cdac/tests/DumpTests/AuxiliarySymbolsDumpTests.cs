// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

public class AuxiliarySymbolsDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Allocation helpers are not included in the .NET 10 auxiliary symbol table")]
    public void JitHelpersAreIncludedInHeapDump(TestConfiguration config)
    {
        InitializeDumpTest(config);

        HashSet<string> expectedHelpers =
        [
            "CORINFO_HELP_NEWFAST",
            "CORINFO_HELP_NEWFAST_MAYBEFROZEN",
            "CORINFO_HELP_NEWSFAST",
            "CORINFO_HELP_NEWSFAST_ALIGN8",
            "CORINFO_HELP_NEWSFAST_ALIGN8_VC",
            "CORINFO_HELP_NEWARR_1_DIRECT",
            "CORINFO_HELP_NEWARR_1_MAYBEFROZEN",
            "CORINFO_HELP_NEWARR_1_PTR",
            "CORINFO_HELP_NEWARR_1_VC",
            "CORINFO_HELP_NEWARR_1_ALIGN8",
        ];
        Dictionary<TargetCodePointer, string> helpersByAddress = [];

        foreach ((TargetCodePointer address, string name) in
            Target.Contracts.AuxiliarySymbols.EnumerateAuxiliarySymbols())
        {
            if (expectedHelpers.Remove(name))
            {
                Assert.NotEqual(TargetCodePointer.Null, address);
                helpersByAddress.TryAdd(address, name);
            }
        }

        Assert.Empty(expectedHelpers);
        foreach ((TargetCodePointer address, string expectedName) in helpersByAddress)
        {
            Assert.True(Target.Contracts.AuxiliarySymbols.TryGetAuxiliarySymbolName(address.AsTargetPointer, out string? name));
            Assert.Equal(expectedName, name);
        }
    }
}
