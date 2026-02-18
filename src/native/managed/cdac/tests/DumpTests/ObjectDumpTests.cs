// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Object and GC contracts.
/// Uses the GCRoots debuggee dump, which pins objects and creates GC handles.
/// </summary>
public abstract class ObjectDumpTestsBase : DumpTestBase
{
    protected ObjectDumpTestsBase()
    {
        LoadDump();
    }

    protected override string DebuggeeName => "GCRoots";

    [Fact]
    public void Object_ContractIsAvailable()
    {
        IObject objectContract = Target.Contracts.Object;
        Assert.NotNull(objectContract);
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void GC_ContractIsAvailable()
    {
        IGC gcContract = Target.Contracts.GC;
        Assert.NotNull(gcContract);
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void GC_HeapCountIsNonZero()
    {
        IGC gcContract = Target.Contracts.GC;
        uint heapCount = gcContract.GetGCHeapCount();
        Assert.True(heapCount > 0, "Expected at least one GC heap");
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void GC_MaxGenerationIsReasonable()
    {
        IGC gcContract = Target.Contracts.GC;
        uint maxGen = gcContract.GetMaxGeneration();
        // .NET typically has gen0, gen1, gen2 (maxGen = 2)
        Assert.True(maxGen >= 1 && maxGen <= 4,
            $"Expected max generation between 1 and 4, got {maxGen}");
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "GC contract is not available in .NET 10 dumps")]
    public void GC_CanGetHeapData()
    {
        IGC gcContract = Target.Contracts.GC;
        GCHeapData heapData = gcContract.GetHeapData();
        Assert.NotNull(heapData.GenerationTable);
        Assert.True(heapData.GenerationTable.Count > 0, "Expected at least one generation");
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
