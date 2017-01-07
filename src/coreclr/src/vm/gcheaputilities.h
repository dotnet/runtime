// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCHEAPUTILITIES_H_
#define _GCHEAPUTILITIES_H_

#include "gcinterface.h"

// The singular heap instance.
GPTR_DECL(IGCHeap, g_pGCHeap);

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

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

// Table containing the dirty state. This table is translated to exclude the lowest address it represents, see
// TranslateTableToExcludeHeapStartAddress.
extern "C" uint8_t *g_sw_ww_table;

// Write watch may be disabled when it is not needed (between GCs for instance). This indicates whether it is enabled.
extern "C" bool g_sw_ww_enabled_for_gc_heap;

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


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

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    // Returns True if software write watch is currently enabled for the GC Heap,
    // or False if it is not.
    inline static bool SoftwareWriteWatchIsEnabled()
    {
        WRAPPER_NO_CONTRACT;

        return g_sw_ww_enabled_for_gc_heap;
    }

    // In accordance with the SoftwareWriteWatch scheme, marks a given address as
    // "dirty" (e.g. has been written to).
    inline static void SoftwareWriteWatchSetDirty(void* address, size_t write_size)
    {
        LIMITED_METHOD_CONTRACT;

        // We presumably have just written something to this address, so it can't be null.
        assert(address != nullptr);

        // The implementation is limited to writes of a pointer size or less. Writes larger
        // than pointer size may cross page boundaries and would require us to potentially
        // set more than one entry in the SWW table, which can't be done atomically under
        // the current scheme.
        assert(write_size <= sizeof(void*));

        size_t table_byte_index = reinterpret_cast<size_t>(address) >> SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift;

        // The table byte index that we calculate for the address should be the same as the one
        // calculated for a pointer to the end of the written region. If this were not the case,
        // this write crossed a boundary and would dirty two pages.
        uint8_t* end_of_write_ptr = reinterpret_cast<uint8_t*>(address) + (write_size - 1);
        assert(table_byte_index == reinterpret_cast<size_t>(end_of_write_ptr) >> SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift);
        uint8_t* table_address = &g_sw_ww_table[table_byte_index];
        if (*table_address == 0)
        {
            *table_address = 0xFF;
        }
    }

    // In accordance with the SoftwareWriteWatch scheme, marks a range of addresses
    // as dirty, starting at the given address and with the given length.
    inline static void SoftwareWriteWatchSetDirtyRegion(void* address, size_t length)
    {
        LIMITED_METHOD_CONTRACT;

        // We presumably have just memcopied something to this address, so it can't be null.
        assert(address != nullptr);

        // The "base index" is the first index in the SWW table that covers the target
        // region of memory.
        size_t base_index = reinterpret_cast<size_t>(address) >> SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift;

        // The "end_index" is the last index in the SWW table that covers the target
        // region of memory.
        uint8_t* end_pointer = reinterpret_cast<uint8_t*>(address) + length - 1;
        size_t end_index = reinterpret_cast<size_t>(end_pointer) >> SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift;

        // We'll mark the entire region of memory as dirty by memseting all entries in
        // the SWW table between the start and end indexes.
        memset(&g_sw_ww_table[base_index], ~0, end_index - base_index + 1);
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


private:
    // This class should never be instantiated.
    GCHeapUtilities() = delete;
};

#endif // _GCHEAPUTILITIES_H_