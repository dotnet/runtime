// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the GC contract in server GC mode.
/// Uses the ServerGC debuggee dump, which enables server GC and allocates
/// objects across multiple heaps.
/// </summary>
public abstract class ServerGCDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "ServerGC";

    [ConditionalFact]
    public void ServerGC_ContractIsAvailable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        Assert.NotNull(gcContract);
    }

    [ConditionalFact]
    public void ServerGC_IsServerGC()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        // Server GC rejects the parameterless GetHeapData() — verify it uses
        // the per-heap overload instead, confirming this is a server GC dump.
        Assert.Throws<System.InvalidOperationException>(() => gcContract.GetHeapData());
    }

    [ConditionalFact]
    public void ServerGC_MaxGenerationIsReasonable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        uint maxGen = gcContract.GetMaxGeneration();
        Assert.True(maxGen >= 1 && maxGen <= 4,
            $"Expected max generation between 1 and 4, got {maxGen}");
    }

    [ConditionalFact]
    public void ServerGC_StructuresAreValid()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        bool valid = gcContract.GetGCStructuresValid();
        Assert.True(valid, "Expected GC structures to be valid in a dump taken outside of GC");
    }

    [ConditionalFact]
    public void ServerGC_CanEnumerateHeaps()
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
    public void ServerGC_CanGetHeapData()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        List<TargetPointer> heaps = gcContract.GetGCHeaps().ToList();
        Assert.True(heaps.Count > 0, "Expected at least one GC heap");

        foreach (TargetPointer heap in heaps)
        {
            GCHeapData heapData = gcContract.GetHeapData(heap);
            Assert.NotNull(heapData.GenerationTable);
            Assert.True(heapData.GenerationTable.Count > 0, "Expected at least one generation");
        }
    }

    [ConditionalFact]
    public void ServerGC_BoundsAreReasonable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        gcContract.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr);
        Assert.True(minAddr < maxAddr,
            $"Expected GC min address (0x{minAddr:X}) < max address (0x{maxAddr:X})");
    }

    [ConditionalFact]
    public void ServerGC_EachHeapHasGenerationData()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        uint maxGen = gcContract.GetMaxGeneration();
        List<TargetPointer> heaps = gcContract.GetGCHeaps().ToList();

        foreach (TargetPointer heap in heaps)
        {
            GCHeapData heapData = gcContract.GetHeapData(heap);
            Assert.NotNull(heapData.GenerationTable);
            Assert.True(heapData.GenerationTable.Count > 0,
                $"Expected generation table for heap 0x{heap:X} to be non-empty");
        }
    }
}

public class ServerGCDumpTests_Local : ServerGCDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class ServerGCDumpTests_Net10 : ServerGCDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
