// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCHEAPUTILITIES_H_
#define _GCHEAPUTILITIES_H_

#include "gcinterface.h"

// The singular heap instance.
GPTR_DECL(IGCHeap, g_pGCHeap);

#ifndef DACCESS_COMPILE
extern "C" {
#endif // !DACCESS_COMPILE

// This struct adds some state that is only visible to the EE onto the standard gc_alloc_context
typedef struct _ee_alloc_context
{
    // TODO: rename it to alloc_sampling for consistency sake with alloc_ptr and alloc_limit
    // This is the address where the next allocation sampling should occur:
    //  - if sampling is off, this is the same as alloc_limit
    //  - if sampling is on, this is the next sampled byte in the gc_alloc_context
    //  - if no such byte exists (i.e. the sampling threshold is outside of the allocation context),
    //     this is the same as alloc_limit
    uint8_t* fast_alloc_helper_limit_ptr;
    gc_alloc_context gc_alloc_context;

 public:
    void init()
    {
        LIMITED_METHOD_CONTRACT;
        fast_alloc_helper_limit_ptr = nullptr;
        gc_alloc_context.init();
    }

    // TODO: rename it to GenerateAllocSampling()
    inline void SetFastAllocHelperLimit()
    {
        // TODO: maybe it is easier to assume that the caller of this function will check if sampling is on/off
        // If sampling is off this is just setting fast_alloc_helper_limit_ptr = alloc_limit
        // If sampling is on then we'd do some pseudo-random number generation to decide what is
        // the next sampled byte in the gc_alloc_context, if any.

        //TODO: implement sampling limit placement strategy
        fast_alloc_helper_limit_ptr = gc_alloc_context.alloc_limit;
    }
} ee_alloc_context;

GPTR_DECL(uint8_t,g_lowest_address);
GPTR_DECL(uint8_t,g_highest_address);
GPTR_DECL(uint32_t,g_card_table);
GVAL_DECL(GCHeapType, g_heap_type);

// For single-proc machines, the EE will use a single, shared alloc context
// for all allocations. In order to avoid extra indirections in assembly
// allocation helpers, the EE owns the global allocation context and the
// GC will update it when it needs to.
extern "C" ee_alloc_context g_global_ee_alloc_context;

// This is a pointer into the g_global_ee_alloc_context for the GC visible
// subset of the data
GPTR_DECL(gc_alloc_context, g_global_alloc_context);
#ifndef DACCESS_COMPILE
}
#endif // !DACCESS_COMPILE

extern "C" uint32_t* g_card_bundle_table;
extern "C" uint8_t* g_ephemeral_low;
extern "C" uint8_t* g_ephemeral_high;
extern "C" uint8_t* g_region_to_generation_table;
extern "C" uint8_t  g_region_shr;
extern "C" bool     g_region_use_bitwise_write_barrier;


#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

// Table containing the dirty state. This table is translated to exclude the lowest address it represents, see
// TranslateTableToExcludeHeapStartAddress.
extern "C" uint8_t *g_sw_ww_table;

// Write watch may be disabled when it is not needed (between GCs for instance). This indicates whether it is enabled.
extern "C" bool g_sw_ww_enabled_for_gc_heap;

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
    inline static bool IsGCInProgress(bool bConsiderGCStart = false)
    {
        WRAPPER_NO_CONTRACT;

        return (IsGCHeapInitialized() ? GetGCHeap()->IsGCInProgressHelper(bConsiderGCStart) : false);
    }

    // Returns true if we should be competing marking for statics. This
    // influences the behavior of `GCToEEInterface::GcScanRoots`.
    inline static bool MarkShouldCompeteForStatics()
    {
        WRAPPER_NO_CONTRACT;

        return IsServerHeap() && g_SystemInfo.dwNumberOfProcessors >= 2;
    }

    // Waits until a GC is complete, if the heap has been initialized.
    inline static void WaitForGCCompletion(bool bConsiderGCStart = false)
    {
        WRAPPER_NO_CONTRACT;

        if (IsGCHeapInitialized())
            GetGCHeap()->WaitUntilGCComplete(bConsiderGCStart);
    }

    // Returns true if the held GC heap is a Server GC heap, false otherwise.
    inline static bool IsServerHeap()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_SVR_GC
        _ASSERTE(g_heap_type != GC_HEAP_INVALID);
        return g_heap_type == GC_HEAP_SVR;
#else
        return false;
#endif // FEATURE_SVR_GC
    }

    static bool UseThreadAllocationContexts()
    {
        return s_useThreadAllocationContexts;
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
#ifdef _DEBUG
        uint8_t* end_of_write_ptr = reinterpret_cast<uint8_t*>(address) + (write_size - 1);
        assert(table_byte_index == reinterpret_cast<size_t>(end_of_write_ptr) >> SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift);
#endif
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

        // We'll mark the entire region of memory as dirty by memsetting all entries in
        // the SWW table between the start and end indexes.
        memset(&g_sw_ww_table[base_index], ~0, end_index - base_index + 1);
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifndef DACCESS_COMPILE
    // Gets a pointer to the module that contains the GC.
    static PTR_VOID GetGCModuleBase();

    // Loads (if using a standalone GC) and initializes the GC.
    static HRESULT LoadAndInitialize();

    // Records a change in eventing state. This ultimately will inform the GC that it needs to be aware
    // of new events being enabled.
    static void RecordEventStateChange(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level);
#endif // DACCESS_COMPILE

private:
    // This class should never be instantiated.
    GCHeapUtilities() = delete;

    static bool s_useThreadAllocationContexts;
};

#endif // _GCHEAPUTILITIES_H_

