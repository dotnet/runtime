// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class GCMemoryRegionTests
{
    private static Dictionary<DataType, Target.TypeInfo> BuildTypes(TargetTestHelpers helpers)
    {
        var types = new Dictionary<DataType, Target.TypeInfo>();

        var handleTableMapLayout = helpers.LayoutFields([
            new("Buckets", DataType.pointer),
            new("Next", DataType.pointer),
            new("MaxIndex", DataType.uint32),
        ]);
        types[DataType.HandleTableMap] = new Target.TypeInfo()
        {
            Fields = handleTableMapLayout.Fields,
            Size = handleTableMapLayout.Stride,
        };

        var handleTableBucketLayout = helpers.LayoutFields([
            new("Table", DataType.pointer),
            new("HandleTableIndex", DataType.uint32),
        ]);
        types[DataType.HandleTableBucket] = new Target.TypeInfo()
        {
            Fields = handleTableBucketLayout.Fields,
            Size = handleTableBucketLayout.Stride,
        };

        var handleTableLayout = helpers.LayoutFields([
            new("SegmentList", DataType.pointer),
        ]);
        types[DataType.HandleTable] = new Target.TypeInfo()
        {
            Fields = handleTableLayout.Fields,
            Size = handleTableLayout.Stride,
        };

        var handleTableSegmentLayout = helpers.LayoutFields([
            new("NextSegment", DataType.pointer),
        ]);
        types[DataType.HandleTableSegment] = new Target.TypeInfo()
        {
            Fields = handleTableSegmentLayout.Fields,
            Size = handleTableSegmentLayout.Stride,
        };

        var cardTableInfoLayout = helpers.LayoutFields([
            new("Recount", DataType.uint32),
            new("Size", DataType.nuint),
            new("NextCardTable", DataType.pointer),
        ]);
        types[DataType.CardTableInfo] = new Target.TypeInfo()
        {
            Fields = cardTableInfoLayout.Fields,
            Size = cardTableInfoLayout.Stride,
        };

        var regionFreeListLayout = helpers.LayoutFields([
            new("HeadFreeRegion", DataType.pointer),
        ]);
        types[DataType.RegionFreeList] = new Target.TypeInfo()
        {
            Fields = regionFreeListLayout.Fields,
            Size = regionFreeListLayout.Stride,
        };

        var heapSegmentLayout = helpers.LayoutFields([
            new("Allocated", DataType.pointer),
            new("Committed", DataType.pointer),
            new("Reserved", DataType.pointer),
            new("Used", DataType.pointer),
            new("Mem", DataType.pointer),
            new("Flags", DataType.nuint),
            new("Next", DataType.pointer),
            new("BackgroundAllocated", DataType.pointer),
        ]);
        types[DataType.HeapSegment] = new Target.TypeInfo()
        {
            Fields = heapSegmentLayout.Fields,
            Size = heapSegmentLayout.Stride,
        };

        var oomHistoryLayout = helpers.LayoutFields([
            new("Reason", DataType.int32),
            new("AllocSize", DataType.nuint),
            new("Reserved", DataType.pointer),
            new("Allocated", DataType.pointer),
            new("GcIndex", DataType.nuint),
            new("Fgm", DataType.int32),
            new("Size", DataType.nuint),
            new("AvailablePagefileMb", DataType.nuint),
            new("LohP", DataType.uint32),
        ]);
        types[DataType.OomHistory] = new Target.TypeInfo()
        {
            Fields = oomHistoryLayout.Fields,
            Size = oomHistoryLayout.Stride,
        };

        return types;
    }

    private static void WritePointerGlobal(
        TargetTestHelpers helpers,
        MockMemorySpace.BumpAllocator allocator,
        List<MockMemorySpace.HeapFragment> fragments,
        List<(string Name, ulong Value)> globals,
        string name,
        ulong pointerValue = 0)
    {
        var fragment = allocator.Allocate((ulong)helpers.PointerSize, $"global {name}");
        helpers.WritePointer(fragment.Data.AsSpan(0), pointerValue);
        fragments.Add(fragment);
        globals.Add((name, fragment.Address));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHandleTableMemoryRegions_SingleMapSingleBucketSingleSegment(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        var types = BuildTypes(helpers);

        int pointerSize = helpers.PointerSize;
        const uint handleSegmentSize = 0x10000;
        const uint initialHandleTableArraySize = 10;

        var allocator = builder.CreateAllocator(0x1_0000, 0x2_0000);

        var bucketsArray = allocator.Allocate((ulong)(initialHandleTableArraySize * pointerSize), "buckets array");
        var mapFragment = allocator.Allocate(types[DataType.HandleTableMap].Size!.Value, "HandleTableMap");
        var bucketFragment = allocator.Allocate(types[DataType.HandleTableBucket].Size!.Value, "HandleTableBucket");
        var tableArray = allocator.Allocate((ulong)pointerSize, "table array");
        var tableFragment = allocator.Allocate(types[DataType.HandleTable].Size!.Value, "HandleTable");
        var segmentFragment = allocator.Allocate(types[DataType.HandleTableSegment].Size!.Value, "HandleTableSegment");

        var mapFields = types[DataType.HandleTableMap].Fields;
        helpers.WritePointer(mapFragment.Data.AsSpan(mapFields["Buckets"].Offset), bucketsArray.Address);
        helpers.WritePointer(mapFragment.Data.AsSpan(mapFields["Next"].Offset), 0);
        helpers.Write(mapFragment.Data.AsSpan(mapFields["MaxIndex"].Offset), (uint)0);

        helpers.WritePointer(bucketsArray.Data.AsSpan(0), bucketFragment.Address);
        for (int i = 1; i < (int)initialHandleTableArraySize; i++)
            helpers.WritePointer(bucketsArray.Data.AsSpan(i * pointerSize), 0UL);

        var bucketFields = types[DataType.HandleTableBucket].Fields;
        helpers.WritePointer(bucketFragment.Data.AsSpan(bucketFields["Table"].Offset), tableArray.Address);
        helpers.Write(bucketFragment.Data.AsSpan(bucketFields["HandleTableIndex"].Offset), (uint)0);

        helpers.WritePointer(tableArray.Data.AsSpan(0), tableFragment.Address);

        var tableFields = types[DataType.HandleTable].Fields;
        helpers.WritePointer(tableFragment.Data.AsSpan(tableFields["SegmentList"].Offset), segmentFragment.Address);

        var segmentFields = types[DataType.HandleTableSegment].Fields;
        helpers.WritePointer(segmentFragment.Data.AsSpan(segmentFields["NextSegment"].Offset), 0UL);

        builder.AddHeapFragments([bucketsArray, mapFragment, bucketFragment, tableArray, tableFragment, segmentFragment]);

        (string Name, ulong Value)[] globals = [
            (nameof(Constants.Globals.HandleTableMap), mapFragment.Address),
            (nameof(Constants.Globals.HandleSegmentSize), handleSegmentSize),
            (nameof(Constants.Globals.InitialHandleTableArraySize), initialHandleTableArraySize),
        ];
        (string Name, string Value)[] globalStrings = [
            (nameof(Constants.Globals.GCIdentifiers), "workstation, segments, background,"),
        ];

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals, globalStrings);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.GC == ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1)));

        IGC gc = target.Contracts.GC;
        var regions = gc.GetHandleTableMemoryRegions().ToList();

        Assert.Single(regions);
        Assert.Equal(segmentFragment.Address, regions[0].Start);
        Assert.Equal((ulong)handleSegmentSize, regions[0].Size);
        Assert.Equal(0, regions[0].Heap);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCBookkeepingMemoryRegions_SingleCardTableEntry(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        var types = BuildTypes(helpers);

        int pointerSize = helpers.PointerSize;
        ulong cardTableInfoSize = types[DataType.CardTableInfo].Size!.Value;

        var allocator = builder.CreateAllocator(0x1_0000, 0x2_0000);

        var bookkeepingStartPtr = allocator.Allocate((ulong)pointerSize, "bookkeeping_start pointer");
        var ctiFragment = allocator.Allocate(cardTableInfoSize, "CardTableInfo");

        helpers.WritePointer(bookkeepingStartPtr.Data.AsSpan(0), ctiFragment.Address);

        var ctiFields = types[DataType.CardTableInfo].Fields;
        helpers.Write(ctiFragment.Data.AsSpan(ctiFields["Recount"].Offset), (uint)1);
        helpers.WriteNUInt(ctiFragment.Data.AsSpan(ctiFields["Size"].Offset), new TargetNUInt(4096));
        helpers.WritePointer(ctiFragment.Data.AsSpan(ctiFields["NextCardTable"].Offset), 0UL);

        builder.AddHeapFragments([bookkeepingStartPtr, ctiFragment]);

        (string Name, ulong Value)[] globals = [
            (nameof(Constants.Globals.GCHeapBookkeepingStart), bookkeepingStartPtr.Address),
            (nameof(Constants.Globals.CardTableInfoSize), cardTableInfoSize),
        ];
        (string Name, string Value)[] globalStrings = [
            (nameof(Constants.Globals.GCIdentifiers), "workstation, segments, background,"),
        ];

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals, globalStrings);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.GC == ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1)));

        IGC gc = target.Contracts.GC;
        var regions = gc.GetGCBookkeepingMemoryRegions().ToList();

        Assert.Single(regions);
        Assert.Equal(ctiFragment.Address, regions[0].Start);
        Assert.Equal(4096UL, regions[0].Size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCFreeRegions_WorkstationWithOneFreeRegion(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        var types = BuildTypes(helpers);

        const int countFreeRegionKinds = 3;

        var allocator = builder.CreateAllocator(0x10_0000, 0x20_0000);
        var fragments = new List<MockMemorySpace.HeapFragment>();
        var globals = new List<(string Name, ulong Value)>();

        // Mandatory GCHeapWKS pointer-read globals: ReadPointer(ReadGlobalPointer(name))
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapMarkArray));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapNextSweepObj));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapBackgroundMinSavedAddr));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapBackgroundMaxSavedAddr));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapAllocAllocated));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapEphemeralHeapSegment));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapCardTable));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapFinalizeQueue));
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapInternalRootArray));

        // ReadNUInt(ReadGlobalPointer(name))
        WritePointerGlobal(helpers, allocator, fragments, globals, nameof(Constants.Globals.GCHeapInternalRootArrayIndex));

        // Read<int>(ReadGlobalPointer(name))
        {
            var frag = allocator.Allocate(sizeof(int), $"global {nameof(Constants.Globals.GCHeapHeapAnalyzeSuccess)}");
            helpers.Write(frag.Data.AsSpan(0), 0);
            fragments.Add(frag);
            globals.Add((nameof(Constants.Globals.GCHeapHeapAnalyzeSuccess), frag.Address));
        }

        // ReadGlobalPointer-only globals (no memory dereference in GCHeapWKS constructor)
        globals.Add((nameof(Constants.Globals.GCHeapGenerationTable), 0xDEAD_0010));
        globals.Add((nameof(Constants.Globals.GCHeapInterestingData), 0xDEAD_0020));
        globals.Add((nameof(Constants.Globals.GCHeapCompactReasons), 0xDEAD_0030));
        globals.Add((nameof(Constants.Globals.GCHeapExpandMechanisms), 0xDEAD_0040));
        globals.Add((nameof(Constants.Globals.GCHeapInterestingMechanismBits), 0xDEAD_0050));

        // OomHistory: ProcessedData.GetOrAdd<OomHistory>(ReadGlobalPointer(name))
        var oomFragment = allocator.Allocate(types[DataType.OomHistory].Size!.Value, "OomHistory");
        fragments.Add(oomFragment);
        globals.Add((nameof(Constants.Globals.GCHeapOomData), oomFragment.Address));

        // Direct value globals
        globals.Add((nameof(Constants.Globals.CountFreeRegionKinds), countFreeRegionKinds));

        // RegionFreeList array for GCHeapFreeRegions
        uint regionFreeListSize = types[DataType.RegionFreeList].Size!.Value;
        var freeRegionsArray = allocator.Allocate((ulong)(countFreeRegionKinds * regionFreeListSize), "RegionFreeList array");
        globals.Add((nameof(Constants.Globals.GCHeapFreeRegions), freeRegionsArray.Address));

        // HeapSegment for the first free region
        var heapSegment = allocator.Allocate(types[DataType.HeapSegment].Size!.Value, "HeapSegment");
        var segFields = types[DataType.HeapSegment].Fields;
        const ulong expectedMem = 0xA000;
        const ulong expectedCommitted = 0xB000;
        helpers.WritePointer(heapSegment.Data.AsSpan(segFields["Allocated"].Offset), 0);
        helpers.WritePointer(heapSegment.Data.AsSpan(segFields["Committed"].Offset), expectedCommitted);
        helpers.WritePointer(heapSegment.Data.AsSpan(segFields["Reserved"].Offset), 0);
        helpers.WritePointer(heapSegment.Data.AsSpan(segFields["Used"].Offset), 0);
        helpers.WritePointer(heapSegment.Data.AsSpan(segFields["Mem"].Offset), expectedMem);
        helpers.WriteNUInt(heapSegment.Data.AsSpan(segFields["Flags"].Offset), new TargetNUInt(0));
        helpers.WritePointer(heapSegment.Data.AsSpan(segFields["Next"].Offset), 0);
        helpers.WritePointer(heapSegment.Data.AsSpan(segFields["BackgroundAllocated"].Offset), 0);
        fragments.Add(heapSegment);

        // First RegionFreeList points to the HeapSegment; rest are null
        var rflFields = types[DataType.RegionFreeList].Fields;
        helpers.WritePointer(freeRegionsArray.Data.AsSpan(rflFields["HeadFreeRegion"].Offset), heapSegment.Address);
        for (int i = 1; i < countFreeRegionKinds; i++)
            helpers.WritePointer(freeRegionsArray.Data.AsSpan((int)(i * regionFreeListSize) + rflFields["HeadFreeRegion"].Offset), 0UL);
        fragments.Add(freeRegionsArray);

        builder.AddHeapFragments(fragments);

        (string Name, string Value)[] globalStrings = [
            (nameof(Constants.Globals.GCIdentifiers), "workstation, segments, background,"),
        ];

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals.ToArray(), globalStrings);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.GC == ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1)));

        IGC gc = target.Contracts.GC;
        var regions = gc.GetGCFreeRegions().ToList();

        Assert.Single(regions);
        Assert.Equal(expectedMem, regions[0].Start);
        Assert.Equal(expectedCommitted - expectedMem, regions[0].Size);
        Assert.Equal((ulong)FreeRegionKind.FreeRegion, regions[0].ExtraData);
        Assert.Equal(0, regions[0].Heap);
    }
}
