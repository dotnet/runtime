// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the GC contract in workstation GC mode.
/// Uses the GCRoots debuggee dump, which pins objects and creates GC handles
/// under the default workstation GC.
/// </summary>
public class WorkstationGCDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_IsWorkstationGC(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;

        string[] gcIdentifiers = gcContract.GetGCIdentifiers();
        Assert.Contains(GCIdentifiers.Workstation, gcIdentifiers);

        uint heapCount = gcContract.GetGCHeapCount();
        Assert.Equal(1u, heapCount);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_MaxGenerationIsReasonable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        uint maxGen = gcContract.GetMaxGeneration();
        Assert.True(maxGen >= 1 && maxGen <= 4,
            $"Expected max generation between 1 and 4, got {maxGen}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_StructuresAreValid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        bool valid = gcContract.GetGCStructuresValid();
        Assert.True(valid, "Expected GC structures to be valid in a dump taken outside of GC");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_CanEnumerateHeaps(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        uint heapCount = gcContract.GetGCHeapCount();
        Assert.Equal(1u, heapCount);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_CanGetHeapData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        GCHeapData heapData = gcContract.GetHeapData();
        Assert.NotNull(heapData.GenerationTable);
        Assert.True(heapData.GenerationTable.Count > 0, "Expected at least one generation");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_BoundsAreReasonable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        gcContract.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr);
        Assert.True(minAddr < maxAddr,
            $"Expected GC min address (0x{minAddr:X}) < max address (0x{maxAddr:X})");
    }
}
