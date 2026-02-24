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
        Assert.All(strongHandles, handle => Assert.NotEqual(0u, handle.StrongReference));

        var weakShortHandles = gcContract.GetHandles([HandleType.WeakShort]);
        Assert.True(weakShortHandles.Count >= 1, "Expected at least 1 weak-short handle");
        Assert.All(weakShortHandles, handle => Assert.NotEqual(TargetPointer.Null, handle.Handle));

        var dependentHandles = gcContract.GetHandles([HandleType.Dependent]);
        Assert.True(dependentHandles.Count >= 1, "Expected at least 1 dependent handle");
        Assert.All(dependentHandles, handle => Assert.NotEqual(TargetPointer.Null, handle.Handle));

        Assert.Contains(
            dependentHandles,
            handle => handle.Secondary != TargetPointer.Null);
    }

}
