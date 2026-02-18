// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Object and GC contracts.
/// Uses the GCRoots debuggee dump, which pins objects and creates GC handles.
/// </summary>
public abstract class ObjectDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";

    [ConditionalFact]
    public void Object_ContractIsAvailable()
    {
        IObject objectContract = Target.Contracts.Object;
        Assert.NotNull(objectContract);
    }

    [ConditionalFact]
    public void GC_ContractIsAvailable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        Assert.NotNull(gcContract);
    }

    [ConditionalFact]
    public void GC_HeapCountIsNonZero()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        uint heapCount = gcContract.GetGCHeapCount();
        Assert.True(heapCount > 0, "Expected at least one GC heap");
    }

    [ConditionalFact]
    public void GC_MaxGenerationIsReasonable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        uint maxGen = gcContract.GetMaxGeneration();
        // .NET typically has gen0, gen1, gen2 (maxGen = 2)
        Assert.True(maxGen >= 1 && maxGen <= 4,
            $"Expected max generation between 1 and 4, got {maxGen}");
    }

    [ConditionalFact]
    public void GC_CanGetHeapData()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        GCHeapData heapData = gcContract.GetHeapData();
        Assert.NotNull(heapData.GenerationTable);
        Assert.True(heapData.GenerationTable.Count > 0, "Expected at least one generation");
    }

    [ConditionalFact]
    public void GC_StructuresAreValid()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        bool valid = gcContract.GetGCStructuresValid();
        Assert.True(valid, "Expected GC structures to be valid in a dump taken outside of GC");
    }

    [ConditionalFact]
    public void GC_CanEnumerateHeaps()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        uint heapCount = gcContract.GetGCHeapCount();

        // For workstation GC (heapCount == 1), GetGCHeaps may return an empty list
        // since the heap is accessed via GetHeapData() instead. For server GC
        // (heapCount > 1), GetGCHeaps should return the per-heap pointers.
        if (heapCount > 1)
        {
            var heaps = gcContract.GetGCHeaps().ToList();
            Assert.True(heaps.Count > 0, "Expected at least one GC heap pointer for server GC");
            foreach (TargetPointer heap in heaps)
            {
                Assert.NotEqual(TargetPointer.Null, heap);
            }
        }
    }

    [ConditionalFact]
    public void GC_BoundsAreReasonable()
    {
        SkipIfVersion("net10.0", "GC contract is not available in .NET 10 dumps");
        IGC gcContract = Target.Contracts.GC;
        gcContract.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr);
        // Min address should be less than max address
        Assert.True(minAddr < maxAddr,
            $"Expected GC min address (0x{minAddr:X}) < max address (0x{maxAddr:X})");
    }

    [ConditionalFact]
    public void Object_StringMethodTableHasCorrectComponentSize()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(stringMT);

        // String component size should be sizeof(char) = 2
        uint componentSize = rts.GetComponentSize(handle);
        Assert.Equal(2u, componentSize);
    }
}

public class ObjectDumpTests_Local : ObjectDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class ObjectDumpTests_Net10 : ObjectDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
