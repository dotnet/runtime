// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct GC_1 : IGC
{
    private const uint WRK_HEAP_COUNT = 1;

    private enum GCType
    {
        Unknown,
        Workstation,
        Server,
    }

    private enum HandleType_1
    {
        WeakShort = 0,
        WeakLong = 1,
        Strong = 2,
        Pinned = 3,
        RefCounted = 5,
        Dependent = 6,
        WeakInteriorPointer = 10,
        CrossReference = 11
    }

    private readonly Target _target;
    private readonly uint _handlesPerBlock;
    private readonly byte _blockInvalid;
    private readonly TargetPointer _debugDestroyedHandleValue;
    private readonly uint _handleMaxInternalTypes;

    internal GC_1(Target target, uint handlesPerBlock, byte blockInvalid, TargetPointer debugDestroyedHandleValue, uint handleMaxInternalTypes)
    {
        _target = target;
        _handlesPerBlock = handlesPerBlock;
        _blockInvalid = blockInvalid;
        _debugDestroyedHandleValue = debugDestroyedHandleValue;
        _handleMaxInternalTypes = handleMaxInternalTypes;
    }

    string[] IGC.GetGCIdentifiers()
    {
        string gcIdentifiers = _target.ReadGlobalString(Constants.Globals.GCIdentifiers);
        return gcIdentifiers.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    uint IGC.GetGCHeapCount()
    {
        switch (GetGCType())
        {
            case GCType.Workstation:
                return WRK_HEAP_COUNT; // Workstation GC has a single heap
            case GCType.Server:
                TargetPointer pNumHeaps = _target.ReadGlobalPointer(Constants.Globals.NumHeaps);
                return (uint)_target.Read<int>(pNumHeaps);
            default:
                throw new NotImplementedException("Unknown GC type");
        }
    }

    bool IGC.GetGCStructuresValid()
    {
        TargetPointer pInvalidCount = _target.ReadGlobalPointer(Constants.Globals.StructureInvalidCount);
        int invalidCount = _target.Read<int>(pInvalidCount);
        return invalidCount == 0; // Structures are valid if the count of invalid structures is zero
    }

    uint IGC.GetMaxGeneration()
    {
        TargetPointer pMaxGeneration = _target.ReadGlobalPointer(Constants.Globals.MaxGeneration);
        return _target.Read<uint>(pMaxGeneration);
    }

    void IGC.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr)
    {
        minAddr = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCLowestAddress));
        maxAddr = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHighestAddress));
    }

    uint IGC.GetCurrentGCState()
    {
        if (!IsBackgroundGCEnabled())
            return 0;
        return _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.CurrentGCState));
    }

    bool IGC.TryGetGCDynamicAdaptationMode(out int mode)
    {
        mode = default;
        if (!IsDatasEnabled())
            return false;
        mode = _target.Read<int>(_target.ReadGlobalPointer(Constants.Globals.DynamicAdaptationMode));
        return true;
    }

    GCHeapSegmentData IGC.GetHeapSegmentData(TargetPointer segmentAddress)
    {
        Data.HeapSegment heapSegment = _target.ProcessedData.GetOrAdd<Data.HeapSegment>(segmentAddress);
        return new GCHeapSegmentData()
        {
            Allocated = heapSegment.Allocated,
            Committed = heapSegment.Committed,
            Reserved = heapSegment.Reserved,
            Used = heapSegment.Used,
            Mem = heapSegment.Mem,
            Flags = heapSegment.Flags,
            Next = heapSegment.Next,
            BackgroundAllocated = heapSegment.BackgroundAllocated,
            Heap = heapSegment.Heap ?? TargetPointer.Null,
        };
    }

    IReadOnlyList<TargetNUInt> IGC.GetGlobalMechanisms()
    {
        if (!_target.TryReadGlobalPointer(Constants.Globals.GCGlobalMechanisms, out TargetPointer? globalMechanismsArrayStart))
            return Array.Empty<TargetNUInt>();
        uint globalMechanismsLength = _target.ReadGlobal<uint>(Constants.Globals.GlobalMechanismsLength);
        return ReadGCHeapDataArray(globalMechanismsArrayStart.Value, globalMechanismsLength);
    }

    IEnumerable<TargetPointer> IGC.GetGCHeaps()
    {
        if (GetGCType() != GCType.Server)
            yield break; // Only server GC has multiple heaps

        uint heapCount = ((IGC)this).GetGCHeapCount();
        TargetPointer heapTable = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.Heaps));
        for (uint i = 0; i < heapCount; i++)
        {
            yield return _target.ReadPointer(heapTable + (i * (uint)_target.PointerSize));
        }
    }

    GCHeapData IGC.GetHeapData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("GetHeapData() is only valid for Workstation GC.");

        return GetGCHeapDataFromHeap(new GCHeapWKS(_target));
    }

    GCHeapData IGC.GetHeapData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetHeapData(TargetPointer heap) is only valid for Server GC.");

        Data.GCHeapSVR heap = _target.ProcessedData.GetOrAdd<Data.GCHeapSVR>(heapAddress);
        return GetGCHeapDataFromHeap(heap);
    }

    private GCHeapData GetGCHeapDataFromHeap(IGCHeap heap)
    {
        Data.CFinalize finalize = _target.ProcessedData.GetOrAdd<Data.CFinalize>(heap.FinalizeQueue);

        return new GCHeapData()
        {
            MarkArray = heap.MarkArray,
            NextSweepObject = heap.NextSweepObj,
            BackGroundSavedMinAddress = heap.BackgroundMinSavedAddr,
            BackGroundSavedMaxAddress = heap.BackgroundMaxSavedAddr,
            AllocAllocated = heap.AllocAllocated,
            EphemeralHeapSegment = heap.EphemeralHeapSegment,
            CardTable = heap.CardTable,
            GenerationTable = GetGenerationData(heap.GenerationTable).AsReadOnly(),
            FillPointers = GetFillPointers(finalize).AsReadOnly(),
            SavedSweepEphemeralSegment = heap.SavedSweepEphemeralSeg ?? TargetPointer.Null,
            SavedSweepEphemeralStart = heap.SavedSweepEphemeralStart ?? TargetPointer.Null,

            InternalRootArray = heap.InternalRootArray,
            InternalRootArrayIndex = heap.InternalRootArrayIndex,
            HeapAnalyzeSuccess = heap.HeapAnalyzeSuccess,

            InterestingData = ReadGCHeapDataArray(
                heap.InterestingData,
                _target.ReadGlobal<uint>(Constants.Globals.InterestingDataLength))
                .AsReadOnly(),
            CompactReasons = ReadGCHeapDataArray(
                heap.CompactReasons,
                _target.ReadGlobal<uint>(Constants.Globals.CompactReasonsLength))
                .AsReadOnly(),
            ExpandMechanisms = ReadGCHeapDataArray(
                heap.ExpandMechanisms,
                _target.ReadGlobal<uint>(Constants.Globals.ExpandMechanismsLength))
                .AsReadOnly(),
            InterestingMechanismBits = ReadGCHeapDataArray(
                heap.InterestingMechanismBits,
                _target.ReadGlobal<uint>(Constants.Globals.InterestingMechanismBitsLength))
                .AsReadOnly(),
        };
    }

    private List<GCGenerationData> GetGenerationData(TargetPointer generationTableArrayStart)
    {
        uint generationTableLength = _target.ReadGlobal<uint>(Constants.Globals.TotalGenerationCount);
        uint generationSize = _target.GetTypeInfo(DataType.Generation).Size ?? throw new InvalidOperationException("Type Generation has no size");
        List<Data.Generation> generationTable = [];
        for (uint i = 0; i < generationTableLength; i++)
        {
            TargetPointer generationAddress = generationTableArrayStart + i * generationSize;
            generationTable.Add(_target.ProcessedData.GetOrAdd<Data.Generation>(generationAddress));
        }
        List<GCGenerationData> generationDataList = generationTable.Select(gen =>
        new GCGenerationData()
        {
            StartSegment = gen.StartSegment,
            AllocationStart = gen.AllocationStart ?? 0,
            AllocationContextPointer = gen.AllocationContext.Pointer,
            AllocationContextLimit = gen.AllocationContext.Limit,
        }).ToList();
        return generationDataList;
    }

    private List<TargetPointer> GetFillPointers(Data.CFinalize cFinalize)
    {
        uint fillPointersLength = _target.ReadGlobal<uint>(Constants.Globals.CFinalizeFillPointersLength);
        TargetPointer fillPointersArrayStart = cFinalize.FillPointers;
        List<TargetPointer> fillPointers = [];
        for (uint i = 0; i < fillPointersLength; i++)
            fillPointers.Add(_target.ReadPointer(fillPointersArrayStart + i * (uint)_target.PointerSize));
        return fillPointers;
    }

    private List<TargetNUInt> ReadGCHeapDataArray(TargetPointer arrayStart, uint length)
    {
        List<TargetNUInt> arr = [];
        for (uint i = 0; i < length; i++)
            arr.Add(_target.ReadNUInt(arrayStart + (i * (uint)_target.PointerSize)));
        return arr;
    }

    GCOomData IGC.GetOomData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("GetOomData() is only valid for Workstation GC.");

        TargetPointer oomHistory = _target.ReadGlobalPointer(Constants.Globals.GCHeapOomData);
        Data.OomHistory oomHistoryData = _target.ProcessedData.GetOrAdd<Data.OomHistory>(oomHistory);
        return GetGCOomData(oomHistoryData);
    }

    GCOomData IGC.GetOomData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetOomData(TargetPointer heap) is only valid for Server GC.");

        Data.GCHeapSVR heap = _target.ProcessedData.GetOrAdd<Data.GCHeapSVR>(heapAddress);
        return GetGCOomData(heap.OomData);
    }

    private static GCOomData GetGCOomData(Data.OomHistory oomHistory)
        => new GCOomData()
        {
            Reason = oomHistory.Reason,
            AllocSize = oomHistory.AllocSize,
            Reserved = oomHistory.Reserved,
            Allocated = oomHistory.Allocated,
            GCIndex = oomHistory.GcIndex,
            Fgm = oomHistory.Fgm,
            Size = oomHistory.Size,
            AvailablePagefileMB = oomHistory.AvailablePagefileMb,
            LohP = oomHistory.LohP != 0,
        };

    void IGC.GetGlobalAllocationContext(out TargetPointer allocPtr, out TargetPointer allocLimit)
    {
        TargetPointer globalAllocContextAddress = _target.ReadGlobalPointer(Constants.Globals.GlobalAllocContext);
        Data.EEAllocContext eeAllocContext = _target.ProcessedData.GetOrAdd<Data.EEAllocContext>(globalAllocContextAddress);
        allocPtr = eeAllocContext.GCAllocationContext.Pointer;
        allocLimit = eeAllocContext.GCAllocationContext.Limit;
    }

    private GCType GetGCType()
    {
        string[] identifiers = ((IGC)this).GetGCIdentifiers();
        if (identifiers.Contains(GCIdentifiers.Workstation))
        {
            return GCType.Workstation;
        }
        else if (identifiers.Contains(GCIdentifiers.Server))
        {
            return GCType.Server;
        }
        else
        {
            return GCType.Unknown; // Unknown or unsupported GC type
        }
    }

    private bool IsBackgroundGCEnabled()
    {
        string[] identifiers = ((IGC)this).GetGCIdentifiers();
        return identifiers.Contains(GCIdentifiers.Background);
    }

    private bool IsDatasEnabled()
    {
        string[] identifiers = ((IGC)this).GetGCIdentifiers();
        return identifiers.Contains(GCIdentifiers.DynamicHeapCount);
    }

    List<HandleData> IGC.GetHandles(HandleType[] types)
    {
        List<HandleType> typesList = types.ToList();
        typesList.Sort();
        List<HandleData> handles = new();
        TargetPointer handleTableMap = _target.ReadGlobalPointer(Constants.Globals.HandleTableMap);
        GCType gcType = GetGCType();
        uint tableCount = gcType switch
        {
            GCType.Workstation => 1,
            GCType.Server => _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.TotalCpuCount)),
            _ => 0 // unknown
        };
        while (handleTableMap != TargetPointer.Null)
        {
            Data.HandleTableMap handleTableData = _target.ProcessedData.GetOrAdd<Data.HandleTableMap>(handleTableMap);
            foreach (TargetPointer bucketPtr in handleTableData.BucketsPtr)
            {
                if (bucketPtr == TargetPointer.Null)
                    continue;

                Data.HandleTableBucket bucket = _target.ProcessedData.GetOrAdd<Data.HandleTableBucket>(bucketPtr);
                for (uint j = 0; j < tableCount; j++)
                {
                    TargetPointer handleTablePtr = _target.ReadPointer(bucket.Table + (ulong)(j * _target.PointerSize));
                    if (handleTablePtr == TargetPointer.Null)
                        continue;

                    Data.HandleTable handleTable = _target.ProcessedData.GetOrAdd<Data.HandleTable>(handleTablePtr);
                    if (handleTable.SegmentList == TargetPointer.Null)
                        continue;
                    foreach (HandleType type in typesList)
                    {
                        TargetPointer segmentPtr = handleTable.SegmentList;
                        do
                        {
                            Data.TableSegment tableSegment = _target.ProcessedData.GetOrAdd<Data.TableSegment>(segmentPtr);
                            segmentPtr = tableSegment.NextSegment;
                            GetHandlesForSegment(tableSegment, type, handles);
                        } while (segmentPtr != TargetPointer.Null);
                    }
                }
            }
            handleTableMap = handleTableData.Next;
        }
        return handles;
    }

    HandleType[] IGC.GetSupportedHandleTypes()
    {
        List<HandleType> supportedTypes =
        [
            HandleType.WeakShort,
            HandleType.WeakLong,
            HandleType.Strong,
            HandleType.Pinned,
            HandleType.Dependent,
            HandleType.WeakInteriorPointer
        ];
        if (_target.ReadGlobal<byte>(Constants.Globals.FeatureCOMInterop) != 0 || _target.ReadGlobal<byte>(Constants.Globals.FeatureComWrappers) != 0 || _target.ReadGlobal<byte>(Constants.Globals.FeatureObjCMarshal) != 0)
        {
            supportedTypes.Add(HandleType.RefCounted);
        }
        if (_target.ReadGlobal<byte>(Constants.Globals.FeatureJavaMarshal) != 0)
        {
            supportedTypes.Add(HandleType.CrossReference);
        }
        return supportedTypes.ToArray();
    }

    HandleType[] IGC.GetHandleTypes(uint[] types)
    {
        List<HandleType> handleTypes = new();
        foreach (uint type in types)
        {
            if (type >= _handleMaxInternalTypes)
                continue;

            HandleType? mappedType = type switch
            {
                (uint)HandleType_1.WeakShort => HandleType.WeakShort,
                (uint)HandleType_1.WeakLong => HandleType.WeakLong,
                (uint)HandleType_1.Strong => HandleType.Strong,
                (uint)HandleType_1.Pinned => HandleType.Pinned,
                (uint)HandleType_1.RefCounted => HandleType.RefCounted,
                (uint)HandleType_1.Dependent => HandleType.Dependent,
                (uint)HandleType_1.WeakInteriorPointer => HandleType.WeakInteriorPointer,
                (uint)HandleType_1.CrossReference => HandleType.CrossReference,
                _ => null,
            };

            if (mappedType is HandleType concreteType)
            {
                handleTypes.Add(concreteType);
            }
        }
        return handleTypes.ToArray();
    }

    private static uint GetInternalHandleType(HandleType type)
    {
        return type switch
        {
            HandleType.WeakShort => (uint)HandleType_1.WeakShort,
            HandleType.WeakLong => (uint)HandleType_1.WeakLong,
            HandleType.Strong => (uint)HandleType_1.Strong,
            HandleType.Pinned => (uint)HandleType_1.Pinned,
            HandleType.Dependent => (uint)HandleType_1.Dependent,
            HandleType.WeakInteriorPointer => (uint)HandleType_1.WeakInteriorPointer,
            HandleType.RefCounted => (uint)HandleType_1.RefCounted,
            HandleType.CrossReference => (uint)HandleType_1.CrossReference,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private void GetHandlesForSegment(Data.TableSegment tableSegment, HandleType type, List<HandleData> handles)
    {
        Debug.Assert(GetInternalHandleType(type) < _handleMaxInternalTypes);
        byte uBlock = tableSegment.RgTail[(int)GetInternalHandleType(type)];
        if (uBlock == _blockInvalid)
            return;
        uBlock = tableSegment.RgAllocation[uBlock];
        byte uHead = uBlock;
        // for each block in the segment for the given handle type
        do
        {
            GetHandlesForBlock(tableSegment, uBlock, type, handles);
            uBlock = tableSegment.RgAllocation[uBlock];
        } while (uBlock != uHead);
    }

    private void GetHandlesForBlock(Data.TableSegment tableSegment, byte uBlock, HandleType type, List<HandleData> handles)
    {
        for (uint k = 0; k < _handlesPerBlock; k++)
        {
            uint offset = uBlock * _handlesPerBlock + k;
            TargetPointer handleAddress = tableSegment.RgValue + offset * (uint)_target.PointerSize;
            TargetPointer handle = _target.ReadPointer(handleAddress);
            if (handle == TargetPointer.Null || handle == _debugDestroyedHandleValue)
                continue;
            handles.Add(CreateHandleData(handleAddress, uBlock, k, tableSegment, type));
        }
    }

    private static bool IsStrongReference(HandleType type) => type == HandleType.Strong || type == HandleType.Pinned;
    private static bool HasSecondary(HandleType type) => type == HandleType.Dependent || type == HandleType.WeakInteriorPointer || type == HandleType.CrossReference;
    private static bool IsRefCounted(HandleType type) => type == HandleType.RefCounted;

    private HandleData CreateHandleData(TargetPointer handleAddress, byte uBlock, uint intraBlockIndex, Data.TableSegment tableSegment, HandleType type)
    {
        HandleData handleData = default;
        handleData.Handle = handleAddress;
        handleData.Type = GetInternalHandleType(type);
        handleData.JupiterRefCount = 0;
        handleData.IsPegged = false;
        handleData.StrongReference = IsStrongReference(type);
        if (HasSecondary(type))
        {
            byte blockIndex = tableSegment.RgUserData[uBlock];
            if (blockIndex == _blockInvalid)
                handleData.Secondary = 0;
            else
            {
                uint offset = blockIndex * _handlesPerBlock + intraBlockIndex;
                handleData.Secondary = _target.ReadPointer(tableSegment.RgValue + offset * (uint)_target.PointerSize);
            }
        }
        else
        {
            handleData.Secondary = 0;
        }

        if (_target.ReadGlobal<byte>(Constants.Globals.FeatureCOMInterop) != 0 && IsRefCounted(type))
        {
            IObject obj = _target.Contracts.Object;
            TargetPointer handle = _target.ReadPointer(handleAddress);
            obj.GetBuiltInComData(handle, out _, out TargetPointer ccw, out _);
            if (ccw != TargetPointer.Null)
            {
                IBuiltInCOM builtInCOM = _target.Contracts.BuiltInCOM;
                handleData.RefCount = (uint)builtInCOM.GetRefCount(ccw);
                handleData.StrongReference = handleData.StrongReference || (handleData.RefCount > 0 && !builtInCOM.IsHandleWeak(ccw));
            }
        }

        return handleData;
    }

    IReadOnlyList<GCMemoryRegionData> IGC.GetHandleTableMemoryRegions()
    {
        List<GCMemoryRegionData> regions = new();
        uint handleSegmentSize = _target.ReadGlobal<uint>(Constants.Globals.HandleSegmentSize);
        uint tableCount = GetGCType() switch
        {
            GCType.Workstation => 1,
            GCType.Server => _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.TotalCpuCount)),
            _ => 0
        };

        int maxRegions = 8192;
        TargetPointer handleTableMap = _target.ReadGlobalPointer(Constants.Globals.HandleTableMap);
        while (handleTableMap != TargetPointer.Null && maxRegions >= 0)
        {
            Data.HandleTableMap map = _target.ProcessedData.GetOrAdd<Data.HandleTableMap>(handleTableMap);
            foreach (TargetPointer bucketPtr in map.BucketsPtr)
            {
                if (bucketPtr == TargetPointer.Null)
                    continue;

                Data.HandleTableBucket bucket = _target.ProcessedData.GetOrAdd<Data.HandleTableBucket>(bucketPtr);
                for (uint j = 0; j < tableCount; j++)
                {
                    TargetPointer handleTablePtr = _target.ReadPointer(bucket.Table + (ulong)(j * _target.PointerSize));
                    if (handleTablePtr == TargetPointer.Null)
                        continue;

                    Data.HandleTable handleTable = _target.ProcessedData.GetOrAdd<Data.HandleTable>(handleTablePtr);
                    if (handleTable.SegmentList == TargetPointer.Null)
                        continue;

                    TargetPointer segmentPtr = handleTable.SegmentList;
                    TargetPointer firstSegment = segmentPtr;
                    do
                    {
                        Data.TableSegment segment = _target.ProcessedData.GetOrAdd<Data.TableSegment>(segmentPtr);
                        regions.Add(new GCMemoryRegionData
                        {
                            Start = segmentPtr,
                            Size = handleSegmentSize,
                            Heap = (int)j,
                        });
                        segmentPtr = segment.NextSegment;
                        maxRegions--;
                    } while (segmentPtr != TargetPointer.Null && segmentPtr != firstSegment && maxRegions >= 0);
                }
            }
            handleTableMap = map.Next;
            maxRegions--;
        }

        return regions;
    }

    IReadOnlyList<GCMemoryRegionData> IGC.GetGCBookkeepingMemoryRegions()
    {
        List<GCMemoryRegionData> regions = new();

        if (!_target.TryReadGlobalPointer(Constants.Globals.BookkeepingStart, out TargetPointer? bookkeepingStartGlobal))
            return regions;

        TargetPointer bookkeepingStart = _target.ReadPointer(bookkeepingStartGlobal.Value);
        if (bookkeepingStart == TargetPointer.Null)
            return regions;

        uint cardTableInfoSize = _target.ReadGlobal<uint>(Constants.Globals.CardTableInfoSize);
        Data.CardTableInfo cardTableInfo = _target.ProcessedData.GetOrAdd<Data.CardTableInfo>(bookkeepingStart);

        if (cardTableInfo.Recount != 0 && cardTableInfo.Size.Value != 0)
        {
            regions.Add(new GCMemoryRegionData
            {
                Start = bookkeepingStart,
                Size = cardTableInfo.Size.Value,
            });
        }

        TargetPointer next = cardTableInfo.NextCardTable;
        TargetPointer firstNext = next;
        int maxRegions = 32;

        while (next != TargetPointer.Null && next > cardTableInfoSize && maxRegions > 0)
        {
            TargetPointer ctAddr = next - cardTableInfoSize;
            Data.CardTableInfo ct = _target.ProcessedData.GetOrAdd<Data.CardTableInfo>(ctAddr);

            if (ct.Recount != 0 && ct.Size.Value != 0)
            {
                regions.Add(new GCMemoryRegionData
                {
                    Start = ctAddr,
                    Size = ct.Size.Value,
                });
            }

            next = ct.NextCardTable;
            if (next == firstNext)
                break;

            maxRegions--;
        }

        return regions;
    }

    IReadOnlyList<GCMemoryRegionData> IGC.GetGCFreeRegions()
    {
        List<GCMemoryRegionData> regions = new();

        int countFreeRegionKinds = (int)_target.ReadGlobal<uint>(Constants.Globals.CountFreeRegionKinds);
        countFreeRegionKinds = Math.Min(countFreeRegionKinds, 16);
        uint regionFreeListSize = _target.GetTypeInfo(DataType.RegionFreeList).Size
            ?? throw new InvalidOperationException("RegionFreeList type has no size");

        // Global free huge regions
        if (_target.TryReadGlobalPointer(Constants.Globals.GlobalFreeHugeRegions, out TargetPointer? globalFreeHugePtr))
        {
            AddFreeList(globalFreeHugePtr.Value, FreeRegionKind.FreeGlobalHugeRegion, regions);
        }

        // Global regions to decommit
        if (_target.TryReadGlobalPointer(Constants.Globals.GlobalRegionsToDecommit, out TargetPointer? globalDecommitPtr))
        {
            for (int i = 0; i < countFreeRegionKinds; i++)
            {
                TargetPointer listAddr = globalDecommitPtr.Value + (ulong)(i * regionFreeListSize);
                AddFreeList(listAddr, FreeRegionKind.FreeGlobalRegion, regions);
            }
        }

        if (GetGCType() == GCType.Server)
        {
            uint heapCount = ((IGC)this).GetGCHeapCount();
            TargetPointer heapTable = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.Heaps));
            for (uint i = 0; i < heapCount; i++)
            {
                TargetPointer heapAddress = _target.ReadPointer(heapTable + (i * (uint)_target.PointerSize));
                if (heapAddress == TargetPointer.Null)
                    continue;

                Data.GCHeapSVR heap = _target.ProcessedData.GetOrAdd<Data.GCHeapSVR>(heapAddress);

                if (heap.FreeRegions is TargetPointer freeRegionsBase)
                {
                    for (int j = 0; j < countFreeRegionKinds; j++)
                    {
                        TargetPointer listAddr = freeRegionsBase + (ulong)(j * regionFreeListSize);
                        AddFreeList(listAddr, FreeRegionKind.FreeRegion, regions, (int)i);
                    }
                }

                if (heap.FreeableSohSegment is TargetPointer freeableSoh && freeableSoh != TargetPointer.Null)
                    AddSegmentList(freeableSoh, FreeRegionKind.FreeSohSegment, regions, (int)i);

                if (heap.FreeableUohSegment is TargetPointer freeableUoh && freeableUoh != TargetPointer.Null)
                    AddSegmentList(freeableUoh, FreeRegionKind.FreeUohSegment, regions, (int)i);
            }
        }
        else
        {
            // Workstation GC: free regions from globals
            if (_target.TryReadGlobalPointer(Constants.Globals.GCHeapFreeRegions, out TargetPointer? freeRegionsPtr))
            {
                for (int i = 0; i < countFreeRegionKinds; i++)
                {
                    TargetPointer listAddr = freeRegionsPtr.Value + (ulong)(i * regionFreeListSize);
                    AddFreeList(listAddr, FreeRegionKind.FreeRegion, regions);
                }
            }

            if (_target.TryReadGlobalPointer(Constants.Globals.GCHeapFreeableSohSegment, out TargetPointer? freeableSohPtr))
            {
                TargetPointer segPtr = _target.ReadPointer(freeableSohPtr.Value);
                if (segPtr != TargetPointer.Null)
                    AddSegmentList(segPtr, FreeRegionKind.FreeSohSegment, regions);
            }

            if (_target.TryReadGlobalPointer(Constants.Globals.GCHeapFreeableUohSegment, out TargetPointer? freeableUohPtr))
            {
                TargetPointer segPtr = _target.ReadPointer(freeableUohPtr.Value);
                if (segPtr != TargetPointer.Null)
                    AddSegmentList(segPtr, FreeRegionKind.FreeUohSegment, regions);
            }
        }

        return regions;
    }

    private void AddFreeList(TargetPointer freeListAddr, FreeRegionKind kind, List<GCMemoryRegionData> regions, int heap = 0)
    {
        Data.RegionFreeList freeList = _target.ProcessedData.GetOrAdd<Data.RegionFreeList>(freeListAddr);
        if (freeList.HeadFreeRegion != TargetPointer.Null)
            AddSegmentList(freeList.HeadFreeRegion, kind, regions, heap);
    }

    private void AddSegmentList(TargetPointer start, FreeRegionKind kind, List<GCMemoryRegionData> regions, int heap = 0)
    {
        int iterationMax = 2048;
        TargetPointer curr = start;
        while (curr != TargetPointer.Null)
        {
            Data.HeapSegment segment = _target.ProcessedData.GetOrAdd<Data.HeapSegment>(curr);
            if (segment.Mem != TargetPointer.Null)
            {
                ulong size = 0;
                if (segment.Mem < segment.Committed)
                    size = segment.Committed - segment.Mem;
                regions.Add(new GCMemoryRegionData
                {
                    Start = segment.Mem,
                    Size = size,
                    ExtraData = (ulong)kind,
                    Heap = heap,
                });
            }

            curr = segment.Next;
            if (curr == start)
                break;
            if (--iterationMax <= 0)
                break;
        }
    }
}
