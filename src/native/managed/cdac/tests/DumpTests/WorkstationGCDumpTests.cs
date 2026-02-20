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
public abstract class WorkstationGCDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";

    [ConditionalFact]
    public void WorkstationGC_ContractIsAvailable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        Assert.NotNull(gcContract);
    }

    [ConditionalFact]
    public void WorkstationGC_IsWorkstationGC()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;

        string[] gcIdentifiers = gcContract.GetGCIdentifiers();
        Assert.Contains(GCIdentifiers.Workstation, gcIdentifiers);

        uint heapCount = gcContract.GetGCHeapCount();
        Assert.Equal(1u, heapCount);
    }

    [ConditionalFact]
    public void WorkstationGC_MaxGenerationIsReasonable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        uint maxGen = gcContract.GetMaxGeneration();
        Assert.True(maxGen >= 1 && maxGen <= 4,
            $"Expected max generation between 1 and 4, got {maxGen}");
    }

    [ConditionalFact]
    public void WorkstationGC_StructuresAreValid()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        bool valid = gcContract.GetGCStructuresValid();
        Assert.True(valid, "Expected GC structures to be valid in a dump taken outside of GC");
    }

    [ConditionalFact]
    public void WorkstationGC_CanEnumerateHeaps()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        uint heapCount = gcContract.GetGCHeapCount();

        List<TargetPointer> heaps = gcContract.GetGCHeaps().ToList();
        Assert.Equal((int)heapCount, heaps.Count);
        foreach (TargetPointer heap in heaps)
        {
            Assert.NotEqual(TargetPointer.Null, heap);
        }
    }

    [ConditionalFact]
    public void WorkstationGC_CanGetHeapData()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        GCHeapData heapData = gcContract.GetHeapData();
        Assert.NotNull(heapData.GenerationTable);
        Assert.True(heapData.GenerationTable.Count > 0, "Expected at least one generation");
    }

    [ConditionalFact]
    public void WorkstationGC_BoundsAreReasonable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        gcContract.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr);
        Assert.True(minAddr < maxAddr,
            $"Expected GC min address (0x{minAddr:X}) < max address (0x{maxAddr:X})");
    }
}

public class WorkstationGCDumpTests_Local : WorkstationGCDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class WorkstationGCDumpTests_Net10 : WorkstationGCDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
