// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

public class AuxiliarySymbolsDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Allocation helpers are not included in the .NET 10 auxiliary symbol table")]
    public void JitHelpersAreReachableByAddress(TestConfiguration config)
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

        TargetPointer table = Target.ReadGlobalPointer(Constants.Globals.AuxiliarySymbols);
        uint count = Target.Read<uint>(Target.ReadGlobalPointer(Constants.Globals.AuxiliarySymbolCount));
        Target.TypeInfo typeInfo = Target.GetTypeInfo(DataType.AuxiliarySymbolInfo);
        uint entrySize = typeInfo.Size!.Value;
        int addressOffset = typeInfo.Fields["Address"].Offset;
        int nameOffset = typeInfo.Fields["Name"].Offset;

        for (uint i = 0; i < count; i++)
        {
            TargetPointer entry = table + ((ulong)i * entrySize);
            TargetPointer namePointer = Target.ReadPointer(entry + (ulong)nameOffset);
            string name = Target.ReadUtf8String(namePointer);
            if (expectedHelpers.Remove(name))
            {
                TargetCodePointer address = Target.ReadCodePointer(entry + (ulong)addressOffset);
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
