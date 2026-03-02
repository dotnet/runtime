// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_CanEnumerateExpectedHandles(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        var pinnedHandles = gcContract.GetHandles([HandleType.Pinned]);
        Assert.True(
            pinnedHandles.Count >= 5,
            $"Expected at least 5 pinned handles, found {pinnedHandles.Count}");
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void WorkstationGC_GlobalAllocationContextIsReadable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        gcContract.GetGlobalAllocationContext(out TargetPointer pointer, out TargetPointer limit);

        if (pointer != TargetPointer.Null)
        {
            Assert.NotEqual(TargetPointer.Null, limit);
            Assert.True(pointer <= limit,
                $"Expected allocPtr (0x{pointer:X}) <= allocLimit (0x{limit:X})");
        }
    }
}
