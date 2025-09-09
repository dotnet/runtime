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
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, MarkArray, cdac_data<GC_NAMESPACE::gc_heap>::MarkArray)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, NextSweepObj, cdac_data<GC_NAMESPACE::gc_heap>::NextSweepObj)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, BackgroundMinSavedAddr, cdac_data<GC_NAMESPACE::gc_heap>::BackgroundMinSavedAddr)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, BackgroundMaxSavedAddr, cdac_data<GC_NAMESPACE::gc_heap>::BackgroundMaxSavedAddr)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, AllocAllocated, cdac_data<GC_NAMESPACE::gc_heap>::AllocAllocated)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, EphemeralHeapSegment, cdac_data<GC_NAMESPACE::gc_heap>::EphemeralHeapSegment)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, CardTable, cdac_data<GC_NAMESPACE::gc_heap>::CardTable)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, FinalizeQueue, cdac_data<GC_NAMESPACE::gc_heap>::FinalizeQueue)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, GenerationTable, cdac_data<GC_NAMESPACE::gc_heap>::GenerationTable)
#ifndef USE_REGIONS
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, SavedSweepEphemeralSeg, cdac_data<GC_NAMESPACE::gc_heap>::SavedSweepEphemeralSeg)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, SavedSweepEphemeralStart, cdac_data<GC_NAMESPACE::gc_heap>::SavedSweepEphemeralStart)
#endif // !USE_REGIONS
CDAC_TYPE_FIELD(GCHeap, /*oom_history*/, OOMData, cdac_data<GC_NAMESPACE::gc_heap>::OOMData)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, InternalRootArray, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArray)
CDAC_TYPE_FIELD(GCHeap, /*nuint*/, InternalRootArrayIndex, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArrayIndex)
CDAC_TYPE_FIELD(GCHeap, /*int*/, HeapAnalyzeSuccess, cdac_data<GC_NAMESPACE::gc_heap>::HeapAnalyzeSuccess)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, InterestingData, cdac_data<GC_NAMESPACE::gc_heap>::InterestingData)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, CompactReasons, cdac_data<GC_NAMESPACE::gc_heap>::CompactReasons)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, ExpandMechanisms, cdac_data<GC_NAMESPACE::gc_heap>::ExpandMechanisms)
CDAC_TYPE_FIELD(GCHeap, /*pointer*/, InterestingMechanismBits, cdac_data<GC_NAMESPACE::gc_heap>::InterestingMechanismBits)
CDAC_TYPE_END(GCHeap)
#endif // SERVER_GC

CDAC_TYPE_BEGIN(Generation)
CDAC_TYPE_SIZE(sizeof(GC_NAMESPACE::generation))
CDAC_TYPE_FIELD(Generation, /*AllocContext*/, AllocationContext, offsetof(GC_NAMESPACE::generation, allocation_context))
CDAC_TYPE_FIELD(Generation, /*pointer*/, StartSegment, offsetof(GC_NAMESPACE::generation, start_segment))
#ifndef USE_REGIONS
CDAC_TYPE_FIELD(Generation, /*pointer*/, AllocationStart, offsetof(GC_NAMESPACE::generation, allocation_start))
#endif // !USE_REGIONS
CDAC_TYPE_END(Generation)

CDAC_TYPE_BEGIN(CFinalize)
CDAC_TYPE_INDETERMINATE(CFinalize)
CDAC_TYPE_FIELD(CFinalize, /*pointer*/, FillPointers, cdac_data<GC_NAMESPACE::CFinalize>::FillPointers)
CDAC_TYPE_END(CFinalize)

CDAC_TYPE_BEGIN(HeapSegment)
CDAC_TYPE_INDETERMINATE(HeapSegment)
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, Allocated, offsetof(GC_NAMESPACE::heap_segment, allocated))
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, Committed, offsetof(GC_NAMESPACE::heap_segment, committed))
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, Reserved, offsetof(GC_NAMESPACE::heap_segment, reserved))
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, Used, offsetof(GC_NAMESPACE::heap_segment, used))
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, Mem, offsetof(GC_NAMESPACE::heap_segment, mem))
CDAC_TYPE_FIELD(HeapSegment, /*nuint*/, Flags, offsetof(GC_NAMESPACE::heap_segment, flags))
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, Next, offsetof(GC_NAMESPACE::heap_segment, next))
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, BackgroundAllocated, offsetof(GC_NAMESPACE::heap_segment, background_allocated))
#ifdef MULTIPLE_HEAPS
CDAC_TYPE_FIELD(HeapSegment, /*pointer*/, Heap, offsetof(GC_NAMESPACE::heap_segment, heap))
#endif // MULTIPLE_HEAPS
CDAC_TYPE_END(HeapSegment)

CDAC_TYPE_BEGIN(OOMHistory)
CDAC_TYPE_INDETERMINATE(OOMHistory)
CDAC_TYPE_FIELD(OOMHistory, /*int32*/, Reason, offsetof(oom_history, reason))
CDAC_TYPE_FIELD(OOMHistory, /*nuint*/, AllocSize, offsetof(oom_history, alloc_size))
CDAC_TYPE_FIELD(OOMHistory, /*pointer*/, Reserved, offsetof(oom_history, reserved))
CDAC_TYPE_FIELD(OOMHistory, /*pointer*/, Allocated, offsetof(oom_history, allocated))
CDAC_TYPE_FIELD(OOMHistory, /*nuint*/, GcIndex, offsetof(oom_history, gc_index))
CDAC_TYPE_FIELD(OOMHistory, /*int32*/, Fgm, offsetof(oom_history, fgm))
CDAC_TYPE_FIELD(OOMHistory, /*nuint*/, Size, offsetof(oom_history, size))
CDAC_TYPE_FIELD(OOMHistory, /*nuint*/, AvailablePagefileMb, offsetof(oom_history, available_pagefile_mb))
CDAC_TYPE_FIELD(OOMHistory, /*uint32*/, LohP, offsetof(oom_history, loh_p))
CDAC_TYPE_END(OOMHistory)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()

CDAC_GLOBAL(TotalGenerationCount, /*uint32*/, (uint32_t)total_generation_count)
CDAC_GLOBAL(CFinalizeFillPointersLength, /*uint32*/, (uint32_t)cdac_data<GC_NAMESPACE::CFinalize>::FillPointersLength)
CDAC_GLOBAL(InterestingDataLength, /*uint32*/, NUM_GC_DATA_POINTS)
CDAC_GLOBAL(CompactReasonsLength, /*uint32*/, MAX_COMPACT_REASONS_COUNT)
CDAC_GLOBAL(ExpandMechanismsLength, /*uint32*/, MAX_EXPAND_MECHANISMS_COUNT)
CDAC_GLOBAL(InterestingMechanismBitsLength, /*uint32*/, MAX_GC_MECHANISM_BITS_COUNT)
CDAC_GLOBAL(GlobalMechanismsLength, /*uint32*/, MAX_GLOBAL_GC_MECHANISMS_COUNT)


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
CDAC_GLOBAL_POINTER(GCHeapOOMData, cdac_data<GC_NAMESPACE::gc_heap>::OOMData)
CDAC_GLOBAL_POINTER(GCHeapInternalRootArray, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArray)
CDAC_GLOBAL_POINTER(GCHeapInternalRootArrayIndex, cdac_data<GC_NAMESPACE::gc_heap>::InternalRootArrayIndex)
CDAC_GLOBAL_POINTER(GCHeapHeapAnalyzeSuccess, cdac_data<GC_NAMESPACE::gc_heap>::HeapAnalyzeSuccess)
CDAC_GLOBAL_POINTER(GCHeapInterestingData, cdac_data<GC_NAMESPACE::gc_heap>::InterestingData)
CDAC_GLOBAL_POINTER(GCHeapCompactReasons, cdac_data<GC_NAMESPACE::gc_heap>::CompactReasons)
CDAC_GLOBAL_POINTER(GCHeapExpandMechanisms, cdac_data<GC_NAMESPACE::gc_heap>::ExpandMechanisms)
CDAC_GLOBAL_POINTER(GCHeapInterestingMechanismBits, cdac_data<GC_NAMESPACE::gc_heap>::InterestingMechanismBits)
#endif // !SERVER_GC

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

CDAC_GLOBALS_END()
