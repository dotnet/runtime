// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCHEAPUTILITIES_H_
#define _GCHEAPUTILITIES_H_

#include "gcinterface.h"

// The singular heap instance.
GPTR_DECL(IGCHeap, g_pGCHeap);

// GCHeapUtilities provides a number of static methods
// that operate on the global heap instance. It can't be
// instantiated.
class GCHeapUtilities {
public:
    // Retrieves the GC heap.
    inline static IGCHeap* GetGCHeap() 
    {
        LIMITED_METHOD_CONTRACT;

        assert(g_pGCHeap != nullptr);
        return g_pGCHeap;
    }

    // Returns true if the heap has been initialized, false otherwise.
    inline static bool IsGCHeapInitialized()
    {
        LIMITED_METHOD_CONTRACT;

        return g_pGCHeap != nullptr;
    }

    // Returns true if a the heap is initialized and a garbage collection
    // is in progress, false otherwise.
    inline static BOOL IsGCInProgress(BOOL bConsiderGCStart = FALSE)
    {
        WRAPPER_NO_CONTRACT;

        return (IsGCHeapInitialized() ? GetGCHeap()->IsGCInProgressHelper(bConsiderGCStart) : false);
    }

    // Returns true if we should be competing marking for statics. This
    // influences the behavior of `GCToEEInterface::GcScanRoots`.
    inline static BOOL MarkShouldCompeteForStatics()
    {
        WRAPPER_NO_CONTRACT;

        return IsServerHeap() && g_SystemInfo.dwNumberOfProcessors >= 2;
    }

    // Waits until a GC is complete, if the heap has been initialized.
    inline static void WaitForGCCompletion(BOOL bConsiderGCStart = FALSE)
    {
        WRAPPER_NO_CONTRACT;

        if (IsGCHeapInitialized())
            GetGCHeap()->WaitUntilGCComplete(bConsiderGCStart);
    }

    // Returns true if we should be using allocation contexts, false otherwise.
    inline static bool UseAllocationContexts()
    {
        WRAPPER_NO_CONTRACT;
#ifdef FEATURE_REDHAWK
        // SIMPLIFY:  only use allocation contexts
        return true;
#else
#if defined(_TARGET_ARM_) || defined(FEATURE_PAL)
        return true;
#else
        return ((IsServerHeap() ? true : (g_SystemInfo.dwNumberOfProcessors >= 2)));
#endif 
#endif 
    }

    // Returns true if the held GC heap is a Server GC heap, false otherwise.
    inline static bool IsServerHeap()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_SVR_GC
        _ASSERTE(IGCHeap::gcHeapType != IGCHeap::GC_HEAP_INVALID);
        return (IGCHeap::gcHeapType == IGCHeap::GC_HEAP_SVR);
#else // FEATURE_SVR_GC
        return false;
#endif // FEATURE_SVR_GC
    }

    // Gets the maximum generation number by reading the static field
    // on IGCHeap. This should only be done by the DAC code paths - all other code
    // should go through IGCHeap::GetMaxGeneration.
    //
    // The reason for this is that, while we are in the early stages of
    // decoupling the GC, the GC and the DAC still remain tightly coupled
    // and, in particular, the DAC needs to know how many generations the GC
    // has. However, it is not permitted to invoke virtual methods on g_pGCHeap
    // while on a DAC code path. Therefore, we need to determine the max generation
    // non-virtually, while still in a manner consistent with the interface - 
    // therefore, a static field is used.
    //
    // This is not without precedent - IGCHeap::gcHeapType is a static field used
    // for a similar reason (the DAC needs to know what kind of heap it's looking at).
    inline static unsigned GetMaxGeneration()
    {
        WRAPPER_NO_CONTRACT;

        return IGCHeap::maxGeneration;
    }

private:
    // This class should never be instantiated.
    GCHeapUtilities() = delete;
};

#ifndef DACCESS_COMPILE
extern "C" {
#endif // !DACCESS_COMPILE
GPTR_DECL(uint8_t,g_lowest_address);
GPTR_DECL(uint8_t,g_highest_address);
GPTR_DECL(uint32_t,g_card_table);
#ifndef DACCESS_COMPILE
} 
#endif // !DACCESS_COMPILE

extern "C" uint8_t* g_ephemeral_low;
extern "C" uint8_t* g_ephemeral_high;

#endif // _GCHEAPUTILITIES_H_