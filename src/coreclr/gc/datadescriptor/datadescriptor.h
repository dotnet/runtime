// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stddef.h>

#include "common.h"
#include "gcenv.h"
#include "gc.h"
#include "gcscan.h"
#include "gchandletableimpl.h"
#include "gceventstatus.h"

#ifdef SERVER_GC
#define GC_NAMESPACE SVR
#else // SERVER_GC
#define GC_NAMESPACE WKS
#endif // SERVER_GC

// These files are designed to be used inside of the GC namespace.
// Without the namespace (WKS/SVR) there are naming conflicts.
namespace GC_NAMESPACE {

#include "gcimpl.h"
#include "gcpriv.h"

}

// On non-MSVC builds explicit specializations must be declared in the namespace the template was defined.
// Due to the gc being built into coreclr, cdac_data must be defined in the global scope.

#ifdef SERVER_GC
#define GC_HEAP_FIELD(cdacName, fieldName) static constexpr size_t cdacName = offsetof(GC_NAMESPACE::gc_heap, fieldName);
#else // !SERVER_GC
#define GC_HEAP_FIELD(cdacName, fieldName) static constexpr decltype(&GC_NAMESPACE::gc_heap::fieldName) cdacName = &GC_NAMESPACE::gc_heap::fieldName;
#endif // !SERVER_GC

template<>
struct cdac_data<GC_NAMESPACE::gc_heap>
{
#ifdef BACKGROUND_GC
    static constexpr c_gc_state* CurrentGCState = const_cast<c_gc_state*>(&GC_NAMESPACE::gc_heap::current_c_gc_state);
#endif // BACKGROUND_GC
#ifdef DYNAMIC_HEAP_COUNT
    static constexpr int* DynamicAdaptationMode = &GC_NAMESPACE::gc_heap::dynamic_adaptation_mode;
#endif // DYNAMIC_HEAP_COUNT
#ifdef SERVER_GC
    static constexpr GC_NAMESPACE::gc_heap*** Heaps = &GC_NAMESPACE::gc_heap::g_heaps;
#endif // SERVER_GC

    GC_HEAP_FIELD(MarkArray, mark_array)
    GC_HEAP_FIELD(NextSweepObj, next_sweep_obj)
    GC_HEAP_FIELD(BackgroundMinSavedAddr, background_saved_lowest_address)
    GC_HEAP_FIELD(BackgroundMaxSavedAddr, background_saved_highest_address)
    GC_HEAP_FIELD(AllocAllocated, alloc_allocated)
    GC_HEAP_FIELD(EphemeralHeapSegment, ephemeral_heap_segment)
    GC_HEAP_FIELD(CardTable, card_table)
    GC_HEAP_FIELD(FinalizeQueue, finalize_queue)

    GC_HEAP_FIELD(GenerationTable, generation_table)

#ifndef USE_REGIONS
    GC_HEAP_FIELD(SavedSweepEphemeralSeg, saved_sweep_ephemeral_seg)
    GC_HEAP_FIELD(SavedSweepEphemeralStart, saved_sweep_ephemeral_start)
#endif // !USE_REGIONS
};

template<>
struct cdac_data<GC_NAMESPACE::CFinalize>
{
    static constexpr size_t FillPointers = offsetof(GC_NAMESPACE::CFinalize, m_FillPointers);
    static constexpr size_t FillPointersLength = sizeof(GC_NAMESPACE::CFinalize::m_FillPointers) / sizeof(GC_NAMESPACE::CFinalize::m_FillPointers[0]);
};
