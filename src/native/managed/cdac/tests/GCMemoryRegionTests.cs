// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GCMemoryRegionTests
{
    private const uint HandleSegmentSize = 0x10000;
    private const uint InitialHandleTableArraySize = 2;
    private const uint HandleBlocksPerSegment = 64;
    private const uint HandleMaxInternalTypes = 12;
    private const uint HandlesPerBlock = 256;
    private const byte BlockInvalid = 0xFF;
    private const uint CountFreeRegionKinds = 3;

    private static IGC CreateGCContract(MockTarget.Architecture arch,
        Dictionary<DataType, Target.TypeInfo> types,
        (string Name, ulong Value)[] globals,
        (string Name, string Value)[] globalStrings,
        TestPlaceholderTarget.ReadFromTargetDelegate readFromTarget)
    {
        var target = new TestPlaceholderTarget(arch, readFromTarget, types, globals, globalStrings);
        var gcContract = ((IContractFactory<IGC>)new GCFactory()).CreateContract(target, 1);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.GC == gcContract));
        return gcContract;
    }

    private static (string Name, ulong Value)[] BuildGlobals(
        Dictionary<string, ulong> pointerGlobals,
        Dictionary<string, ulong> valueGlobals)
    {
        var result = new List<(string Name, ulong Value)>();

        result.Add((Constants.Globals.HandlesPerBlock, HandlesPerBlock));
        result.Add((Constants.Globals.BlockInvalid, BlockInvalid));
        result.Add((Constants.Globals.DebugDestroyedHandleValue, 0));
        result.Add((Constants.Globals.HandleMaxInternalTypes, HandleMaxInternalTypes));
        result.Add((Constants.Globals.HandleSegmentSize, HandleSegmentSize));
        result.Add((Constants.Globals.InitialHandleTableArraySize, InitialHandleTableArraySize));
        result.Add((Constants.Globals.HandleBlocksPerSegment, HandleBlocksPerSegment));
        result.Add((Constants.Globals.CountFreeRegionKinds, CountFreeRegionKinds));

        foreach (var (name, value) in valueGlobals)
            result.Add((name, value));
        foreach (var (name, value) in pointerGlobals)
            result.Add((name, value));

        return result.ToArray();
    }

    private static Dictionary<string, Target.FieldInfo> MakeFields(params (string Name, int Offset, DataType Type)[] fields)
    {
        var result = new Dictionary<string, Target.FieldInfo>();
        foreach (var (name, offset, type) in fields)
            result[name] = new Target.FieldInfo { Offset = offset, Type = type };
        return result;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHandleTableMemoryRegions_SingleSegment(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0100_0000);
        int ptrSize = helpers.PointerSize;

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.HandleTableMap] = new() { Fields = MakeFields(("BucketsPtr", 0, DataType.pointer), ("Next", ptrSize, DataType.pointer)) },
            [DataType.HandleTableBucket] = new() { Fields = MakeFields(("Table", 0, DataType.pointer)) },
            [DataType.HandleTable] = new() { Fields = MakeFields(("SegmentList", 0, DataType.pointer)) },
            [DataType.TableSegment] = new() { Fields = MakeFields(
                ("NextSegment", 0, DataType.pointer),
                ("RgAllocation", ptrSize, DataType.uint8),
                ("RgTail", ptrSize + (int)HandleBlocksPerSegment, DataType.uint8),
                ("RgValue", ptrSize + (int)HandleBlocksPerSegment + (int)HandleMaxInternalTypes, DataType.pointer),
                ("RgUserData", ptrSize + (int)HandleBlocksPerSegment + (int)HandleMaxInternalTypes + ptrSize, DataType.uint8)) },
        };

        var segFragment = allocator.Allocate(HandleSegmentSize, "Segment");
        builder.AddHeapFragment(segFragment);
        Span<byte> segSpan = builder.BorrowAddressRange(segFragment.Address, segFragment.Data.Length);
        helpers.WritePointer(segSpan.Slice(0, ptrSize), TargetPointer.Null);

        var htFragment = allocator.Allocate((ulong)ptrSize, "HandleTable");
        builder.AddHeapFragment(htFragment);
        Span<byte> htSpan = builder.BorrowAddressRange(htFragment.Address, htFragment.Data.Length);
        helpers.WritePointer(htSpan.Slice(0, ptrSize), segFragment.Address);

        var bucketTableFragment = allocator.Allocate((ulong)ptrSize, "BucketTable");
        builder.AddHeapFragment(bucketTableFragment);
        Span<byte> btSpan = builder.BorrowAddressRange(bucketTableFragment.Address, bucketTableFragment.Data.Length);
        helpers.WritePointer(btSpan.Slice(0, ptrSize), htFragment.Address);

        var bucketFragment = allocator.Allocate((ulong)ptrSize, "Bucket");
        builder.AddHeapFragment(bucketFragment);
        Span<byte> bSpan = builder.BorrowAddressRange(bucketFragment.Address, bucketFragment.Data.Length);
        helpers.WritePointer(bSpan.Slice(0, ptrSize), bucketTableFragment.Address);

        var bucketsArrayFragment = allocator.Allocate((ulong)(InitialHandleTableArraySize * ptrSize), "BucketsArray");
        builder.AddHeapFragment(bucketsArrayFragment);
        Span<byte> baSpan = builder.BorrowAddressRange(bucketsArrayFragment.Address, bucketsArrayFragment.Data.Length);
        helpers.WritePointer(baSpan.Slice(0, ptrSize), bucketFragment.Address);
        helpers.WritePointer(baSpan.Slice(ptrSize, ptrSize), TargetPointer.Null);

        var mapFragment = allocator.Allocate((ulong)(2 * ptrSize), "Map");
        builder.AddHeapFragment(mapFragment);
        Span<byte> mapSpan = builder.BorrowAddressRange(mapFragment.Address, mapFragment.Data.Length);
        helpers.WritePointer(mapSpan.Slice(0, ptrSize), bucketsArrayFragment.Address);
        helpers.WritePointer(mapSpan.Slice(ptrSize, ptrSize), TargetPointer.Null);

        var globals = BuildGlobals(
            new() { [Constants.Globals.HandleTableMap] = mapFragment.Address },
            new());

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        IReadOnlyList<GCMemoryRegionData> regions = gc.GetHandleTableMemoryRegions();

        Assert.Single(regions);
        Assert.Equal(new TargetPointer(segFragment.Address), regions[0].Start);
        Assert.Equal((ulong)HandleSegmentSize, regions[0].Size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHandleTableMemoryRegions_AllNullBuckets_ReturnsEmpty(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0100_0000);
        int ptrSize = helpers.PointerSize;

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.HandleTableMap] = new() { Fields = MakeFields(("BucketsPtr", 0, DataType.pointer), ("Next", ptrSize, DataType.pointer)) },
            [DataType.HandleTableBucket] = new() { Fields = MakeFields(("Table", 0, DataType.pointer)) },
            [DataType.HandleTable] = new() { Fields = MakeFields(("SegmentList", 0, DataType.pointer)) },
            [DataType.TableSegment] = new() { Fields = MakeFields(("NextSegment", 0, DataType.pointer)) },
        };

        var bucketsArrayFragment = allocator.Allocate((ulong)(InitialHandleTableArraySize * ptrSize), "BucketsArray");
        builder.AddHeapFragment(bucketsArrayFragment);
        Span<byte> baSpan = builder.BorrowAddressRange(bucketsArrayFragment.Address, bucketsArrayFragment.Data.Length);
        helpers.WritePointer(baSpan.Slice(0, ptrSize), TargetPointer.Null);
        helpers.WritePointer(baSpan.Slice(ptrSize, ptrSize), TargetPointer.Null);

        var mapFragment = allocator.Allocate((ulong)(2 * ptrSize), "Map");
        builder.AddHeapFragment(mapFragment);
        Span<byte> mapSpan = builder.BorrowAddressRange(mapFragment.Address, mapFragment.Data.Length);
        helpers.WritePointer(mapSpan.Slice(0, ptrSize), bucketsArrayFragment.Address);
        helpers.WritePointer(mapSpan.Slice(ptrSize, ptrSize), TargetPointer.Null);

        var globals = BuildGlobals(
            new() { [Constants.Globals.HandleTableMap] = mapFragment.Address },
            new());

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        Assert.Empty(gc.GetHandleTableMemoryRegions());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetHandleTableMemoryRegions_MultipleLinkedSegments(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0100_0000);
        int ptrSize = helpers.PointerSize;

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.HandleTableMap] = new() { Fields = MakeFields(("BucketsPtr", 0, DataType.pointer), ("Next", ptrSize, DataType.pointer)) },
            [DataType.HandleTableBucket] = new() { Fields = MakeFields(("Table", 0, DataType.pointer)) },
            [DataType.HandleTable] = new() { Fields = MakeFields(("SegmentList", 0, DataType.pointer)) },
            [DataType.TableSegment] = new() { Fields = MakeFields(
                ("NextSegment", 0, DataType.pointer),
                ("RgAllocation", ptrSize, DataType.uint8),
                ("RgTail", ptrSize + (int)HandleBlocksPerSegment, DataType.uint8),
                ("RgValue", ptrSize + (int)HandleBlocksPerSegment + (int)HandleMaxInternalTypes, DataType.pointer),
                ("RgUserData", ptrSize + (int)HandleBlocksPerSegment + (int)HandleMaxInternalTypes + ptrSize, DataType.uint8)) },
        };

        var seg2Fragment = allocator.Allocate(HandleSegmentSize, "Seg2");
        builder.AddHeapFragment(seg2Fragment);
        Span<byte> seg2Span = builder.BorrowAddressRange(seg2Fragment.Address, seg2Fragment.Data.Length);
        helpers.WritePointer(seg2Span.Slice(0, ptrSize), TargetPointer.Null);

        var seg1Fragment = allocator.Allocate(HandleSegmentSize, "Seg1");
        builder.AddHeapFragment(seg1Fragment);
        Span<byte> seg1Span = builder.BorrowAddressRange(seg1Fragment.Address, seg1Fragment.Data.Length);
        helpers.WritePointer(seg1Span.Slice(0, ptrSize), seg2Fragment.Address);

        var htFragment = allocator.Allocate((ulong)ptrSize, "HT");
        builder.AddHeapFragment(htFragment);
        Span<byte> htSpan = builder.BorrowAddressRange(htFragment.Address, htFragment.Data.Length);
        helpers.WritePointer(htSpan.Slice(0, ptrSize), seg1Fragment.Address);

        var btFragment = allocator.Allocate((ulong)ptrSize, "BT");
        builder.AddHeapFragment(btFragment);
        Span<byte> btSpan = builder.BorrowAddressRange(btFragment.Address, btFragment.Data.Length);
        helpers.WritePointer(btSpan.Slice(0, ptrSize), htFragment.Address);

        var bucketFragment = allocator.Allocate((ulong)ptrSize, "Bucket");
        builder.AddHeapFragment(bucketFragment);
        Span<byte> bucketSpan = builder.BorrowAddressRange(bucketFragment.Address, bucketFragment.Data.Length);
        helpers.WritePointer(bucketSpan.Slice(0, ptrSize), btFragment.Address);

        var baFragment = allocator.Allocate((ulong)(InitialHandleTableArraySize * ptrSize), "BA");
        builder.AddHeapFragment(baFragment);
        Span<byte> baSpan = builder.BorrowAddressRange(baFragment.Address, baFragment.Data.Length);
        helpers.WritePointer(baSpan.Slice(0, ptrSize), bucketFragment.Address);
        helpers.WritePointer(baSpan.Slice(ptrSize, ptrSize), TargetPointer.Null);

        var mapFragment = allocator.Allocate((ulong)(2 * ptrSize), "Map");
        builder.AddHeapFragment(mapFragment);
        Span<byte> mapSpan = builder.BorrowAddressRange(mapFragment.Address, mapFragment.Data.Length);
        helpers.WritePointer(mapSpan.Slice(0, ptrSize), baFragment.Address);
        helpers.WritePointer(mapSpan.Slice(ptrSize, ptrSize), TargetPointer.Null);

        var globals = BuildGlobals(
            new() { [Constants.Globals.HandleTableMap] = mapFragment.Address },
            new());

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        IReadOnlyList<GCMemoryRegionData> regions = gc.GetHandleTableMemoryRegions();

        Assert.Equal(2, regions.Count);
        Assert.Equal(new TargetPointer(seg1Fragment.Address), regions[0].Start);
        Assert.Equal(new TargetPointer(seg2Fragment.Address), regions[1].Start);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCBookkeepingMemoryRegions_SingleEntry(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0100_0000);
        int ptrSize = helpers.PointerSize;

        uint cardTableTypeSize = (uint)(4 + ptrSize + ptrSize);
        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.CardTableInfo] = new() { Fields = MakeFields(
                ("Recount", 0, DataType.uint32),
                ("Size", 4, DataType.nuint),
                ("NextCardTable", 4 + ptrSize, DataType.pointer)), Size = cardTableTypeSize },
        };

        var ctiFragment = allocator.Allocate(cardTableTypeSize, "CTI");
        builder.AddHeapFragment(ctiFragment);
        Span<byte> ctiSpan = builder.BorrowAddressRange(ctiFragment.Address, ctiFragment.Data.Length);
        helpers.Write(ctiSpan.Slice(0, sizeof(uint)), (uint)1);
        if (ptrSize == 8)
            helpers.Write(ctiSpan.Slice(4, sizeof(ulong)), (ulong)0x1000);
        else
            helpers.Write(ctiSpan.Slice(4, sizeof(uint)), (uint)0x1000);
        helpers.WritePointer(ctiSpan.Slice(4 + ptrSize, ptrSize), TargetPointer.Null);

        var bkFragment = allocator.Allocate((ulong)ptrSize, "BK");
        builder.AddHeapFragment(bkFragment);
        Span<byte> bkSpan = builder.BorrowAddressRange(bkFragment.Address, bkFragment.Data.Length);
        helpers.WritePointer(bkSpan.Slice(0, ptrSize), ctiFragment.Address);

        var globals = BuildGlobals(
            new() { [Constants.Globals.BookkeepingStart] = bkFragment.Address },
            new() { [Constants.Globals.CardTableInfoSize] = cardTableTypeSize });

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        IReadOnlyList<GCMemoryRegionData> regions = gc.GetGCBookkeepingMemoryRegions();

        Assert.Single(regions);
        Assert.Equal(new TargetPointer(ctiFragment.Address), regions[0].Start);
        Assert.Equal((ulong)0x1000, regions[0].Size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCBookkeepingMemoryRegions_MissingGlobal_ReturnsEmpty(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);

        var types = new Dictionary<DataType, Target.TypeInfo>();
        var globals = BuildGlobals(new(), new());

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        Assert.Empty(gc.GetGCBookkeepingMemoryRegions());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCFreeRegions_SingleFreeRegion(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0100_0000);
        int ptrSize = helpers.PointerSize;

        uint heapSegmentSize = (uint)(8 * ptrSize);
        uint regionFreeListSize = (uint)(7 * ptrSize);

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.HeapSegment] = new() { Fields = MakeFields(
                ("Allocated", 0, DataType.pointer),
                ("Committed", ptrSize, DataType.pointer),
                ("Reserved", 2 * ptrSize, DataType.pointer),
                ("Used", 3 * ptrSize, DataType.pointer),
                ("Mem", 4 * ptrSize, DataType.pointer),
                ("Flags", 5 * ptrSize, DataType.nuint),
                ("Next", 6 * ptrSize, DataType.pointer),
                ("BackgroundAllocated", 7 * ptrSize, DataType.pointer)), Size = heapSegmentSize },
            [DataType.RegionFreeList] = new() { Fields = MakeFields(
                ("HeadFreeRegion", 5 * ptrSize, DataType.pointer)), Size = regionFreeListSize },
        };

        TargetPointer memAddr = new(0x1000_0000);
        TargetPointer committedAddr = new(0x1000_2000);

        var segFragment = allocator.Allocate(heapSegmentSize, "Seg");
        builder.AddHeapFragment(segFragment);
        Span<byte> segSpan = builder.BorrowAddressRange(segFragment.Address, segFragment.Data.Length);
        helpers.WritePointer(segSpan.Slice(ptrSize, ptrSize), committedAddr);
        helpers.WritePointer(segSpan.Slice(4 * ptrSize, ptrSize), memAddr);
        helpers.WritePointer(segSpan.Slice(6 * ptrSize, ptrSize), TargetPointer.Null);

        var flFragment = allocator.Allocate(regionFreeListSize, "FL");
        builder.AddHeapFragment(flFragment);
        Span<byte> flSpan = builder.BorrowAddressRange(flFragment.Address, flFragment.Data.Length);
        helpers.WritePointer(flSpan.Slice(5 * ptrSize, ptrSize), segFragment.Address);

        var globals = BuildGlobals(
            new() { [Constants.Globals.GlobalFreeHugeRegions] = flFragment.Address },
            new());

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        IReadOnlyList<GCMemoryRegionData> regions = gc.GetGCFreeRegions();

        Assert.Single(regions);
        Assert.Equal(memAddr, regions[0].Start);
        Assert.Equal((ulong)(committedAddr - memAddr), regions[0].Size);
        Assert.Equal((ulong)FreeRegionKind.FreeGlobalHugeRegion, regions[0].ExtraData);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCFreeRegions_NoFreeRegions_ReturnsEmpty(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0100_0000);
        int ptrSize = helpers.PointerSize;

        uint regionFreeListSize = (uint)(7 * ptrSize);
        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RegionFreeList] = new() { Fields = MakeFields(
                ("HeadFreeRegion", 5 * ptrSize, DataType.pointer)), Size = regionFreeListSize },
        };

        var flFragment = allocator.Allocate(regionFreeListSize, "FL");
        builder.AddHeapFragment(flFragment);
        Span<byte> flSpan = builder.BorrowAddressRange(flFragment.Address, flFragment.Data.Length);
        helpers.WritePointer(flSpan.Slice(5 * ptrSize, ptrSize), TargetPointer.Null);

        var globals = BuildGlobals(
            new() { [Constants.Globals.GlobalFreeHugeRegions] = flFragment.Address },
            new());

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        Assert.Empty(gc.GetGCFreeRegions());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCFreeRegions_MultipleFreeRegionsWithLinkedSegments(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0100_0000);
        int ptrSize = helpers.PointerSize;

        uint heapSegmentSize = (uint)(8 * ptrSize);
        uint regionFreeListSize = (uint)(7 * ptrSize);

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.HeapSegment] = new() { Fields = MakeFields(
                ("Allocated", 0, DataType.pointer),
                ("Committed", ptrSize, DataType.pointer),
                ("Reserved", 2 * ptrSize, DataType.pointer),
                ("Used", 3 * ptrSize, DataType.pointer),
                ("Mem", 4 * ptrSize, DataType.pointer),
                ("Flags", 5 * ptrSize, DataType.nuint),
                ("Next", 6 * ptrSize, DataType.pointer),
                ("BackgroundAllocated", 7 * ptrSize, DataType.pointer)), Size = heapSegmentSize },
            [DataType.RegionFreeList] = new() { Fields = MakeFields(
                ("HeadFreeRegion", 5 * ptrSize, DataType.pointer)), Size = regionFreeListSize },
        };

        var seg2Fragment = allocator.Allocate(heapSegmentSize, "Seg2");
        builder.AddHeapFragment(seg2Fragment);
        Span<byte> seg2Span = builder.BorrowAddressRange(seg2Fragment.Address, seg2Fragment.Data.Length);
        helpers.WritePointer(seg2Span.Slice(ptrSize, ptrSize), new TargetPointer(0x2000_4000));
        helpers.WritePointer(seg2Span.Slice(4 * ptrSize, ptrSize), new TargetPointer(0x2000_0000));
        helpers.WritePointer(seg2Span.Slice(6 * ptrSize, ptrSize), TargetPointer.Null);

        var seg1Fragment = allocator.Allocate(heapSegmentSize, "Seg1");
        builder.AddHeapFragment(seg1Fragment);
        Span<byte> seg1Span = builder.BorrowAddressRange(seg1Fragment.Address, seg1Fragment.Data.Length);
        helpers.WritePointer(seg1Span.Slice(ptrSize, ptrSize), new TargetPointer(0x1000_2000));
        helpers.WritePointer(seg1Span.Slice(4 * ptrSize, ptrSize), new TargetPointer(0x1000_0000));
        helpers.WritePointer(seg1Span.Slice(6 * ptrSize, ptrSize), seg2Fragment.Address);

        var flFragment = allocator.Allocate(regionFreeListSize, "FL");
        builder.AddHeapFragment(flFragment);
        Span<byte> flSpan = builder.BorrowAddressRange(flFragment.Address, flFragment.Data.Length);
        helpers.WritePointer(flSpan.Slice(5 * ptrSize, ptrSize), seg1Fragment.Address);

        var globals = BuildGlobals(
            new() { [Constants.Globals.GlobalFreeHugeRegions] = flFragment.Address },
            new());

        var readContext = builder.GetMemoryContext();
        IGC gc = CreateGCContract(arch, types, globals,
            [(Constants.Globals.GCIdentifiers, "workstation,regions,background")],
            readContext.ReadFromTarget);

        IReadOnlyList<GCMemoryRegionData> regions = gc.GetGCFreeRegions();

        Assert.Equal(2, regions.Count);
        Assert.Equal(new TargetPointer(0x1000_0000), regions[0].Start);
        Assert.Equal((ulong)0x2000, regions[0].Size);
        Assert.Equal(new TargetPointer(0x2000_0000), regions[1].Start);
        Assert.Equal((ulong)0x4000, regions[1].Size);
    }
}
