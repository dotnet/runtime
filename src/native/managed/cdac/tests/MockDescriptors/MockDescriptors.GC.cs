// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Configuration object for GC heap mock data, used with
/// <see cref="GCHeapBuilderExtensions.AddGCHeapWks"/> and
/// <see cref="GCHeapBuilderExtensions.AddGCHeapSvr"/>.
/// </summary>
internal class GCHeapBuilder
{
    private GCHeapBuilder.GenerationInput[]? _generations;
    private ulong[]? _fillPointers;

    public GCHeapBuilder SetGenerations(params GenerationInput[] generations)
    {
        _generations = generations;
        return this;
    }

    public GCHeapBuilder SetFillPointers(params ulong[] fillPointers)
    {
        _fillPointers = fillPointers;
        return this;
    }

    internal GenerationInput[] GetGenerationsOrDefault(uint defaultCount) =>
        _generations ?? new GenerationInput[defaultCount];

    internal ulong[] GetFillPointersOrDefault(uint generationCount) =>
        _fillPointers ?? [];

    public record struct GenerationInput
    {
        public ulong StartSegment;
        public ulong AllocationStart;
        public ulong AllocContextPointer;
        public ulong AllocContextLimit;
    }
}

internal static class GCHeapBuilderExtensions
{
    private const ulong DefaultAllocationRangeStart = 0x0010_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0020_0000;
    private const uint DefaultGenerationCount = 4;

    public static TestPlaceholderTarget.Builder AddGCHeapWks(
        this TestPlaceholderTarget.Builder targetBuilder,
        Action<GCHeapBuilder> configure)
    {
        var config = new GCHeapBuilder();
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
        var config = new GCHeapBuilder();
        configure(config);
        heapAddress = BuildSvrHeap(targetBuilder, config);
        targetBuilder.AddContract<IGC>(target =>
            ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1));
        return targetBuilder;
    }

    #region Type field definitions

    private static readonly MockDescriptors.TypeFields GCAllocContextFields = new()
    {
        DataType = DataType.GCAllocContext,
        Fields =
        [
            new(nameof(Data.GCAllocContext.Pointer), DataType.pointer),
            new(nameof(Data.GCAllocContext.Limit), DataType.pointer),
        ]
    };

    private static MockDescriptors.TypeFields GetGenerationFields(TargetTestHelpers helpers)
    {
        uint allocContextSize = MockDescriptors.GetTypesForTypeFields(helpers, [GCAllocContextFields])[DataType.GCAllocContext].Size!.Value;
        return new MockDescriptors.TypeFields()
        {
            DataType = DataType.Generation,
            Fields =
            [
                new(nameof(Data.Generation.AllocationContext), DataType.GCAllocContext, allocContextSize),
                new(nameof(Data.Generation.StartSegment), DataType.pointer),
                new(nameof(Data.Generation.AllocationStart), DataType.pointer),
            ]
        };
    }

    private static readonly MockDescriptors.TypeFields CFinalizeFields = new()
    {
        DataType = DataType.CFinalize,
        Fields =
        [
            new(nameof(Data.CFinalize.FillPointers), DataType.pointer),
        ]
    };

    private static readonly MockDescriptors.TypeFields OomHistoryFields = new()
    {
        DataType = DataType.OomHistory,
        Fields =
        [
            new(nameof(Data.OomHistory.Reason), DataType.int32),
            new(nameof(Data.OomHistory.AllocSize), DataType.nuint),
            new(nameof(Data.OomHistory.Reserved), DataType.pointer),
            new(nameof(Data.OomHistory.Allocated), DataType.pointer),
            new(nameof(Data.OomHistory.GcIndex), DataType.nuint),
            new(nameof(Data.OomHistory.Fgm), DataType.int32),
            new(nameof(Data.OomHistory.Size), DataType.nuint),
            new(nameof(Data.OomHistory.AvailablePagefileMb), DataType.nuint),
            new(nameof(Data.OomHistory.LohP), DataType.uint32),
        ]
    };

    #endregion

    #region Shared helpers

    private static Dictionary<DataType, Target.TypeInfo> GetBaseTypes(TargetTestHelpers helpers)
    {
        return MockDescriptors.GetTypesForTypeFields(helpers,
        [
            GCAllocContextFields,
            GetGenerationFields(helpers),
            CFinalizeFields,
            OomHistoryFields,
        ]);
    }

    private static Dictionary<DataType, Target.TypeInfo> GetSvrTypes(TargetTestHelpers helpers, uint totalGenerationCount)
    {
        var baseTypes = GetBaseTypes(helpers);

        uint genSize = baseTypes[DataType.Generation].Size!.Value;
        uint oomSize = baseTypes[DataType.OomHistory].Size!.Value;

        int ptrSize = helpers.PointerSize;
        int offset = 0;

        var fields = new Dictionary<string, Target.FieldInfo>();
        void AddPointerField(string name) { fields[name] = new Target.FieldInfo() { Offset = offset }; offset += ptrSize; }

        AddPointerField(nameof(Data.GCHeapSVR.MarkArray));
        AddPointerField(nameof(Data.GCHeapSVR.NextSweepObj));
        AddPointerField(nameof(Data.GCHeapSVR.BackgroundMinSavedAddr));
        AddPointerField(nameof(Data.GCHeapSVR.BackgroundMaxSavedAddr));
        AddPointerField(nameof(Data.GCHeapSVR.AllocAllocated));
        AddPointerField(nameof(Data.GCHeapSVR.EphemeralHeapSegment));
        AddPointerField(nameof(Data.GCHeapSVR.CardTable));
        AddPointerField(nameof(Data.GCHeapSVR.FinalizeQueue));

        fields[nameof(Data.GCHeapSVR.GenerationTable)] = new Target.FieldInfo() { Offset = offset };
        offset += (int)(genSize * totalGenerationCount);

        fields[nameof(Data.GCHeapSVR.OomData)] = new Target.FieldInfo() { Offset = offset };
        offset += (int)oomSize;

        AddPointerField(nameof(Data.GCHeapSVR.InternalRootArray));
        AddPointerField(nameof(Data.GCHeapSVR.InternalRootArrayIndex));

        fields[nameof(Data.GCHeapSVR.HeapAnalyzeSuccess)] = new Target.FieldInfo() { Offset = offset };
        offset += sizeof(int);
        offset = (offset + ptrSize - 1) & ~(ptrSize - 1);

        fields[nameof(Data.GCHeapSVR.InterestingData)] = new Target.FieldInfo() { Offset = offset };
        fields[nameof(Data.GCHeapSVR.CompactReasons)] = new Target.FieldInfo() { Offset = offset };
        fields[nameof(Data.GCHeapSVR.ExpandMechanisms)] = new Target.FieldInfo() { Offset = offset };
        fields[nameof(Data.GCHeapSVR.InterestingMechanismBits)] = new Target.FieldInfo() { Offset = offset };

        baseTypes[DataType.GCHeap] = new Target.TypeInfo()
        {
            Fields = fields,
            Size = (uint)offset,
        };

        return baseTypes;
    }

    private static void WriteGenerationData(
        TargetTestHelpers helpers,
        Span<byte> genSpan,
        Dictionary<DataType, Target.TypeInfo> types,
        GCHeapBuilder.GenerationInput generation)
    {
        Target.TypeInfo genTypeInfo = types[DataType.Generation];
        Target.TypeInfo allocCtxTypeInfo = types[DataType.GCAllocContext];
        int allocCtxOffset = genTypeInfo.Fields[nameof(Data.Generation.AllocationContext)].Offset;

        helpers.WritePointer(
            genSpan.Slice(allocCtxOffset + allocCtxTypeInfo.Fields[nameof(Data.GCAllocContext.Pointer)].Offset),
            generation.AllocContextPointer);
        helpers.WritePointer(
            genSpan.Slice(allocCtxOffset + allocCtxTypeInfo.Fields[nameof(Data.GCAllocContext.Limit)].Offset),
            generation.AllocContextLimit);
        helpers.WritePointer(
            genSpan.Slice(genTypeInfo.Fields[nameof(Data.Generation.StartSegment)].Offset),
            generation.StartSegment);
        helpers.WritePointer(
            genSpan.Slice(genTypeInfo.Fields[nameof(Data.Generation.AllocationStart)].Offset),
            generation.AllocationStart);
    }

    private static void WriteFillPointers(
        TargetTestHelpers helpers,
        Span<byte> span,
        ulong[] fillPointers)
    {
        for (int i = 0; i < fillPointers.Length; i++)
        {
            helpers.WritePointer(
                span.Slice(helpers.PointerSize * i),
                fillPointers[i]);
        }
    }

    private static MockMemorySpace.HeapFragment AllocatePointerGlobal(
        MockMemorySpace.BumpAllocator allocator,
        MockMemorySpace.Builder memBuilder,
        TargetTestHelpers helpers,
        ulong pointsTo,
        string name)
    {
        MockMemorySpace.HeapFragment fragment = allocator.Allocate((ulong)helpers.PointerSize, $"[global pointer] {name}");
        helpers.WritePointer(fragment.Data, pointsTo);
        memBuilder.AddHeapFragment(fragment);
        return fragment;
    }

    #endregion

    private static void BuildWksHeap(TestPlaceholderTarget.Builder targetBuilder, GCHeapBuilder config)
    {
        MockMemorySpace.Builder memBuilder = targetBuilder.MemoryBuilder;
        TargetTestHelpers helpers = memBuilder.TargetTestHelpers;
        MockMemorySpace.BumpAllocator allocator = memBuilder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);

        GCHeapBuilder.GenerationInput[] generations = config.GetGenerationsOrDefault(DefaultGenerationCount);
        uint genCount = (uint)generations.Length;
        ulong[] fillPointers = config.GetFillPointersOrDefault(genCount);
        uint fpLength = (uint)fillPointers.Length;

        var types = GetBaseTypes(helpers);
        Target.TypeInfo genTypeInfo = types[DataType.Generation];
        Target.TypeInfo cFinalizeTypeInfo = types[DataType.CFinalize];
        Target.TypeInfo oomTypeInfo = types[DataType.OomHistory];
        uint genSize = genTypeInfo.Size!.Value;

        // Allocate and populate generation table
        MockMemorySpace.HeapFragment generationTable = allocator.Allocate(genSize * genCount, "GenerationTable");
        for (int i = 0; i < generations.Length; i++)
        {
            WriteGenerationData(helpers,
                generationTable.Data.AsSpan().Slice((int)(i * genSize), (int)genSize),
                types, generations[i]);
        }
        memBuilder.AddHeapFragment(generationTable);

        // Allocate and populate CFinalize with embedded fill pointers array
        int fpFieldOffset = cFinalizeTypeInfo.Fields[nameof(Data.CFinalize.FillPointers)].Offset;
        ulong cFinalizeSize = (ulong)fpFieldOffset + (ulong)(helpers.PointerSize * (int)fpLength);
        MockMemorySpace.HeapFragment cFinalize = allocator.Allocate(cFinalizeSize, "CFinalize");
        WriteFillPointers(helpers, cFinalize.Data.AsSpan().Slice(fpFieldOffset), fillPointers);
        memBuilder.AddHeapFragment(cFinalize);

        // Allocate OomHistory (zero-initialized)
        MockMemorySpace.HeapFragment oomHistory = allocator.Allocate(oomTypeInfo.Size!.Value, "OomHistory");
        memBuilder.AddHeapFragment(oomHistory);

        // WKS global pointers (double-indirection)
        MockMemorySpace.HeapFragment markArrayGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "MarkArray");
        MockMemorySpace.HeapFragment nextSweepObjGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "NextSweepObj");
        MockMemorySpace.HeapFragment bgMinGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "BgMinSavedAddr");
        MockMemorySpace.HeapFragment bgMaxGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "BgMaxSavedAddr");
        MockMemorySpace.HeapFragment allocAllocatedGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "AllocAllocated");
        MockMemorySpace.HeapFragment ephSegGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "EphemeralHeapSegment");
        MockMemorySpace.HeapFragment cardTableGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "CardTable");
        MockMemorySpace.HeapFragment finalizeQueueGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, cFinalize.Address, "FinalizeQueue");

        MockMemorySpace.HeapFragment internalRootArrayGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "InternalRootArray");
        MockMemorySpace.HeapFragment internalRootArrayIndexGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "InternalRootArrayIndex");
        MockMemorySpace.HeapFragment heapAnalyzeSuccessGlobal = allocator.Allocate((ulong)helpers.PointerSize, "[HeapAnalyzeSuccess]");
        helpers.Write(heapAnalyzeSuccessGlobal.Data.AsSpan(0, sizeof(int)), 0);
        memBuilder.AddHeapFragment(heapAnalyzeSuccessGlobal);

        MockMemorySpace.HeapFragment interestingDataArray = allocator.Allocate((ulong)helpers.PointerSize, "InterestingDataArray");
        MockMemorySpace.HeapFragment compactReasonsArray = allocator.Allocate((ulong)helpers.PointerSize, "CompactReasonsArray");
        MockMemorySpace.HeapFragment expandMechanismsArray = allocator.Allocate((ulong)helpers.PointerSize, "ExpandMechanismsArray");
        MockMemorySpace.HeapFragment interestingMechBitsArray = allocator.Allocate((ulong)helpers.PointerSize, "InterestingMechBitsArray");
        memBuilder.AddHeapFragments([interestingDataArray, compactReasonsArray, expandMechanismsArray, interestingMechBitsArray]);

        MockMemorySpace.HeapFragment lowestAddrGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0x1000, "LowestAddress");
        MockMemorySpace.HeapFragment highestAddrGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0xFFFF_0000, "HighestAddress");
        MockMemorySpace.HeapFragment structInvalidCountGlobal = allocator.Allocate((ulong)helpers.PointerSize, "[StructureInvalidCount]");
        helpers.Write(structInvalidCountGlobal.Data.AsSpan(0, sizeof(int)), 0);
        memBuilder.AddHeapFragment(structInvalidCountGlobal);

        MockMemorySpace.HeapFragment maxGenGlobal = allocator.Allocate((ulong)helpers.PointerSize, "[MaxGeneration]");
        helpers.Write(maxGenGlobal.Data.AsSpan(0, sizeof(uint)), genCount - 1);
        memBuilder.AddHeapFragment(maxGenGlobal);

        targetBuilder.AddTypes(types);
        targetBuilder.AddGlobals(
            (nameof(Constants.Globals.TotalGenerationCount), genCount),
            (nameof(Constants.Globals.CFinalizeFillPointersLength), fpLength),
            (nameof(Constants.Globals.InterestingDataLength), 0UL),
            (nameof(Constants.Globals.CompactReasonsLength), 0UL),
            (nameof(Constants.Globals.ExpandMechanismsLength), 0UL),
            (nameof(Constants.Globals.InterestingMechanismBitsLength), 0UL),
            (nameof(Constants.Globals.GCHeapMarkArray), markArrayGlobal.Address),
            (nameof(Constants.Globals.GCHeapNextSweepObj), nextSweepObjGlobal.Address),
            (nameof(Constants.Globals.GCHeapBackgroundMinSavedAddr), bgMinGlobal.Address),
            (nameof(Constants.Globals.GCHeapBackgroundMaxSavedAddr), bgMaxGlobal.Address),
            (nameof(Constants.Globals.GCHeapAllocAllocated), allocAllocatedGlobal.Address),
            (nameof(Constants.Globals.GCHeapEphemeralHeapSegment), ephSegGlobal.Address),
            (nameof(Constants.Globals.GCHeapCardTable), cardTableGlobal.Address),
            (nameof(Constants.Globals.GCHeapFinalizeQueue), finalizeQueueGlobal.Address),
            (nameof(Constants.Globals.GCHeapGenerationTable), generationTable.Address),
            (nameof(Constants.Globals.GCHeapOomData), oomHistory.Address),
            (nameof(Constants.Globals.GCHeapInternalRootArray), internalRootArrayGlobal.Address),
            (nameof(Constants.Globals.GCHeapInternalRootArrayIndex), internalRootArrayIndexGlobal.Address),
            (nameof(Constants.Globals.GCHeapHeapAnalyzeSuccess), heapAnalyzeSuccessGlobal.Address),
            (nameof(Constants.Globals.GCHeapInterestingData), interestingDataArray.Address),
            (nameof(Constants.Globals.GCHeapCompactReasons), compactReasonsArray.Address),
            (nameof(Constants.Globals.GCHeapExpandMechanisms), expandMechanismsArray.Address),
            (nameof(Constants.Globals.GCHeapInterestingMechanismBits), interestingMechBitsArray.Address),
            (nameof(Constants.Globals.GCLowestAddress), lowestAddrGlobal.Address),
            (nameof(Constants.Globals.GCHighestAddress), highestAddrGlobal.Address),
            (nameof(Constants.Globals.StructureInvalidCount), structInvalidCountGlobal.Address),
            (nameof(Constants.Globals.MaxGeneration), maxGenGlobal.Address));
        targetBuilder.AddGlobalStrings(
            (nameof(Constants.Globals.GCIdentifiers), "workstation,segments"));
    }

    private static ulong BuildSvrHeap(TestPlaceholderTarget.Builder targetBuilder, GCHeapBuilder config)
    {
        MockMemorySpace.Builder memBuilder = targetBuilder.MemoryBuilder;
        TargetTestHelpers helpers = memBuilder.TargetTestHelpers;
        MockMemorySpace.BumpAllocator allocator = memBuilder.CreateAllocator(DefaultAllocationRangeStart, DefaultAllocationRangeEnd);

        GCHeapBuilder.GenerationInput[] generations = config.GetGenerationsOrDefault(DefaultGenerationCount);
        uint genCount = (uint)generations.Length;
        ulong[] fillPointers = config.GetFillPointersOrDefault(genCount);
        uint fpLength = (uint)fillPointers.Length;

        var types = GetSvrTypes(helpers, genCount);
        Target.TypeInfo gcHeapTypeInfo = types[DataType.GCHeap];
        Target.TypeInfo cFinalizeTypeInfo = types[DataType.CFinalize];
        uint genSize = types[DataType.Generation].Size!.Value;

        // Allocate and populate CFinalize with embedded fill pointers array
        int fpFieldOffset = cFinalizeTypeInfo.Fields[nameof(Data.CFinalize.FillPointers)].Offset;
        ulong cFinalizeSize = (ulong)fpFieldOffset + (ulong)(helpers.PointerSize * (int)fpLength);
        MockMemorySpace.HeapFragment cFinalize = allocator.Allocate(cFinalizeSize, "CFinalize_SVR");
        WriteFillPointers(helpers, cFinalize.Data.AsSpan().Slice(fpFieldOffset), fillPointers);
        memBuilder.AddHeapFragment(cFinalize);

        // Allocate the GCHeap struct, populate FinalizeQueue pointer and generation table
        uint heapSize = gcHeapTypeInfo.Size!.Value;
        MockMemorySpace.HeapFragment gcHeap = allocator.Allocate(heapSize, "GCHeap_SVR");
        helpers.WritePointer(
            gcHeap.Data.AsSpan().Slice(gcHeapTypeInfo.Fields[nameof(Data.GCHeapSVR.FinalizeQueue)].Offset),
            cFinalize.Address);
        int genTableOffset = gcHeapTypeInfo.Fields[nameof(Data.GCHeapSVR.GenerationTable)].Offset;
        for (int i = 0; i < generations.Length; i++)
        {
            WriteGenerationData(helpers,
                gcHeap.Data.AsSpan().Slice(genTableOffset + (int)(i * genSize), (int)genSize),
                types, generations[i]);
        }
        memBuilder.AddHeapFragment(gcHeap);

        // Heap table (array of pointers to heap structs)
        MockMemorySpace.HeapFragment heapTable = allocator.Allocate((ulong)helpers.PointerSize, "HeapTable");
        helpers.WritePointer(heapTable.Data, gcHeap.Address);
        memBuilder.AddHeapFragment(heapTable);

        // SVR globals
        MockMemorySpace.HeapFragment numHeapsGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0, "NumHeaps");
        helpers.Write(numHeapsGlobal.Data.AsSpan(0, sizeof(int)), 1);
        MockMemorySpace.HeapFragment heapsGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, heapTable.Address, "Heaps");

        MockMemorySpace.HeapFragment lowestAddrGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0x1000, "LowestAddress");
        MockMemorySpace.HeapFragment highestAddrGlobal = AllocatePointerGlobal(allocator, memBuilder, helpers, 0x7FFF_0000, "HighestAddress");
        MockMemorySpace.HeapFragment structInvalidCountGlobal = allocator.Allocate((ulong)helpers.PointerSize, "[StructureInvalidCount]");
        helpers.Write(structInvalidCountGlobal.Data.AsSpan(0, sizeof(int)), 0);
        memBuilder.AddHeapFragment(structInvalidCountGlobal);

        MockMemorySpace.HeapFragment maxGenGlobal = allocator.Allocate((ulong)helpers.PointerSize, "[MaxGeneration]");
        helpers.Write(maxGenGlobal.Data.AsSpan(0, sizeof(uint)), genCount - 1);
        memBuilder.AddHeapFragment(maxGenGlobal);

        targetBuilder.AddTypes(types);
        targetBuilder.AddGlobals(
            (nameof(Constants.Globals.TotalGenerationCount), genCount),
            (nameof(Constants.Globals.CFinalizeFillPointersLength), fpLength),
            (nameof(Constants.Globals.InterestingDataLength), 0UL),
            (nameof(Constants.Globals.CompactReasonsLength), 0UL),
            (nameof(Constants.Globals.ExpandMechanismsLength), 0UL),
            (nameof(Constants.Globals.InterestingMechanismBitsLength), 0UL),
            (nameof(Constants.Globals.NumHeaps), numHeapsGlobal.Address),
            (nameof(Constants.Globals.Heaps), heapsGlobal.Address),
            (nameof(Constants.Globals.GCLowestAddress), lowestAddrGlobal.Address),
            (nameof(Constants.Globals.GCHighestAddress), highestAddrGlobal.Address),
            (nameof(Constants.Globals.StructureInvalidCount), structInvalidCountGlobal.Address),
            (nameof(Constants.Globals.MaxGeneration), maxGenGlobal.Address));
        targetBuilder.AddGlobalStrings(
            (nameof(Constants.Globals.GCIdentifiers), "server,segments"));

        return gcHeap.Address;
    }
}
