// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
public class ServerGCDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "ServerGC";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_IsServerGC(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;

        string[] gcIdentifiers = gcContract.GetGCIdentifiers();
        Assert.Contains(GCIdentifiers.Server, gcIdentifiers);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_MaxGenerationIsReasonable(TestConfiguration config)
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
    public void ServerGC_StructuresAreValid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        bool valid = gcContract.GetGCStructuresValid();
        Assert.True(valid, "Expected GC structures to be valid in a dump taken outside of GC");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_CanEnumerateHeaps(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        uint heapCount = gcContract.GetGCHeapCount();

        List<TargetPointer> heaps = gcContract.GetGCHeaps().ToList();
        Assert.Equal((int)heapCount, heaps.Count);
        foreach (TargetPointer heap in heaps)
        {
            Assert.NotEqual(TargetPointer.Null, heap);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_CanGetHeapData(TestConfiguration config)
    {
        InitializeDumpTest(config);
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_BoundsAreReasonable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        gcContract.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr);
        Assert.True(minAddr < maxAddr,
            $"Expected GC min address (0x{minAddr:X}) < max address (0x{maxAddr:X})");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_EachHeapHasGenerationData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        List<TargetPointer> heaps = gcContract.GetGCHeaps().ToList();

        foreach (TargetPointer heap in heaps)
        {
            GCHeapData heapData = gcContract.GetHeapData(heap);
            Assert.NotNull(heapData.GenerationTable);
            Assert.True(heapData.GenerationTable.Count > 0,
                $"Expected generation table for heap 0x{heap:X} to be non-empty");
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_GetHandleTableMemoryRegions(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;

        IReadOnlyList<GCMemoryRegionData> regions = gcContract.GetHandleTableMemoryRegions();
        Assert.NotNull(regions);
        Assert.True(regions.Count > 0, "Expected at least one handle table memory region");
        Assert.All(regions, region =>
        {
            Assert.NotEqual(TargetPointer.Null, region.Start);
            Assert.True(region.Size > 0, $"Expected non-zero size for region starting at 0x{region.Start:X}");
        });
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_HandleTableRegionsSpanMultipleHeaps(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;

        IReadOnlyList<GCMemoryRegionData> regions = gcContract.GetHandleTableMemoryRegions();
        Assert.True(regions.Count > 0, "Expected at least one handle table memory region");

        Assert.All(regions, region =>
            Assert.True(region.Heap >= 0,
                $"Heap index {region.Heap} should be non-negative"));

        HashSet<int> observedHeaps = new(regions.Select(r => r.Heap));
        Assert.True(observedHeaps.Count > 1,
            $"Expected handle table regions across multiple CPU slots, but only found slot(s): {string.Join(", ", observedHeaps)}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_GetGCBookkeepingMemoryRegions(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;

        IReadOnlyList<GCMemoryRegionData> regions = gcContract.GetGCBookkeepingMemoryRegions();
        Assert.NotNull(regions);
        Assert.True(regions.Count > 0, "Expected at least one bookkeeping memory region");
        Assert.All(regions, region =>
        {
            Assert.NotEqual(TargetPointer.Null, region.Start);
            Assert.True(region.Size > 0, $"Expected non-zero size for region starting at 0x{region.Start:X}");
        });

        HashSet<TargetPointer> uniqueStarts = new(regions.Select(r => r.Start));
        Assert.Equal(regions.Count, uniqueStarts.Count);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_GetGCFreeRegions(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;

        IReadOnlyList<GCMemoryRegionData> regions = gcContract.GetGCFreeRegions();
        Assert.NotNull(regions);
        Assert.All(regions, region =>
        {
            Assert.NotEqual(TargetPointer.Null, region.Start);
            Assert.True(region.Size > 0, $"Expected non-zero size for region starting at 0x{region.Start:X}");
        });
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_FreeRegionsHaveValidKindAndHeap(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        uint heapCount = gcContract.GetGCHeapCount();

        IReadOnlyList<GCMemoryRegionData> regions = gcContract.GetGCFreeRegions();
        Assert.NotNull(regions);
        Assert.All(regions, region =>
        {
            FreeRegionKind kind = (FreeRegionKind)region.ExtraData;
            Assert.True(Enum.IsDefined(kind),
                $"ExtraData {region.ExtraData} is not a valid FreeRegionKind");
            Assert.True(kind != FreeRegionKind.FreeUnknownRegion,
                $"Region at 0x{region.Start:X} has FreeUnknownRegion kind");
            Assert.True(region.Heap >= 0 && region.Heap < (int)heapCount,
                $"Heap index {region.Heap} out of range [0, {heapCount})");
        });
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void ServerGC_CanEnumerateExpectedHandles(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;

        var pinnedHandles = gcContract.GetHandles([HandleType.Pinned]);
        Assert.True(
            pinnedHandles.Count >= 10,
            $"Expected at least 10 pinned handles, found {pinnedHandles.Count}");
        Assert.All(pinnedHandles, handle => Assert.NotEqual(TargetPointer.Null, handle.Handle));

        var strongHandles = gcContract.GetHandles([HandleType.Strong]);
        Assert.True(strongHandles.Count >= 1, "Expected at least 1 strong handle");
        Assert.All(strongHandles, handle => Assert.NotEqual(TargetPointer.Null, handle.Handle));
        Assert.All(strongHandles, handle => Assert.True(handle.StrongReference));

        var weakShortHandles = gcContract.GetHandles([HandleType.WeakShort]);
        Assert.True(weakShortHandles.Count >= 1, "Expected at least 1 weak-short handle");
        Assert.All(weakShortHandles, handle => Assert.NotEqual(TargetPointer.Null, handle.Handle));

        var weakLongHandles = gcContract.GetHandles([HandleType.WeakLong]);
        Assert.True(weakLongHandles.Count >= 1, "Expected at least 1 weak-long handle");
        Assert.All(weakLongHandles, handle => Assert.NotEqual(TargetPointer.Null, handle.Handle));

        var dependentHandles = gcContract.GetHandles([HandleType.Dependent]);
        Assert.True(dependentHandles.Count >= 1, "Expected at least 1 dependent handle");
        Assert.All(dependentHandles, handle => Assert.NotEqual(TargetPointer.Null, handle.Handle));

        Assert.Contains(
            dependentHandles,
            handle => handle.Secondary != TargetPointer.Null);
    }

}
