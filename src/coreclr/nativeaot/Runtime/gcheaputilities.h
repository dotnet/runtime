// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCHEAPUTILITIES_H_
#define _GCHEAPUTILITIES_H_

#include "gcinterface.h"
#include "daccess.h"
#include "writebarrier.h"

// The singular heap instance.
GPTR_DECL(IGCHeap, g_pGCHeap);

GVAL_DECL(GCHeapType, g_heap_type);

#ifndef DACCESS_COMPILE
extern WriteBarrierFunctions g_writeBarrierFunctions;
#endif // !DACCESS_COMPILE

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
    static void InitializeWriteBarrierFunctions(IGCHeap* gcHeap);

    inline static bool IsInGCHeap(void* address)
    {
        return g_writeBarrierFunctions.is_in_gc_heap(
            g_writeBarrierFunctions.context,
            address);
    }

    inline static void BulkMoveWithWriteBarrier(
        void* destination,
        const void* source,
        size_t length)
    {
        g_writeBarrierFunctions.bulk_move_with_write_barrier(
            destination,
            source,
            length);
    }

    static void SetWriteBarrierHelpers(const WriteBarrierHelperDescriptor& helpers);
    static bool IsIPInWriteBarrierHelper(uintptr_t instructionPointer);

    // Initializes the GC
    static HRESULT InitializeGC();

    // Initializes a non-standalone GC.
    static HRESULT InitializeDefaultGC();

    // Initializes a standalone GC.
    static HRESULT InitializeStandaloneGC();

    // Records a change in eventing state. This ultimately will inform the GC that it needs to be aware
    // of new events being enabled.
    static void RecordEventStateChange(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level);
#endif // DACCESS_COMPILE

private:
    // This class should never be instantiated.
    GCHeapUtilities() = delete;
};

#endif // _GCHEAPUTILITIES_H_
