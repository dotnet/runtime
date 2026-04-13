// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal static class MockGCFieldNames
{
    internal const string GCAllocContextPointer = "Pointer";
    internal const string GCAllocContextLimit = "Limit";
    internal const string GCAllocContextAllocBytes = "AllocBytes";
    internal const string GCAllocContextAllocBytesLoh = "AllocBytesLoh";

    internal const string GenerationAllocationContext = "AllocationContext";
    internal const string GenerationStartSegment = "StartSegment";
    internal const string GenerationAllocationStart = "AllocationStart";

    internal const string CFinalizeFillPointers = "FillPointers";

    internal const string OomHistoryReason = "Reason";
    internal const string OomHistoryAllocSize = "AllocSize";
    internal const string OomHistoryReserved = "Reserved";
    internal const string OomHistoryAllocated = "Allocated";
    internal const string OomHistoryGcIndex = "GcIndex";
    internal const string OomHistoryFgm = "Fgm";
    internal const string OomHistorySize = "Size";
    internal const string OomHistoryAvailablePagefileMb = "AvailablePagefileMb";
    internal const string OomHistoryLohP = "LohP";

    internal const string GCHeapSvrMarkArray = "MarkArray";
    internal const string GCHeapSvrNextSweepObj = "NextSweepObj";
    internal const string GCHeapSvrBackgroundMinSavedAddr = "BackgroundMinSavedAddr";
    internal const string GCHeapSvrBackgroundMaxSavedAddr = "BackgroundMaxSavedAddr";
    internal const string GCHeapSvrAllocAllocated = "AllocAllocated";
    internal const string GCHeapSvrEphemeralHeapSegment = "EphemeralHeapSegment";
    internal const string GCHeapSvrCardTable = "CardTable";
    internal const string GCHeapSvrFinalizeQueue = "FinalizeQueue";
    internal const string GCHeapSvrGenerationTable = "GenerationTable";
    internal const string GCHeapSvrOomData = "OomData";
    internal const string GCHeapSvrInternalRootArray = "InternalRootArray";
    internal const string GCHeapSvrInternalRootArrayIndex = "InternalRootArrayIndex";
    internal const string GCHeapSvrHeapAnalyzeSuccess = "HeapAnalyzeSuccess";
    internal const string GCHeapSvrInterestingData = "InterestingData";
    internal const string GCHeapSvrCompactReasons = "CompactReasons";
    internal const string GCHeapSvrExpandMechanisms = "ExpandMechanisms";
    internal const string GCHeapSvrInterestingMechanismBits = "InterestingMechanismBits";
}

internal sealed class MockGeneration : TypedView
{
    private const string AllocationContextFieldName = MockGCFieldNames.GenerationAllocationContext;
    private const string StartSegmentFieldName = MockGCFieldNames.GenerationStartSegment;
    private const string AllocationStartFieldName = MockGCFieldNames.GenerationAllocationStart;

    public static Layout<MockGeneration> CreateLayout(
        MockTarget.Architecture architecture,
        Layout<MockGCAllocContext> gcAllocContextLayout)
        => new SequentialLayoutBuilder("Generation", architecture)
            .AddField(AllocationContextFieldName, gcAllocContextLayout.Size)
            .AddPointerField(StartSegmentFieldName)
            .AddPointerField(AllocationStartFieldName)
            .Build<MockGeneration>();

    public MockGCAllocContext GetAllocationContext(Layout<MockGCAllocContext> gcAllocContextLayout)
    {
        LayoutField field = Layout.GetField(AllocationContextFieldName);
        return gcAllocContextLayout.Create(
            Memory.Slice(field.Offset, gcAllocContextLayout.Size),
            GetFieldAddress(AllocationContextFieldName));
    }

    public ulong StartSegment
    {
        get => ReadPointerField(StartSegmentFieldName);
        set => WritePointerField(StartSegmentFieldName, value);
    }

    public ulong AllocationStart
    {
        get => ReadPointerField(AllocationStartFieldName);
        set => WritePointerField(AllocationStartFieldName, value);
    }
}

internal sealed class MockCFinalize : TypedView
{
    private const string FillPointersFieldName = MockGCFieldNames.CFinalizeFillPointers;

    public static Layout<MockCFinalize> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("CFinalize", architecture)
            .AddPointerField(FillPointersFieldName)
            .Build<MockCFinalize>();

    public ulong this[int index]
    {
        get => ReadPointer(GetFillPointerSlice(index));
        set => WritePointer(GetFillPointerSlice(index), value);
    }

    private Span<byte> GetFillPointerSlice(int index)
    {
        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int offset = Layout.GetField(FillPointersFieldName).Offset + (index * pointerSize);
        return Memory.Span.Slice(offset, pointerSize);
    }
}

internal sealed class MockOomHistory : TypedView
{
    private const string ReasonFieldName = MockGCFieldNames.OomHistoryReason;
    private const string AllocSizeFieldName = MockGCFieldNames.OomHistoryAllocSize;
    private const string ReservedFieldName = MockGCFieldNames.OomHistoryReserved;
    private const string AllocatedFieldName = MockGCFieldNames.OomHistoryAllocated;
    private const string GcIndexFieldName = MockGCFieldNames.OomHistoryGcIndex;
    private const string FgmFieldName = MockGCFieldNames.OomHistoryFgm;
    private const string SizeFieldName = MockGCFieldNames.OomHistorySize;
    private const string AvailablePagefileMbFieldName = MockGCFieldNames.OomHistoryAvailablePagefileMb;
    private const string LohPFieldName = MockGCFieldNames.OomHistoryLohP;

    public static Layout<MockOomHistory> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("OomHistory", architecture)
            .AddField(ReasonFieldName, sizeof(int))
            .AddNUIntField(AllocSizeFieldName)
            .AddPointerField(ReservedFieldName)
            .AddPointerField(AllocatedFieldName)
            .AddNUIntField(GcIndexFieldName)
            .AddField(FgmFieldName, sizeof(int))
            .AddNUIntField(SizeFieldName)
            .AddNUIntField(AvailablePagefileMbFieldName)
            .AddUInt32Field(LohPFieldName)
            .Build<MockOomHistory>();
}

internal sealed class MockGCHeapSVR : TypedView
{
    private const int InterestingDataCount = 9;
    private const int CompactReasonsCount = 11;
    private const int ExpandMechanismsCount = 6;
    private const int InterestingMechanismBitsCount = 2;

    private const string MarkArrayFieldName = MockGCFieldNames.GCHeapSvrMarkArray;
    private const string NextSweepObjFieldName = MockGCFieldNames.GCHeapSvrNextSweepObj;
    private const string BackgroundMinSavedAddrFieldName = MockGCFieldNames.GCHeapSvrBackgroundMinSavedAddr;
    private const string BackgroundMaxSavedAddrFieldName = MockGCFieldNames.GCHeapSvrBackgroundMaxSavedAddr;
    private const string AllocAllocatedFieldName = MockGCFieldNames.GCHeapSvrAllocAllocated;
    private const string EphemeralHeapSegmentFieldName = MockGCFieldNames.GCHeapSvrEphemeralHeapSegment;
    private const string CardTableFieldName = MockGCFieldNames.GCHeapSvrCardTable;
    private const string FinalizeQueueFieldName = MockGCFieldNames.GCHeapSvrFinalizeQueue;
    private const string GenerationTableFieldName = MockGCFieldNames.GCHeapSvrGenerationTable;
    private const string OomDataFieldName = MockGCFieldNames.GCHeapSvrOomData;
    private const string InternalRootArrayFieldName = MockGCFieldNames.GCHeapSvrInternalRootArray;
    private const string InternalRootArrayIndexFieldName = MockGCFieldNames.GCHeapSvrInternalRootArrayIndex;
    private const string HeapAnalyzeSuccessFieldName = MockGCFieldNames.GCHeapSvrHeapAnalyzeSuccess;
    private const string InterestingDataFieldName = MockGCFieldNames.GCHeapSvrInterestingData;
    private const string CompactReasonsFieldName = MockGCFieldNames.GCHeapSvrCompactReasons;
    private const string ExpandMechanismsFieldName = MockGCFieldNames.GCHeapSvrExpandMechanisms;
    private const string InterestingMechanismBitsFieldName = MockGCFieldNames.GCHeapSvrInterestingMechanismBits;

    public static Layout<MockGCHeapSVR> CreateLayout(
        MockTarget.Architecture architecture,
        Layout<MockGeneration> generationLayout,
        Layout<MockOomHistory> oomHistoryLayout,
        uint generationCount)
        => new SequentialLayoutBuilder("GCHeapSVR", architecture)
            .AddPointerField(MarkArrayFieldName)
            .AddPointerField(NextSweepObjFieldName)
            .AddPointerField(BackgroundMinSavedAddrFieldName)
            .AddPointerField(BackgroundMaxSavedAddrFieldName)
            .AddPointerField(AllocAllocatedFieldName)
            .AddPointerField(EphemeralHeapSegmentFieldName)
            .AddPointerField(CardTableFieldName)
            .AddPointerField(FinalizeQueueFieldName)
            .AddField(GenerationTableFieldName, checked((int)(generationLayout.Size * generationCount)))
            .AddField(OomDataFieldName, oomHistoryLayout.Size)
            .AddPointerField(InternalRootArrayFieldName)
            .AddPointerField(InternalRootArrayIndexFieldName)
            .AddField(HeapAnalyzeSuccessFieldName, sizeof(int))
            .AddField(InterestingDataFieldName, InterestingDataCount * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint)))
            .AddField(CompactReasonsFieldName, CompactReasonsCount * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint)))
            .AddField(ExpandMechanismsFieldName, ExpandMechanismsCount * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint)))
            .AddField(InterestingMechanismBitsFieldName, InterestingMechanismBitsCount * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint)))
            .Build<MockGCHeapSVR>();

    public ulong FinalizeQueue
    {
        get => ReadPointerField(FinalizeQueueFieldName);
        set => WritePointerField(FinalizeQueueFieldName, value);
    }

    public MockGeneration GetGeneration(Layout<MockGeneration> generationLayout, int index)
    {
        LayoutField generationTableField = Layout.GetField(GenerationTableFieldName);
        int offset = checked(generationTableField.Offset + (index * generationLayout.Size));
        return generationLayout.Create(Memory.Slice(offset, generationLayout.Size), Address + (ulong)offset);
    }
}

internal sealed class MockGCBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0010_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0020_0000;

    internal readonly MockMemorySpace.Builder Builder;
    internal MockMemorySpace.BumpAllocator GCAllocator { get; }

    internal Layout<MockGCAllocContext> GCAllocContextLayout { get; }
    internal Layout<MockGeneration> GenerationLayout { get; }
    internal Layout<MockCFinalize> CFinalizeLayout { get; }
    internal Layout<MockOomHistory> OomHistoryLayout { get; }
    internal ulong MarkArrayGlobalAddress { get; }
    internal ulong NextSweepObjGlobalAddress { get; }
    internal ulong BackgroundMinSavedAddrGlobalAddress { get; }
    internal ulong BackgroundMaxSavedAddrGlobalAddress { get; }
    internal ulong AllocAllocatedGlobalAddress { get; }
    internal ulong EphemeralHeapSegmentGlobalAddress { get; }
    internal ulong CardTableGlobalAddress { get; }
    internal ulong FinalizeQueueGlobalAddress { get; }
    internal ulong InternalRootArrayGlobalAddress { get; }
    internal ulong InternalRootArrayIndexGlobalAddress { get; }
    internal ulong HeapAnalyzeSuccessGlobalAddress { get; }
    internal ulong InterestingDataAddress { get; }
    internal ulong CompactReasonsAddress { get; }
    internal ulong ExpandMechanismsAddress { get; }
    internal ulong InterestingMechanismBitsAddress { get; }
    internal ulong LowestAddressGlobalAddress { get; }
    internal ulong HighestAddressGlobalAddress { get; }
    internal ulong StructureInvalidCountGlobalAddress { get; }
    internal ulong MaxGenerationGlobalAddress { get; }
    internal ulong NumHeapsGlobalAddress { get; }
    internal ulong HeapsGlobalAddress { get; }

    public record struct Generation
    {
        public ulong StartSegment;
        public ulong AllocationStart;
        public ulong AllocContextPointer;
        public ulong AllocContextLimit;
    }

    public MockGCBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockGCBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
        GCAllocator = builder.CreateAllocator(allocationRange.Start, allocationRange.End);

        GCAllocContextLayout = MockGCAllocContext.CreateLayout(builder.TargetTestHelpers.Arch);
        GenerationLayout = MockGeneration.CreateLayout(builder.TargetTestHelpers.Arch, GCAllocContextLayout);
        CFinalizeLayout = MockCFinalize.CreateLayout(builder.TargetTestHelpers.Arch);
        OomHistoryLayout = MockOomHistory.CreateLayout(builder.TargetTestHelpers.Arch);

        MarkArrayGlobalAddress = AddPointerGlobal(0, "MarkArray");
        NextSweepObjGlobalAddress = AddPointerGlobal(0, "NextSweepObj");
        BackgroundMinSavedAddrGlobalAddress = AddPointerGlobal(0, "BgMinSavedAddr");
        BackgroundMaxSavedAddrGlobalAddress = AddPointerGlobal(0, "BgMaxSavedAddr");
        AllocAllocatedGlobalAddress = AddPointerGlobal(0, "AllocAllocated");
        EphemeralHeapSegmentGlobalAddress = AddPointerGlobal(0, "EphemeralHeapSegment");
        CardTableGlobalAddress = AddPointerGlobal(0, "CardTable");
        FinalizeQueueGlobalAddress = AddPointerGlobal(0, "FinalizeQueue");
        InternalRootArrayGlobalAddress = AddPointerGlobal(0, "InternalRootArray");
        InternalRootArrayIndexGlobalAddress = AddPointerGlobal(0, "InternalRootArrayIndex");
        HeapAnalyzeSuccessGlobalAddress = AddInt32Global(0, "HeapAnalyzeSuccess");
        InterestingDataAddress = AddDataRegion((ulong)Builder.TargetTestHelpers.PointerSize, "InterestingDataArray");
        CompactReasonsAddress = AddDataRegion((ulong)Builder.TargetTestHelpers.PointerSize, "CompactReasonsArray");
        ExpandMechanismsAddress = AddDataRegion((ulong)Builder.TargetTestHelpers.PointerSize, "ExpandMechanismsArray");
        InterestingMechanismBitsAddress = AddDataRegion((ulong)Builder.TargetTestHelpers.PointerSize, "InterestingMechanismBitsArray");
        LowestAddressGlobalAddress = AddPointerGlobal(0x1000, "LowestAddress");
        HighestAddressGlobalAddress = AddPointerGlobal(0, "HighestAddress");
        StructureInvalidCountGlobalAddress = AddInt32Global(0, "StructureInvalidCount");
        MaxGenerationGlobalAddress = AddUInt32Global(0, "MaxGeneration");
        NumHeapsGlobalAddress = AddUInt32Global(1, "NumHeaps");
        HeapsGlobalAddress = AddPointerGlobal(0, "Heaps");
    }

    internal ulong AddGenerationTable(Generation[] generations)
    {
        ArgumentNullException.ThrowIfNull(generations);

        int generationSize = GenerationLayout.Size;
        MockMemorySpace.HeapFragment generationTable = GCAllocator.Allocate(
            (ulong)(generationSize * generations.Length),
            "GenerationTable");

        for (int i = 0; i < generations.Length; i++)
        {
            int offset = checked(i * generationSize);
            MockGeneration generation = GenerationLayout.Create(
                generationTable.Data.AsMemory(offset, generationSize),
                generationTable.Address + (ulong)offset);
            MockGCAllocContext allocationContext = generation.GetAllocationContext(GCAllocContextLayout);
            allocationContext.Pointer = generations[i].AllocContextPointer;
            allocationContext.Limit = generations[i].AllocContextLimit;
            generation.StartSegment = generations[i].StartSegment;
            generation.AllocationStart = generations[i].AllocationStart;
        }

        Builder.AddHeapFragment(generationTable);
        return generationTable.Address;
    }

    internal MockCFinalize AddCFinalize(ulong[] fillPointers, string name = "CFinalize")
    {
        ArgumentNullException.ThrowIfNull(fillPointers);

        int fillPointersOffset = CFinalizeLayout.GetField(MockGCFieldNames.CFinalizeFillPointers).Offset;
        ulong size = (ulong)(fillPointersOffset + (Builder.TargetTestHelpers.PointerSize * fillPointers.Length));
        MockCFinalize cFinalize = Add(CFinalizeLayout, size, name);
        for (int i = 0; i < fillPointers.Length; i++)
        {
            cFinalize[i] = fillPointers[i];
        }

        return cFinalize;
    }

    internal MockOomHistory AddOomHistory(string name = "OomHistory")
        => Add(OomHistoryLayout, name);

    internal Layout<MockGCHeapSVR> GetGCHeapSVRLayout(uint generationCount)
        => MockGCHeapSVR.CreateLayout(Builder.TargetTestHelpers.Arch, GenerationLayout, OomHistoryLayout, generationCount);

    internal MockGCHeapSVR AddGCHeapSVR(
        Generation[] generations,
        ulong finalizeQueueAddress,
        string name = "GCHeap_SVR")
    {
        ArgumentNullException.ThrowIfNull(generations);

        Layout<MockGCHeapSVR> layout = GetGCHeapSVRLayout((uint)generations.Length);
        MockGCHeapSVR gcHeap = Add(layout, name);
        gcHeap.FinalizeQueue = finalizeQueueAddress;

        for (int i = 0; i < generations.Length; i++)
        {
            MockGeneration generation = gcHeap.GetGeneration(GenerationLayout, i);
            MockGCAllocContext allocationContext = generation.GetAllocationContext(GCAllocContextLayout);
            allocationContext.Pointer = generations[i].AllocContextPointer;
            allocationContext.Limit = generations[i].AllocContextLimit;
            generation.StartSegment = generations[i].StartSegment;
            generation.AllocationStart = generations[i].AllocationStart;
        }

        return gcHeap;
    }

    internal ulong AddPointerGlobal(ulong pointsTo, string name)
    {
        MockMemorySpace.HeapFragment fragment = GCAllocator.Allocate(
            (ulong)Builder.TargetTestHelpers.PointerSize,
            $"[global pointer] {name}");
        Builder.TargetTestHelpers.WritePointer(fragment.Data, pointsTo);
        Builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    internal ulong AddInt32Global(int value, string name)
    {
        MockMemorySpace.HeapFragment fragment = GCAllocator.Allocate(
            (ulong)Builder.TargetTestHelpers.PointerSize,
            $"[{name}]");
        Builder.TargetTestHelpers.Write(fragment.Data.AsSpan(0, sizeof(int)), value);
        Builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    internal ulong AddUInt32Global(uint value, string name)
    {
        MockMemorySpace.HeapFragment fragment = GCAllocator.Allocate(
            (ulong)Builder.TargetTestHelpers.PointerSize,
            $"[{name}]");
        Builder.TargetTestHelpers.Write(fragment.Data.AsSpan(0, sizeof(uint)), value);
        Builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    internal void WritePointerGlobal(ulong globalAddress, ulong value)
    {
        Span<byte> globalBytes = Builder.BorrowAddressRange(globalAddress, Builder.TargetTestHelpers.PointerSize);
        Builder.TargetTestHelpers.WritePointer(globalBytes, value);
    }

    internal void WriteInt32Global(ulong globalAddress, int value)
    {
        Span<byte> globalBytes = Builder.BorrowAddressRange(globalAddress, sizeof(int));
        Builder.TargetTestHelpers.Write(globalBytes, value);
    }

    internal void WriteUInt32Global(ulong globalAddress, uint value)
    {
        Span<byte> globalBytes = Builder.BorrowAddressRange(globalAddress, sizeof(uint));
        Builder.TargetTestHelpers.Write(globalBytes, value);
    }

    private ulong AddDataRegion(ulong size, string name)
    {
        MockMemorySpace.HeapFragment fragment = GCAllocator.Allocate(size, name);
        Builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    private TView Add<TView>(Layout<TView> layout, string name)
        where TView : TypedView, new()
        => Add(layout, (ulong)layout.Size, name);

    private TView Add<TView>(Layout<TView> layout, ulong size, string name)
        where TView : TypedView, new()
    {
        MockMemorySpace.HeapFragment fragment = GCAllocator.Allocate(size, name);
        Builder.AddHeapFragment(fragment);
        return layout.Create(fragment.Data.AsMemory(), fragment.Address);
    }
}
