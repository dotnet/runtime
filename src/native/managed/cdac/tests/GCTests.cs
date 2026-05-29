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

    private sealed record CapturedSegment(ulong Start, ulong End, GCSegmentClassification Generation);

    private static MockGCBuilder.Generation[] MakeGenerations(ulong gen0Seg, ulong gen0Start, ulong gen1Seg, ulong gen1Start, ulong gen2Seg, ulong lohSeg, ulong pohSeg)
        =>
        [
            new() { StartSegment = gen0Seg, AllocationStart = gen0Start, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = gen1Seg, AllocationStart = gen1Start, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = gen2Seg, AllocationStart = 0, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = lohSeg, AllocationStart = 0, AllocContextPointer = 0, AllocContextLimit = 0 },
            new() { StartSegment = pohSeg, AllocationStart = 0, AllocContextPointer = 0, AllocContextLimit = 0 },
        ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateHeapSegments_Wks_Regions(MockTarget.Architecture arch)
    {
        ulong allocAllocated = 0x4000_0500;
        ulong frozenSeg = 0, gen2Seg = 0, gen1Seg = 0, gen0Seg = 0, lohSeg = 0, pohSeg = 0;

        ulong[] fillPointers = [0x1001, 0x2002, 0x3003, 0x4004, 0x5005, 0x6006, 0x7007];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc =>
            {
                gc.GCIdentifiers = "workstation,regions";
                gc.AllocAllocated = allocAllocated;
                gc.FillPointers = fillPointers;
                gc.ConfigureMemory = b =>
                {
                    Layout<MockHeapSegment> segLayout = b.GetHeapSegmentLayout(includeHeapField: false);
                    // gen2 list: gen2 seg -> frozen (readonly) seg
                    frozenSeg = b.AddHeapSegment(segLayout, mem: 0x6000_0000, allocated: 0x6000_1000, next: 0, flags: 1, name: "FrozenSeg").Address;
                    gen2Seg = b.AddHeapSegment(segLayout, mem: 0x2000_0000, allocated: 0x2000_1000, next: frozenSeg, name: "Gen2Seg").Address;
                    gen1Seg = b.AddHeapSegment(segLayout, mem: 0x3000_0000, allocated: 0x3000_1000, next: 0, name: "Gen1Seg").Address;
                    gen0Seg = b.AddHeapSegment(segLayout, mem: 0x4000_0000, allocated: 0x4000_FFFF, next: 0, name: "Gen0Seg").Address;
                    lohSeg = b.AddHeapSegment(segLayout, mem: 0x5000_0000, allocated: 0x5000_1000, next: 0, name: "LohSeg").Address;
                    pohSeg = b.AddHeapSegment(segLayout, mem: 0x5100_0000, allocated: 0x5100_1000, next: 0, name: "PohSeg").Address;
                    // Ephemeral is the gen0 segment in regions mode -> end is overridden to alloc_allocated.
                    b.WritePointerGlobal(b.EphemeralHeapSegmentGlobalAddress, gen0Seg);
                };
                gc.GenerationsFactory = b => MakeGenerations(gen0Seg, 0, gen1Seg, 0, gen2Seg, lohSeg, pohSeg);
            })
            .Build();
        IGC gc = target.Contracts.GC;

        List<CapturedSegment> captured = new();
        foreach (GCHeapSegmentInfo seg in gc.EnumerateHeapSegments(gc.GetHeapData()))
            captured.Add(new CapturedSegment(seg.Start, seg.End, seg.Generation));

        // Raw per-heap walk: gen2 list (gen2Seg, frozenSeg), gen1 list (gen1Seg),
        // gen0 list (gen0Seg ephemeral - end overridden to allocAllocated), LOH, POH.
        Assert.Equal(6, captured.Count);
        Assert.Equal(new CapturedSegment(0x2000_0000, 0x2000_1000, GCSegmentClassification.Gen2), captured[0]);
        Assert.Equal(new CapturedSegment(0x6000_0000, 0x6000_1000, GCSegmentClassification.NonGC), captured[1]);
        Assert.Equal(new CapturedSegment(0x3000_0000, 0x3000_1000, GCSegmentClassification.Gen1), captured[2]);
        Assert.Equal(new CapturedSegment(0x4000_0000, allocAllocated, GCSegmentClassification.Gen0), captured[3]);
        Assert.Equal(new CapturedSegment(0x5000_0000, 0x5000_1000, GCSegmentClassification.LOH), captured[4]);
        Assert.Equal(new CapturedSegment(0x5100_0000, 0x5100_1000, GCSegmentClassification.POH), captured[5]);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateHeapSegments_Wks_Segments(MockTarget.Architecture arch)
    {
        // In segments mode, the gen2 list contains all SOH segments. The ephemeral segment is
        // surfaced as a single Ephemeral-tagged entry; the caller is responsible for splitting it.
        ulong gen1Start = 0x2000_4000;
        ulong gen0Start = 0x2000_6000;
        ulong allocAllocated = 0x2000_7000;

        ulong gen2OnlySeg = 0, ephSeg = 0, lohSeg = 0, pohSeg = 0;
        ulong[] fillPointers = [0x1001, 0x2002, 0x3003, 0x4004, 0x5005, 0x6006, 0x7007];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapWks(gc =>
            {
                gc.GCIdentifiers = "workstation,segments";
                gc.AllocAllocated = allocAllocated;
                gc.FillPointers = fillPointers;
                gc.ConfigureMemory = b =>
                {
                    Layout<MockHeapSegment> segLayout = b.GetHeapSegmentLayout(includeHeapField: false);
                    // Allocated on the ephemeral seg is ignored - end is alloc_allocated.
                    ephSeg = b.AddHeapSegment(segLayout, mem: 0x2000_0000, allocated: allocAllocated, next: 0, name: "EphSeg").Address;
                    gen2OnlySeg = b.AddHeapSegment(segLayout, mem: 0x1000_0000, allocated: 0x1000_1000, next: ephSeg, name: "Gen2Only").Address;
                    lohSeg = b.AddHeapSegment(segLayout, mem: 0x5000_0000, allocated: 0x5000_1000, next: 0, name: "LohSeg").Address;
                    pohSeg = b.AddHeapSegment(segLayout, mem: 0x5100_0000, allocated: 0x5100_1000, next: 0, name: "PohSeg").Address;
                    b.WritePointerGlobal(b.EphemeralHeapSegmentGlobalAddress, ephSeg);
                };
                // In segments mode, gen0/gen1 segments are not used; gen2 starts at the head of the SOH list.
                gc.GenerationsFactory = b => new MockGCBuilder.Generation[]
                {
                    new() { StartSegment = 0, AllocationStart = gen0Start, AllocContextPointer = 0, AllocContextLimit = 0 },
                    new() { StartSegment = 0, AllocationStart = gen1Start, AllocContextPointer = 0, AllocContextLimit = 0 },
                    new() { StartSegment = gen2OnlySeg, AllocationStart = 0, AllocContextPointer = 0, AllocContextLimit = 0 },
                    new() { StartSegment = lohSeg, AllocationStart = 0, AllocContextPointer = 0, AllocContextLimit = 0 },
                    new() { StartSegment = pohSeg, AllocationStart = 0, AllocContextPointer = 0, AllocContextLimit = 0 },
                };
            })
            .Build();
        IGC gc = target.Contracts.GC;

        List<CapturedSegment> captured = new();
        foreach (GCHeapSegmentInfo seg in gc.EnumerateHeapSegments(gc.GetHeapData()))
            captured.Add(new CapturedSegment(seg.Start, seg.End, seg.Generation));

        // Raw per-heap walk in segments mode: gen2 list (gen2OnlySeg as Gen2, ephSeg
        // tagged Ephemeral as the marker), then LOH, POH. No synthetic Gen0 and no
        // Gen1/Gen2 split - those are the caller's responsibility.
        Assert.Equal(4, captured.Count);
        Assert.Equal(new CapturedSegment(0x1000_0000, 0x1000_1000, GCSegmentClassification.Gen2), captured[0]);
        Assert.Equal(new CapturedSegment(0x2000_0000, allocAllocated, GCSegmentClassification.Ephemeral), captured[1]);
        Assert.Equal(new CapturedSegment(0x5000_0000, 0x5000_1000, GCSegmentClassification.LOH), captured[2]);
        Assert.Equal(new CapturedSegment(0x5100_0000, 0x5100_1000, GCSegmentClassification.POH), captured[3]);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateHeapSegments_Svr_Regions(MockTarget.Architecture arch)
    {
        ulong allocAllocated = 0x4000_0500;
        ulong gen2Seg = 0, gen1Seg = 0, gen0Seg = 0, lohSeg = 0, pohSeg = 0;
        ulong[] fillPointers = [0x1001, 0x2002, 0x3003, 0x4004, 0x5005, 0x6006, 0x7007];

        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddGCHeapSvr(gc =>
            {
                gc.GCIdentifiers = "server,regions";
                gc.AllocAllocated = allocAllocated;
                gc.FillPointers = fillPointers;
                gc.ConfigureMemory = b =>
                {
                    Layout<MockHeapSegment> segLayout = b.GetHeapSegmentLayout(includeHeapField: false);
                    gen2Seg = b.AddHeapSegment(segLayout, mem: 0x2000_0000, allocated: 0x2000_1000, next: 0, name: "SvrGen2Seg").Address;
                    gen1Seg = b.AddHeapSegment(segLayout, mem: 0x3000_0000, allocated: 0x3000_1000, next: 0, name: "SvrGen1Seg").Address;
                    gen0Seg = b.AddHeapSegment(segLayout, mem: 0x4000_0000, allocated: 0x4000_FFFF, next: 0, name: "SvrGen0Seg").Address;
                    lohSeg = b.AddHeapSegment(segLayout, mem: 0x5000_0000, allocated: 0x5000_1000, next: 0, name: "SvrLohSeg").Address;
                    pohSeg = b.AddHeapSegment(segLayout, mem: 0x5100_0000, allocated: 0x5100_1000, next: 0, name: "SvrPohSeg").Address;
                };
                gc.EphemeralHeapSegment = 0; // set after segments allocated; use post-config
                gc.GenerationsFactory = b => MakeGenerations(gen0Seg, 0, gen1Seg, 0, gen2Seg, lohSeg, pohSeg);
            }, out _)
            .Build();

        // EphemeralHeapSegment is on the SVR per-heap struct; set it explicitly via the heap pointer.
        // For simplicity here we leave it null so the gen0 segment is NOT considered ephemeral and its
        // Allocated is emitted as-is.
        IGC gc = target.Contracts.GC;

        List<CapturedSegment> captured = new();
        foreach (TargetPointer heapAddr in gc.GetGCHeaps())
        {
            foreach (GCHeapSegmentInfo seg in gc.EnumerateHeapSegments(gc.GetHeapData(heapAddr)))
                captured.Add(new CapturedSegment(seg.Start, seg.End, seg.Generation));
        }

        // Raw regions-mode walk: gen2, gen1, gen0 (non-ephemeral so end=allocated), LOH, POH.
        Assert.Equal(5, captured.Count);
        Assert.Equal(new CapturedSegment(0x2000_0000, 0x2000_1000, GCSegmentClassification.Gen2), captured[0]);
        Assert.Equal(new CapturedSegment(0x3000_0000, 0x3000_1000, GCSegmentClassification.Gen1), captured[1]);
        Assert.Equal(new CapturedSegment(0x4000_0000, 0x4000_FFFF, GCSegmentClassification.Gen0), captured[2]);
        Assert.Equal(new CapturedSegment(0x5000_0000, 0x5000_1000, GCSegmentClassification.LOH), captured[3]);
        Assert.Equal(new CapturedSegment(0x5100_0000, 0x5100_1000, GCSegmentClassification.POH), captured[4]);
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
    public string? GCIdentifiers { get; set; }
    public ulong EphemeralHeapSegment { get; set; }
    public ulong AllocAllocated { get; set; }
    public Action<MockGCBuilder>? ConfigureMemory { get; set; }
    public Func<MockGCBuilder, MockGCBuilder.Generation[]>? GenerationsFactory { get; set; }
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
        targetBuilder.AddContract<IGC>(version: "c1");
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
        targetBuilder.AddContract<IGC>(version: "c1");
        return targetBuilder;
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockGCBuilder gcBuilder)
        => new()
        {
            [DataType.GCAllocContext] = TargetTestHelpers.CreateTypeInfo(gcBuilder.GCAllocContextLayout),
            [DataType.Generation] = TargetTestHelpers.CreateTypeInfo(gcBuilder.GenerationLayout),
            [DataType.CFinalize] = TargetTestHelpers.CreateTypeInfo(gcBuilder.CFinalizeLayout),
            [DataType.OomHistory] = TargetTestHelpers.CreateTypeInfo(gcBuilder.OomHistoryLayout),
            [DataType.HeapSegment] = TargetTestHelpers.CreateTypeInfo(gcBuilder.GetHeapSegmentLayout(includeHeapField: false)),
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

        config.ConfigureMemory?.Invoke(gcBuilder);

        MockGCBuilder.Generation[] generations = config.GenerationsFactory?.Invoke(gcBuilder) ?? config.Generations;
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
        if (config.EphemeralHeapSegment != 0)
            gcBuilder.WritePointerGlobal(gcBuilder.EphemeralHeapSegmentGlobalAddress, config.EphemeralHeapSegment);
        if (config.AllocAllocated != 0)
            gcBuilder.WritePointerGlobal(gcBuilder.AllocAllocatedGlobalAddress, config.AllocAllocated);

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
            (nameof(Constants.Globals.GCIdentifiers), config.GCIdentifiers ?? "workstation,segments"));
    }

    private static ulong BuildSvrHeap(TestPlaceholderTarget.Builder targetBuilder, GCHeapBuilder config)
    {
        MockMemorySpace.Builder memBuilder = targetBuilder.MemoryBuilder;
        MockGCBuilder gcBuilder = new(memBuilder);

        config.ConfigureMemory?.Invoke(gcBuilder);

        MockGCBuilder.Generation[] generations = config.GenerationsFactory?.Invoke(gcBuilder) ?? config.Generations;
        uint generationCount = (uint)generations.Length;
        ulong[] fillPointers = config.FillPointers;
        uint fillPointersLength = (uint)fillPointers.Length;

        Dictionary<DataType, Target.TypeInfo> types = CreateServerContractTypes(gcBuilder, generationCount);
        MockCFinalize cFinalize = gcBuilder.AddCFinalize(fillPointers, "CFinalize_SVR");
        MockGCHeapSVR gcHeap = gcBuilder.AddGCHeapSVR(generations, cFinalize.Address);
        if (config.EphemeralHeapSegment != 0)
            gcHeap.EphemeralHeapSegment = config.EphemeralHeapSegment;
        if (config.AllocAllocated != 0)
            gcHeap.AllocAllocated = config.AllocAllocated;
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
            (nameof(Constants.Globals.GCIdentifiers), config.GCIdentifiers ?? "server,segments"));

        return gcHeap.Address;
    }
}
