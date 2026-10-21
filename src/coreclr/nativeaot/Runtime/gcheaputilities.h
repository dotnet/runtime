// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCHEAPUTILITIES_H_
#define _GCHEAPUTILITIES_H_

#include "gcinterface.h"
#include "daccess.h"

// The singular heap instance.
GPTR_DECL(IGCHeap, g_pGCHeap);

// Whether the loaded collector's WriteBarrierParameters contains the 5.9 tail.
extern bool g_write_barrier_parameters_include_shape;

// Whether the loaded collector's WriteBarrierParameters contains the 5.10 tail.
extern bool g_write_barrier_parameters_include_complete_store;

// Whether the loaded collector's WriteBarrierParameters contains the 5.11 tail.
extern bool g_write_barrier_parameters_include_epoch_reset;

// Whether the loaded collector's WriteBarrierParameters contains the 5.12 tail.
extern bool g_write_barrier_parameters_include_bulk_scan;

#ifndef DACCESS_COMPILE
extern "C" {
#endif // !DACCESS_COMPILE
GPTR_DECL(uint8_t,g_lowest_address);
GPTR_DECL(uint8_t,g_highest_address);
GPTR_DECL(uint32_t,g_card_table);
GVAL_DECL(GCHeapType, g_heap_type);
#ifndef DACCESS_COMPILE
}
#endif // !DACCESS_COMPILE

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
extern "C" uint32_t* g_card_bundle_table;
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

extern "C" uint8_t* g_ephemeral_low;
extern "C" uint8_t* g_ephemeral_high;

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
extern "C" bool g_sw_ww_enabled_for_gc_heap;
extern "C" uint8_t* g_write_watch_table;
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


// g_gc_dac_vars is a structure of pointers to GC globals that the
// DAC uses. It is not exposed directly to the DAC.
extern GcDacVars g_gc_dac_vars;

// Instead of exposing g_gc_dac_vars to the DAC, a pointer to it
// is exposed here (g_gcDacGlobals). The reason for this is to avoid
// a problem in which a debugger attaches to a program while the program
// is in the middle of initializing the GC DAC vars - if the "publishing"
// of DAC vars isn't atomic, the debugger could see a partially initialized
// GcDacVars structure.
//
// Instead, the debuggee "publishes" GcDacVars by assigning a pointer to g_gc_dac_vars
// to this global, and the DAC will read this global.
typedef DPTR(GcDacVars) PTR_GcDacVars;
GPTR_DECL(GcDacVars, g_gcDacGlobals);

// GCHeapUtilities provides a number of static methods
// that operate on the global heap instance. It can't be
// instantiated.
class GCHeapUtilities {
public:
    // Retrieves the GC heap.
    inline static IGCHeap* GetGCHeap()
    {
        assert(g_pGCHeap != nullptr);
        return g_pGCHeap;
    }

    // Returns true if the heap has been initialized, false otherwise.
    inline static bool IsGCHeapInitialized()
    {
        return g_pGCHeap != nullptr;
    }

    // Returns true if a the heap is initialized and a garbage collection
    // is in progress, false otherwise.
    inline static BOOL IsGCInProgress(BOOL bConsiderGCStart = FALSE)
    {
        return GetGCHeap()->IsGCInProgressHelper(bConsiderGCStart);
    }

    // Returns true if the held GC heap is a Server GC heap, false otherwise.
    inline static bool IsServerHeap()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_SVR_GC
        _ASSERTE(g_heap_type != GC_HEAP_INVALID);
        return (g_heap_type == GC_HEAP_SVR);
#else
        return false;
#endif // FEATURE_SVR_GC
    }

#ifndef DACCESS_COMPILE
    // Initializes the GC
    static HRESULT InitializeGC();

    // Initializes a non-standalone GC.
    static HRESULT InitializeDefaultGC();

    // Initializes a standalone GC.
    static HRESULT InitializeStandaloneGC();

    // Accepts the disabled descriptor and rejects enabled allocation
    // notification for precompiled NativeAOT images.
    static HRESULT ConfigureAllocationNotification(IGCHeap* gcHeap);

    // Accepts object-reference enumeration. NativeAOT MethodTables are not collectible.
    static HRESULT ConfigureObjectReferenceEnumeration(IGCHeap* gcHeap);

    // Validates and completes the collector's object-header bit request.
    static HRESULT ConfigureObjectHeaderBits(IGCHeap* gcHeap);

    // Records a change in eventing state. This ultimately will inform the GC that it needs to be aware
    // of new events being enabled.
    static void RecordEventStateChange(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level);
#endif // DACCESS_COMPILE

private:
    // This class should never be instantiated.
    GCHeapUtilities() = delete;
};

#endif // _GCHEAPUTILITIES_H_
