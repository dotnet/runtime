// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// No include guards. This file is included multiple times.
//
// This file is compiled using the target architecture.  Preprocessor defines for the target
// platform will be available.  It is ok to use `#ifdef`.


CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

#ifdef SERVER_GC
CDAC_TYPE_BEGIN(GCHeap)
CDAC_TYPE_INDETERMINATE(GCHeap)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, MarkArray, cdac_data<GC_NAMESPACE::gc_heap>::MarkArray)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, NextSweepObj, cdac_data<GC_NAMESPACE::gc_heap>::NextSweepObj)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, BackgroundMinSavedAddr, cdac_data<GC_NAMESPACE::gc_heap>::BackgroundMinSavedAddr)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, BackgroundMaxSavedAddr, cdac_data<GC_NAMESPACE::gc_heap>::BackgroundMaxSavedAddr)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, AllocAllocated, cdac_data<GC_NAMESPACE::gc_heap>::AllocAllocated)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, EphemeralHeapSegment, cdac_data<GC_NAMESPACE::gc_heap>::EphemeralHeapSegment)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, CardTable, cdac_data<GC_NAMESPACE::gc_heap>::CardTable)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, FinalizeQueue, cdac_data<GC_NAMESPACE::gc_heap>::FinalizeQueue)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, GenerationTable, cdac_data<GC_NAMESPACE::gc_heap>::GenerationTable)
#ifndef USE_REGIONS
CDAC_TYPE_FIELD(GCHeap, T_POINTER, SavedSweepEphemeralSeg, cdac_data<GC_NAMESPACE::gc_heap>::SavedSweepEphemeralSeg)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, SavedSweepEphemeralStart, cdac_data<GC_NAMESPACE::gc_heap>::SavedSweepEphemeralStart)
#endif // !USE_REGIONS
CDAC_TYPE_FIELD(GCHeap, TYPE(OomHistory), OomData, cdac_data<GC_NAMESPACE::gc_heap>::OomData)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, InternalRootArray, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArray)
CDAC_TYPE_FIELD(GCHeap, T_NUINT, InternalRootArrayIndex, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArrayIndex)
CDAC_TYPE_FIELD(GCHeap, T_INT32, HeapAnalyzeSuccess, cdac_data<GC_NAMESPACE::gc_heap>::HeapAnalyzeSuccess)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, InterestingData, cdac_data<GC_NAMESPACE::gc_heap>::InterestingData)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, CompactReasons, cdac_data<GC_NAMESPACE::gc_heap>::CompactReasons)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, ExpandMechanisms, cdac_data<GC_NAMESPACE::gc_heap>::ExpandMechanisms)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, InterestingMechanismBits, cdac_data<GC_NAMESPACE::gc_heap>::InterestingMechanismBits)
#ifdef BACKGROUND_GC
CDAC_TYPE_FIELD(GCHeap, T_POINTER, FreeableSohSegment, cdac_data<GC_NAMESPACE::gc_heap>::FreeableSohSegment)
CDAC_TYPE_FIELD(GCHeap, T_POINTER, FreeableUohSegment, cdac_data<GC_NAMESPACE::gc_heap>::FreeableUohSegment)
#endif // BACKGROUND_GC
#ifdef USE_REGIONS
CDAC_TYPE_FIELD(GCHeap, T_POINTER, FreeRegions, cdac_data<GC_NAMESPACE::gc_heap>::FreeRegions)
#endif // USE_REGIONS
CDAC_TYPE_END(GCHeap)
#endif // SERVER_GC

CDAC_TYPE_BEGIN(Generation)
CDAC_TYPE_SIZE(sizeof(GC_NAMESPACE::generation))
CDAC_TYPE_FIELD(Generation, EXTERN_TYPE(GCAllocContext), AllocationContext, offsetof(GC_NAMESPACE::generation, allocation_context))
CDAC_TYPE_FIELD(Generation, T_POINTER, StartSegment, offsetof(GC_NAMESPACE::generation, start_segment))
#ifndef USE_REGIONS
CDAC_TYPE_FIELD(Generation, T_POINTER, AllocationStart, offsetof(GC_NAMESPACE::generation, allocation_start))
#endif // !USE_REGIONS
CDAC_TYPE_END(Generation)

CDAC_TYPE_BEGIN(CFinalize)
CDAC_TYPE_INDETERMINATE(CFinalize)
CDAC_TYPE_FIELD(CFinalize, T_POINTER, FillPointers, cdac_data<GC_NAMESPACE::CFinalize>::FillPointers)
CDAC_TYPE_END(CFinalize)

CDAC_TYPE_BEGIN(HeapSegment)
CDAC_TYPE_INDETERMINATE(HeapSegment)
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, Allocated, offsetof(GC_NAMESPACE::heap_segment, allocated))
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, Committed, offsetof(GC_NAMESPACE::heap_segment, committed))
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, Reserved, offsetof(GC_NAMESPACE::heap_segment, reserved))
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, Used, offsetof(GC_NAMESPACE::heap_segment, used))
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, Mem, offsetof(GC_NAMESPACE::heap_segment, mem))
CDAC_TYPE_FIELD(HeapSegment, T_NUINT, Flags, offsetof(GC_NAMESPACE::heap_segment, flags))
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, Next, offsetof(GC_NAMESPACE::heap_segment, next))
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, BackgroundAllocated, offsetof(GC_NAMESPACE::heap_segment, background_allocated))
#ifdef MULTIPLE_HEAPS
CDAC_TYPE_FIELD(HeapSegment, T_POINTER, Heap, offsetof(GC_NAMESPACE::heap_segment, heap))
#endif // MULTIPLE_HEAPS
CDAC_TYPE_END(HeapSegment)

CDAC_TYPE_BEGIN(OomHistory)
CDAC_TYPE_INDETERMINATE(OomHistory)
CDAC_TYPE_FIELD(OomHistory, T_INT32, Reason, offsetof(oom_history, reason))
CDAC_TYPE_FIELD(OomHistory, T_NUINT, AllocSize, offsetof(oom_history, alloc_size))
CDAC_TYPE_FIELD(OomHistory, T_POINTER, Reserved, offsetof(oom_history, reserved))
CDAC_TYPE_FIELD(OomHistory, T_POINTER, Allocated, offsetof(oom_history, allocated))
CDAC_TYPE_FIELD(OomHistory, T_NUINT, GcIndex, offsetof(oom_history, gc_index))
CDAC_TYPE_FIELD(OomHistory, T_INT32, Fgm, offsetof(oom_history, fgm))
CDAC_TYPE_FIELD(OomHistory, T_NUINT, Size, offsetof(oom_history, size))
CDAC_TYPE_FIELD(OomHistory, T_NUINT, AvailablePagefileMb, offsetof(oom_history, available_pagefile_mb))
CDAC_TYPE_FIELD(OomHistory, T_UINT32, LohP, offsetof(oom_history, loh_p))
CDAC_TYPE_END(OomHistory)

CDAC_TYPE_BEGIN(HandleTableMap)
CDAC_TYPE_INDETERMINATE(HandleTableMap)
CDAC_TYPE_FIELD(HandleTableMap, T_POINTER, BucketsPtr, offsetof(HandleTableMap, pBuckets))
CDAC_TYPE_FIELD(HandleTableMap, T_POINTER, Next, offsetof(HandleTableMap, pNext))
CDAC_TYPE_END(HandleTableMap)

CDAC_TYPE_BEGIN(HandleTableBucket)
CDAC_TYPE_INDETERMINATE(HandleTableBucket)
CDAC_TYPE_FIELD(HandleTableBucket, T_POINTER, Table, offsetof(HandleTableBucket, pTable))
CDAC_TYPE_END(HandleTableBucket)

CDAC_TYPE_BEGIN(HandleTable)
CDAC_TYPE_INDETERMINATE(HandleTable)
CDAC_TYPE_FIELD(HandleTable, T_POINTER, SegmentList, offsetof(HandleTable, pSegmentList))
CDAC_TYPE_END(HandleTable)

CDAC_TYPE_BEGIN(TableSegment)
CDAC_TYPE_INDETERMINATE(TableSegment)
CDAC_TYPE_FIELD(TableSegment, T_POINTER, NextSegment, offsetof(TableSegment, pNextSegment))
CDAC_TYPE_FIELD(TableSegment, T_ARRAY(T_UINT8), RgAllocation, offsetof(TableSegment, rgAllocation))
CDAC_TYPE_FIELD(TableSegment, T_ARRAY(T_UINT8), RgTail, offsetof(TableSegment, rgTail))
CDAC_TYPE_FIELD(TableSegment, T_ARRAY(T_UINT8), RgValue, offsetof(TableSegment, rgValue))
CDAC_TYPE_FIELD(TableSegment, T_ARRAY(T_UINT8), RgUserData, offsetof(TableSegment, rgUserData))
CDAC_TYPE_END(TableSegment)

CDAC_TYPE_BEGIN(CardTableInfo)
CDAC_TYPE_SIZE(sizeof(dac_card_table_info))
CDAC_TYPE_FIELD(CardTableInfo, T_UINT32, Recount, offsetof(dac_card_table_info, recount))
CDAC_TYPE_FIELD(CardTableInfo, T_NUINT, Size, offsetof(dac_card_table_info, size))
CDAC_TYPE_FIELD(CardTableInfo, T_POINTER, NextCardTable, offsetof(dac_card_table_info, next_card_table))
CDAC_TYPE_END(CardTableInfo)

CDAC_TYPE_BEGIN(RegionFreeList)
CDAC_TYPE_SIZE(sizeof(dac_region_free_list))
CDAC_TYPE_FIELD(RegionFreeList, T_POINTER, HeadFreeRegion, offsetof(dac_region_free_list, head_free_region))
CDAC_TYPE_END(RegionFreeList)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()

CDAC_GLOBAL(TotalGenerationCount, T_UINT32, (uint32_t)total_generation_count)
CDAC_GLOBAL(CFinalizeFillPointersLength, T_UINT32, (uint32_t)cdac_data<GC_NAMESPACE::CFinalize>::FillPointersLength)
CDAC_GLOBAL(InterestingDataLength, T_UINT32, NUM_GC_DATA_POINTS)
CDAC_GLOBAL(CompactReasonsLength, T_UINT32, MAX_COMPACT_REASONS_COUNT)
CDAC_GLOBAL(ExpandMechanismsLength, T_UINT32, MAX_EXPAND_MECHANISMS_COUNT)
CDAC_GLOBAL(InterestingMechanismBitsLength, T_UINT32, MAX_GC_MECHANISM_BITS_COUNT)
CDAC_GLOBAL(GlobalMechanismsLength, T_UINT32, MAX_GLOBAL_GC_MECHANISMS_COUNT)
CDAC_GLOBAL(InitialHandleTableArraySize, T_UINT32, INITIAL_HANDLE_TABLE_ARRAY_SIZE)
#ifdef DEBUG_DestroyedHandleValue
CDAC_GLOBAL(DebugDestroyedHandleValue, T_NUINT, (uintptr_t)DEBUG_DestroyedHandleValue)
#else
CDAC_GLOBAL(DebugDestroyedHandleValue, T_NUINT, (uintptr_t)(0))
#endif
CDAC_GLOBAL(HandleSegmentSize, T_UINT32, HANDLE_SEGMENT_SIZE)
CDAC_GLOBAL(HandleBlocksPerSegment, T_UINT32, HANDLE_BLOCKS_PER_SEGMENT)
CDAC_GLOBAL(HandleMaxInternalTypes, T_UINT32, HANDLE_MAX_INTERNAL_TYPES)
CDAC_GLOBAL(HandlesPerBlock, T_UINT32, HANDLE_HANDLES_PER_BLOCK)
CDAC_GLOBAL(BlockInvalid, T_UINT8, BLOCK_INVALID)
CDAC_GLOBAL(CardTableInfoSize, T_UINT32, (uint32_t)sizeof(dac_card_table_info))
#ifdef USE_REGIONS
CDAC_GLOBAL(CountFreeRegionKinds, T_UINT32, (uint32_t)FREE_REGION_KINDS)
#endif // USE_REGIONS


#ifndef SERVER_GC
CDAC_GLOBAL_POINTER(GCHeapMarkArray, cdac_data<GC_NAMESPACE::gc_heap>::MarkArray)
CDAC_GLOBAL_POINTER(GCHeapNextSweepObj, cdac_data<GC_NAMESPACE::gc_heap>::NextSweepObj)
CDAC_GLOBAL_POINTER(GCHeapBackgroundMinSavedAddr, cdac_data<GC_NAMESPACE::gc_heap>::BackgroundMinSavedAddr)
CDAC_GLOBAL_POINTER(GCHeapBackgroundMaxSavedAddr, cdac_data<GC_NAMESPACE::gc_heap>::BackgroundMaxSavedAddr)
CDAC_GLOBAL_POINTER(GCHeapAllocAllocated, cdac_data<GC_NAMESPACE::gc_heap>::AllocAllocated)
CDAC_GLOBAL_POINTER(GCHeapEphemeralHeapSegment, cdac_data<GC_NAMESPACE::gc_heap>::EphemeralHeapSegment)
CDAC_GLOBAL_POINTER(GCHeapCardTable, cdac_data<GC_NAMESPACE::gc_heap>::CardTable)
CDAC_GLOBAL_POINTER(GCHeapFinalizeQueue, cdac_data<GC_NAMESPACE::gc_heap>::FinalizeQueue)
CDAC_GLOBAL_POINTER(GCHeapGenerationTable, cdac_data<GC_NAMESPACE::gc_heap>::GenerationTable)
#ifndef USE_REGIONS
CDAC_GLOBAL_POINTER(GCHeapSavedSweepEphemeralSeg, cdac_data<GC_NAMESPACE::gc_heap>::SavedSweepEphemeralSeg)
CDAC_GLOBAL_POINTER(GCHeapSavedSweepEphemeralStart, cdac_data<GC_NAMESPACE::gc_heap>::SavedSweepEphemeralStart)
#endif // !USE_REGIONS
CDAC_GLOBAL_POINTER(GCHeapOomData, cdac_data<GC_NAMESPACE::gc_heap>::OomData)
CDAC_GLOBAL_POINTER(GCHeapInternalRootArray, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArray)
CDAC_GLOBAL_POINTER(GCHeapInternalRootArrayIndex, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArrayIndex)
CDAC_GLOBAL_POINTER(GCHeapHeapAnalyzeSuccess, cdac_data<GC_NAMESPACE::gc_heap>::HeapAnalyzeSuccess)
CDAC_GLOBAL_POINTER(GCHeapInterestingData, cdac_data<GC_NAMESPACE::gc_heap>::InterestingData)
CDAC_GLOBAL_POINTER(GCHeapCompactReasons, cdac_data<GC_NAMESPACE::gc_heap>::CompactReasons)
CDAC_GLOBAL_POINTER(GCHeapExpandMechanisms, cdac_data<GC_NAMESPACE::gc_heap>::ExpandMechanisms)
CDAC_GLOBAL_POINTER(GCHeapInterestingMechanismBits, cdac_data<GC_NAMESPACE::gc_heap>::InterestingMechanismBits)
#ifdef BACKGROUND_GC
CDAC_GLOBAL_POINTER(GCHeapFreeableSohSegment, cdac_data<GC_NAMESPACE::gc_heap>::FreeableSohSegment)
CDAC_GLOBAL_POINTER(GCHeapFreeableUohSegment, cdac_data<GC_NAMESPACE::gc_heap>::FreeableUohSegment)
#endif // BACKGROUND_GC
#ifdef USE_REGIONS
CDAC_GLOBAL_POINTER(GCHeapFreeRegions, cdac_data<GC_NAMESPACE::gc_heap>::FreeRegions)
#endif // USE_REGIONS
#endif // !SERVER_GC

CDAC_GLOBAL_POINTER(HandleTableMap, &g_HandleTableMap)
CDAC_GLOBAL_POINTER(BookkeepingStart, cdac_data<GC_NAMESPACE::gc_heap>::BookkeepingStart)
#ifdef USE_REGIONS
CDAC_GLOBAL_POINTER(GlobalFreeHugeRegions, cdac_data<GC_NAMESPACE::gc_heap>::GlobalFreeHugeRegions)
CDAC_GLOBAL_POINTER(GlobalRegionsToDecommit, cdac_data<GC_NAMESPACE::gc_heap>::GlobalRegionsToDecommit)
#endif // USE_REGIONS

#ifdef SERVER_GC
#define GC_TYPE server
#else // SERVER_GC
#define GC_TYPE workstation
#endif // SERVER_GC

#ifdef USE_REGIONS
#define HEAP_TYPE regions
#else // USE_REGIONS
#define HEAP_TYPE segments
#endif // USE_REGIONS

#ifdef BACKGROUND_GC
#define BACKGROUND_TYPE background
#else // BACKGROUND_GC
#define BACKGROUND_TYPE
#endif // BACKGROUND_GC

#ifdef DYNAMIC_HEAP_COUNT
#define DYNAMIC_HEAP_TYPE dynamic_heap
#else // DYNAMIC_HEAP_COUNT
#define DYNAMIC_HEAP_TYPE
#endif // DYNAMIC_HEAP_COUNT

// CDAC_GLOBAL_STRING takes a single value argument.
// To avoid issues with commas in the string we wrap the input string in a macro.
#define GC_IDENTIFIER(...) __VA_ARGS__ // GC_IDENTIFIER(gc, heap) expands to: gc, heap
CDAC_GLOBAL_STRING(GCIdentifiers, GC_IDENTIFIER(GC_TYPE, HEAP_TYPE, BACKGROUND_TYPE, DYNAMIC_HEAP_TYPE))

CDAC_GLOBAL_POINTER(MaxGeneration, &::g_max_generation)
CDAC_GLOBAL_POINTER(StructureInvalidCount, &GCScan::m_GcStructuresInvalidCnt)

#ifdef SERVER_GC
CDAC_GLOBAL_POINTER(NumHeaps, &GC_NAMESPACE::gc_heap::n_heaps)
CDAC_GLOBAL_POINTER(Heaps, cdac_data<GC_NAMESPACE::gc_heap>::Heaps)
CDAC_GLOBAL_POINTER(TotalCpuCount, &g_totalCpuCount)
#endif // SERVER_GC

#ifdef BACKGROUND_GC
CDAC_GLOBAL_POINTER(CurrentGCState, cdac_data<GC_NAMESPACE::gc_heap>::CurrentGCState)
#endif // BACKGROUND_GC

#ifdef DYNAMIC_HEAP_COUNT
CDAC_GLOBAL_POINTER(DynamicAdaptationMode, cdac_data<GC_NAMESPACE::gc_heap>::DynamicAdaptationMode)
#endif // DYNAMIC_HEAP_COUNT

#ifdef GC_CONFIG_DRIVEN
CDAC_GLOBAL_POINTER(GCGlobalMechanisms, &gc_global_mechanisms)
#endif // GC_CONFIG_DRIVEN

CDAC_GLOBAL_CONTRACT(GC, c1)

CDAC_GLOBALS_END()
