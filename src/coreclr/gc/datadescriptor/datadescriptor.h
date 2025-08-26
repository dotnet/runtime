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
template<>
struct cdac_data<GC_NAMESPACE::gc_heap>
{
#ifdef BACKGROUND_GC
    static constexpr c_gc_state* CurrentGCState = const_cast<c_gc_state*>(&GC_NAMESPACE::gc_heap::current_c_gc_state);
#endif // BACKGROUND_GC
#ifdef SERVER_GC
    static constexpr GC_NAMESPACE::gc_heap*** Heaps = &GC_NAMESPACE::gc_heap::g_heaps;

    static constexpr size_t MarkArray = offsetof(GC_NAMESPACE::gc_heap, mark_array);
    static constexpr size_t NextSweepObj = offsetof(GC_NAMESPACE::gc_heap, next_sweep_obj);
    static constexpr size_t BackgroundMinSavedAddr = offsetof(GC_NAMESPACE::gc_heap, background_saved_lowest_address);
    static constexpr size_t BackgroundMaxSavedAddr = offsetof(GC_NAMESPACE::gc_heap, background_saved_highest_address);
    static constexpr size_t AllocAllocated = offsetof(GC_NAMESPACE::gc_heap, alloc_allocated);
    static constexpr size_t EphemeralHeapSegment = offsetof(GC_NAMESPACE::gc_heap, ephemeral_heap_segment);
    static constexpr size_t CardTable = offsetof(GC_NAMESPACE::gc_heap, card_table);
    static constexpr size_t FinalizeQueue = offsetof(GC_NAMESPACE::gc_heap, finalize_queue);

    static constexpr size_t GenerationTable = offsetof(GC_NAMESPACE::gc_heap, generation_table);

#ifndef USE_REGIONS
    static constexpr size_t SavedSweepEphemeralSeg = offsetof(GC_NAMESPACE::gc_heap, saved_sweep_ephemeral_seg);
    static constexpr size_t SavedSweepEphemeralStart = offsetof(GC_NAMESPACE::gc_heap, saved_sweep_ephemeral_start);
#endif // !USE_REGIONS
#endif // SERVER_GC
};

template<>
struct cdac_data<GC_NAMESPACE::CFinalize>
{
    static constexpr size_t FillPointers = offsetof(GC_NAMESPACE::CFinalize, m_FillPointers);
    static constexpr size_t FillPointersLength = sizeof(GC_NAMESPACE::CFinalize::m_FillPointers) / sizeof(GC_NAMESPACE::CFinalize::m_FillPointers[0]);
};
