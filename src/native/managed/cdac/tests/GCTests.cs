// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GCTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHeapData_ReturnsCorrectGenerationTable(MockTarget.Architecture arch)
    {
        var generations = new MockGCBuilder.Generation[]
        {
            new() { StartSegment = 0xAA00_0000, AllocationStart = 0xAA00_1000, AllocContextPointer = 0xAA00_2000, AllocContextLimit = 0xAA00_3000 },
            new() { StartSegment = 0xBB00_0000, AllocationStart = 0xBB00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xCC00_0000, AllocationStart = 0xCC00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xDD00_0000, AllocationStart = 0xDD00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc =>
            {
                gc.Generations = generations;
                gc.FillPointers = fillPointers;
            })
            .Build();
        IGC gc = target.Contracts.GC;

        GCHeapData heapData = gc.GetHeapData();

        Assert.Equal(generations.Length, heapData.GenerationTable.Count);
        for (int i = 0; i < generations.Length; i++)
        {
            Assert.Equal(generations[i].StartSegment, (ulong)heapData.GenerationTable[i].StartSegment);
            Assert.Equal(generations[i].AllocationStart, (ulong)heapData.GenerationTable[i].AllocationStart);
            Assert.Equal(generations[i].AllocContextPointer, (ulong)heapData.GenerationTable[i].AllocationContextPointer);
            Assert.Equal(generations[i].AllocContextLimit, (ulong)heapData.GenerationTable[i].AllocationContextLimit);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHeapData_ReturnsCorrectFillPointers(MockTarget.Architecture arch)
    {
        var generations = new MockGCBuilder.Generation[]
        {
            new() { StartSegment = 0xAA00_0000, AllocationStart = 0xAA00_1000, AllocContextPointer = 0xAA00_2000, AllocContextLimit = 0xAA00_3000 },
            new() { StartSegment = 0xBB00_0000, AllocationStart = 0xBB00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xCC00_0000, AllocationStart = 0xCC00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xDD00_0000, AllocationStart = 0xDD00_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1111, 0x2222, 0x3333, 0x4444, 0x5555, 0x6666, 0x7777];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc =>
            {
                gc.Generations = generations;
                gc.FillPointers = fillPointers;
            })
            .Build();
        IGC gc = target.Contracts.GC;

        GCHeapData heapData = gc.GetHeapData();

        Assert.Equal(fillPointers.Length, heapData.FillPointers.Count);
        for (int i = 0; i < fillPointers.Length; i++)
        {
            Assert.Equal(fillPointers[i], (ulong)heapData.FillPointers[i]);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHeapData_WithFiveGenerations(MockTarget.Architecture arch)
    {
        var generations = new MockGCBuilder.Generation[]
        {
            new() { StartSegment = 0xA000_0000, AllocationStart = 0xA000_1000, AllocContextPointer = 0xA000_2000, AllocContextLimit = 0xA000_3000 },
            new() { StartSegment = 0xB000_0000, AllocationStart = 0xB000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xC000_0000, AllocationStart = 0xC000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xD000_0000, AllocationStart = 0xD000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = 0xE000_0000, AllocationStart = 0xE000_1000, AllocContextPointer = 0, AllocContextLimit = 0 },
        };

        ulong[] fillPointers = [0x1001, 0x2002, 0x3003, 0x4004, 0x5005, 0x6006, 0x7007];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc =>
            {
                gc.Generations = generations;
                gc.FillPointers = fillPointers;
            })
            .Build();
        IGC gc = target.Contracts.GC;

        GCHeapData heapData = gc.GetHeapData();

        Assert.Equal(5, heapData.GenerationTable.Count);
        for (int i = 0; i < generations.Length; i++)
        {
            Assert.Equal(generations[i].StartSegment, (ulong)heapData.GenerationTable[i].StartSegment);
            Assert.Equal(generations[i].AllocationStart, (ulong)heapData.GenerationTable[i].AllocationStart);
        }

        Assert.Equal(fillPointers.Length, heapData.FillPointers.Count);
        for (int i = 0; i < fillPointers.Length; i++)
        {
            Assert.Equal(fillPointers[i], (ulong)heapData.FillPointers[i]);
        }
    }
}

/// <summary>
/// Configuration object for GC heap mock data, used with
/// <see cref="GCHeapBuilderExtensions.AddGCHeapWks"/> and
/// <see cref="GCHeapBuilderExtensions.AddGCHeapSvr"/>.
/// </summary>
internal class GCHeapBuilder
{
    // The native GC sizes m_FillPointers as total_generation_count + ExtraSegCount.
    private const int DefaultGenerationCount = 4;
    private const int ExtraSegCount = 2;

    public MockGCBuilder.Generation[] Generations { get; set; } = new MockGCBuilder.Generation[DefaultGenerationCount];
    public ulong[] FillPointers { get; set; } = new ulong[DefaultGenerationCount + ExtraSegCount];
}

internal static class GCHeapBuilderExtensions
{
    public static TestPlaceholderTarget.Builder AddGCHeapWks(
        this TestPlaceholderTarget.Builder targetBuilder,
        Action<GCHeapBuilder> configure)
    {
        GCHeapBuilder config = new();
        configure(config);
        BuildWksHeap(targetBuilder, config);
        targetBuilder.AddContract<IGC>(target =>
            ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1));
        return targetBuilder;
    }

    public static TestPlaceholderTarget.Builder AddGCHeapSvr(
        this TestPlaceholderTarget.Builder targetBuilder,
        Action<GCHeapBuilder> configure,
        out ulong heapAddress)
    {
        GCHeapBuilder config = new();
        configure(config);
        heapAddress = BuildSvrHeap(targetBuilder, config);
        targetBuilder.AddContract<IGC>(target =>
            ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1));
        return targetBuilder;
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockGCBuilder gcBuilder)
        => new()
        {
            [DataType.GCAllocContext] = TargetTestHelpers.CreateTypeInfo(gcBuilder.GCAllocContextLayout),
            [DataType.Generation] = TargetTestHelpers.CreateTypeInfo(gcBuilder.GenerationLayout),
            [DataType.CFinalize] = TargetTestHelpers.CreateTypeInfo(gcBuilder.CFinalizeLayout),
            [DataType.OomHistory] = TargetTestHelpers.CreateTypeInfo(gcBuilder.OomHistoryLayout),
        };

    private static Dictionary<DataType, Target.TypeInfo> CreateServerContractTypes(MockGCBuilder gcBuilder, uint generationCount)
    {
        Dictionary<DataType, Target.TypeInfo> types = CreateContractTypes(gcBuilder);
        types[DataType.GCHeap] = TargetTestHelpers.CreateTypeInfo(gcBuilder.GetGCHeapSVRLayout(generationCount));

        return types;
    }

    private static void BuildWksHeap(TestPlaceholderTarget.Builder targetBuilder, GCHeapBuilder config)
    {
        MockMemorySpace.Builder memBuilder = targetBuilder.MemoryBuilder;
        MockGCBuilder gcBuilder = new(memBuilder);

        MockGCBuilder.Generation[] generations = config.Generations;
        uint generationCount = (uint)generations.Length;
        ulong[] fillPointers = config.FillPointers;
        uint fillPointersLength = (uint)fillPointers.Length;

        Dictionary<DataType, Target.TypeInfo> types = CreateContractTypes(gcBuilder);
        ulong generationTableAddress = gcBuilder.AddGenerationTable(generations);
        MockCFinalize cFinalize = gcBuilder.AddCFinalize(fillPointers);
        MockOomHistory oomHistory = gcBuilder.AddOomHistory();
        gcBuilder.WritePointerGlobal(gcBuilder.FinalizeQueueGlobalAddress, cFinalize.Address);
        gcBuilder.WritePointerGlobal(gcBuilder.HighestAddressGlobalAddress, 0xFFFF_0000);
        gcBuilder.WriteUInt32Global(gcBuilder.MaxGenerationGlobalAddress, generationCount - 1);

        targetBuilder.AddTypes(types);
        targetBuilder.AddGlobals(
            (nameof(Constants.Globals.TotalGenerationCount), generationCount),
            (nameof(Constants.Globals.CFinalizeFillPointersLength), fillPointersLength),
            (nameof(Constants.Globals.InterestingDataLength), 0UL),
            (nameof(Constants.Globals.CompactReasonsLength), 0UL),
            (nameof(Constants.Globals.ExpandMechanismsLength), 0UL),
            (nameof(Constants.Globals.InterestingMechanismBitsLength), 0UL),
            (nameof(Constants.Globals.HandlesPerBlock), 32UL),
            (nameof(Constants.Globals.BlockInvalid), 1UL),
            (nameof(Constants.Globals.DebugDestroyedHandleValue), 0UL),
            (nameof(Constants.Globals.HandleMaxInternalTypes), 12UL),
            (nameof(Constants.Globals.HandleSegmentSize), 0x10000UL),
            (nameof(Constants.Globals.GCHeapMarkArray), gcBuilder.MarkArrayGlobalAddress),
            (nameof(Constants.Globals.GCHeapNextSweepObj), gcBuilder.NextSweepObjGlobalAddress),
            (nameof(Constants.Globals.GCHeapBackgroundMinSavedAddr), gcBuilder.BackgroundMinSavedAddrGlobalAddress),
            (nameof(Constants.Globals.GCHeapBackgroundMaxSavedAddr), gcBuilder.BackgroundMaxSavedAddrGlobalAddress),
            (nameof(Constants.Globals.GCHeapAllocAllocated), gcBuilder.AllocAllocatedGlobalAddress),
            (nameof(Constants.Globals.GCHeapEphemeralHeapSegment), gcBuilder.EphemeralHeapSegmentGlobalAddress),
            (nameof(Constants.Globals.GCHeapCardTable), gcBuilder.CardTableGlobalAddress),
            (nameof(Constants.Globals.GCHeapFinalizeQueue), gcBuilder.FinalizeQueueGlobalAddress),
            (nameof(Constants.Globals.GCHeapGenerationTable), generationTableAddress),
            (nameof(Constants.Globals.GCHeapOomData), oomHistory.Address),
            (nameof(Constants.Globals.GCHeapInternalRootArray), gcBuilder.InternalRootArrayGlobalAddress),
            (nameof(Constants.Globals.GCHeapInternalRootArrayIndex), gcBuilder.InternalRootArrayIndexGlobalAddress),
            (nameof(Constants.Globals.GCHeapHeapAnalyzeSuccess), gcBuilder.HeapAnalyzeSuccessGlobalAddress),
            (nameof(Constants.Globals.GCHeapInterestingData), gcBuilder.InterestingDataAddress),
            (nameof(Constants.Globals.GCHeapCompactReasons), gcBuilder.CompactReasonsAddress),
            (nameof(Constants.Globals.GCHeapExpandMechanisms), gcBuilder.ExpandMechanismsAddress),
            (nameof(Constants.Globals.GCHeapInterestingMechanismBits), gcBuilder.InterestingMechanismBitsAddress),
            (nameof(Constants.Globals.GCLowestAddress), gcBuilder.LowestAddressGlobalAddress),
            (nameof(Constants.Globals.GCHighestAddress), gcBuilder.HighestAddressGlobalAddress),
            (nameof(Constants.Globals.StructureInvalidCount), gcBuilder.StructureInvalidCountGlobalAddress),
            (nameof(Constants.Globals.MaxGeneration), gcBuilder.MaxGenerationGlobalAddress));
        targetBuilder.AddGlobalStrings(
            (nameof(Constants.Globals.GCIdentifiers), "workstation,segments"));
    }

    private static ulong BuildSvrHeap(TestPlaceholderTarget.Builder targetBuilder, GCHeapBuilder config)
    {
        MockMemorySpace.Builder memBuilder = targetBuilder.MemoryBuilder;
        MockGCBuilder gcBuilder = new(memBuilder);

        MockGCBuilder.Generation[] generations = config.Generations;
        uint generationCount = (uint)generations.Length;
        ulong[] fillPointers = config.FillPointers;
        uint fillPointersLength = (uint)fillPointers.Length;

        Dictionary<DataType, Target.TypeInfo> types = CreateServerContractTypes(gcBuilder, generationCount);
        MockCFinalize cFinalize = gcBuilder.AddCFinalize(fillPointers, "CFinalize_SVR");
        MockGCHeapSVR gcHeap = gcBuilder.AddGCHeapSVR(generations, cFinalize.Address);
        ulong heapTableAddress = gcBuilder.AddPointerGlobal(gcHeap.Address, "HeapTable");
        gcBuilder.WritePointerGlobal(gcBuilder.HeapsGlobalAddress, heapTableAddress);
        gcBuilder.WritePointerGlobal(gcBuilder.HighestAddressGlobalAddress, 0x7FFF_0000);
        gcBuilder.WriteUInt32Global(gcBuilder.MaxGenerationGlobalAddress, generationCount - 1);

        targetBuilder.AddTypes(types);
        targetBuilder.AddGlobals(
            (nameof(Constants.Globals.TotalGenerationCount), generationCount),
            (nameof(Constants.Globals.CFinalizeFillPointersLength), fillPointersLength),
            (nameof(Constants.Globals.InterestingDataLength), 0UL),
            (nameof(Constants.Globals.CompactReasonsLength), 0UL),
            (nameof(Constants.Globals.ExpandMechanismsLength), 0UL),
            (nameof(Constants.Globals.InterestingMechanismBitsLength), 0UL),
            (nameof(Constants.Globals.HandlesPerBlock), 32UL),
            (nameof(Constants.Globals.BlockInvalid), 1UL),
            (nameof(Constants.Globals.DebugDestroyedHandleValue), 0UL),
            (nameof(Constants.Globals.HandleMaxInternalTypes), 12UL),
            (nameof(Constants.Globals.HandleSegmentSize), 0x10000UL),
            (nameof(Constants.Globals.NumHeaps), gcBuilder.NumHeapsGlobalAddress),
            (nameof(Constants.Globals.Heaps), gcBuilder.HeapsGlobalAddress),
            (nameof(Constants.Globals.GCLowestAddress), gcBuilder.LowestAddressGlobalAddress),
            (nameof(Constants.Globals.GCHighestAddress), gcBuilder.HighestAddressGlobalAddress),
            (nameof(Constants.Globals.StructureInvalidCount), gcBuilder.StructureInvalidCountGlobalAddress),
            (nameof(Constants.Globals.MaxGeneration), gcBuilder.MaxGenerationGlobalAddress));
        targetBuilder.AddGlobalStrings(
            (nameof(Constants.Globals.GCIdentifiers), "server,segments"));

        return gcHeap.Address;
    }
}
