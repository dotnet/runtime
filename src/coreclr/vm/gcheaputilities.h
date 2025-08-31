// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCHEAPUTILITIES_H_
#define _GCHEAPUTILITIES_H_

#include "eventtracebase.h"
#include "gcinterface.h"
#include "math.h"
#include <minipal/xoshiro128pp.h>

// The singular heap instance.
GPTR_DECL(IGCHeap, g_pGCHeap);

#ifndef DACCESS_COMPILE
extern "C" {
#endif // !DACCESS_COMPILE

const DWORD SamplingDistributionMean = (100 * 1024);

// This struct allows adding some state that is only visible to the EE onto the standard gc_alloc_context
struct ee_alloc_context
{
    // Any allocation that would overlap m_CombinedLimit needs to be handled by the allocation slow path.
    // m_CombinedLimit is the minimum of:
    //  - gc_alloc_context.alloc_limit (the end of the current AC)
    //  - the sampling_limit
    //
    // In the simple case that randomized sampling is disabled, m_CombinedLimit is always equal to alloc_limit.
    //
    // There are two different useful interpretations for the sampling_limit. One is to treat the sampling_limit
    // as an address and when we allocate an object that overlaps that address we should emit a sampling event.
    // The other is that we can treat (sampling_limit - alloc_ptr) as a budget of how many bytes we can allocate
    // before emitting a sampling event. If we always allocated objects contiguously in the AC and incremented
    // alloc_ptr by the size of the object, these two interpretations would be equivalent. However, when objects
    // don't fit in the AC we allocate them in some other address range. The budget interpretation is more
    // flexible to handle those cases.
    //
    // The sampling limit isn't stored in any separate field explicitly, instead it is implied:
    // - if m_CombinedLimit == alloc_limit there is no sampled byte in the AC. In the budget interpretation
    //   we can allocate (alloc_limit - alloc_ptr) unsampled bytes. We'll need a new random number after
    //   that to determine whether future allocated bytes should be sampled.
    //   This occurs either because the sampling feature is disabled, or because the randomized selection
    //   of sampled bytes didn't select a byte in this AC.
    // - if m_CombinedLimit < alloc_limit there is a sample limit in the AC. sample_limit = m_CombinedLimit.
    uint8_t* m_CombinedLimit;
    gc_alloc_context m_GCAllocContext;

    void init()
    {
        LIMITED_METHOD_CONTRACT;
        m_CombinedLimit = 0;
        m_GCAllocContext.init();
    }

    uint8_t* getCombinedLimit()
    {
        LIMITED_METHOD_CONTRACT;
        return m_CombinedLimit;
    }

    static size_t getAllocPtrFieldOffset()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(ee_alloc_context, m_GCAllocContext) + offsetof(gc_alloc_context, alloc_ptr);
    }

    static size_t getCombinedLimitFieldOffset()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(ee_alloc_context, m_CombinedLimit);
    }

    // Regenerate the randomized sampling limit and update the m_CombinedLimit field.
    inline void UpdateCombinedLimit()
    {
        // The randomized sampling feature is being submitted in stages. At this point the sampling is never
        // activated so m_CombinedLimit is always equal to alloc_limit.
        m_CombinedLimit = m_GCAllocContext.alloc_limit;
    }

    static inline bool IsRandomizedSamplingEnabled()
    {
#ifdef FEATURE_EVENT_TRACE
        return ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                        TRACE_LEVEL_INFORMATION,
                                        CLR_ALLOCATIONSAMPLING_KEYWORD);
#else
        return false;
#endif // FEATURE_EVENT_TRACE
    }

    inline void UpdateCombinedLimit(bool samplingEnabled)
    {
        if (!samplingEnabled)
        {
            m_CombinedLimit = m_GCAllocContext.alloc_limit;
        }
        else
        {
            // compute the next sampling budget based on a geometric distribution
            size_t samplingBudget = ComputeGeometricRandom();

            // if the sampling limit is larger than the allocation context, no sampling will occur in this AC
            // We do Min() prior to adding to alloc_ptr to ensure alloc_ptr+samplingBudget doesn't cause an overflow.
            size_t size = m_GCAllocContext.alloc_limit - m_GCAllocContext.alloc_ptr;
            m_CombinedLimit = m_GCAllocContext.alloc_ptr + Min(samplingBudget, size);
        }
    }

    static inline uint32_t ComputeGeometricRandom()
    {
        // compute a random sample from the Geometric distribution.
        double probability = t_random.NextDouble();
        uint32_t threshold = (uint32_t)(-log(1 - probability) * SamplingDistributionMean);
        return threshold;
    }

    struct PerThreadRandom
    {
        minipal_xoshiro128pp random_state;

        PerThreadRandom()
        {
            minipal_xoshiro128pp_init(&random_state, GetRandomInt(INT_MAX));
        }

        // Returns a random double in the range [0, 1).
        double NextDouble()
        {
            uint32_t value = minipal_xoshiro128pp_next(&random_state);
            return value * (1.0/(UINT32_MAX+1.0));
        }
    };

    static thread_local PerThreadRandom t_random;
};

GPTR_DECL(uint8_t,g_lowest_address);
GPTR_DECL(uint8_t,g_highest_address);
GPTR_DECL(uint32_t,g_card_table);
GVAL_DECL(GCHeapType, g_heap_type);

// For single-proc machines, the EE will use a single, shared alloc context
// for all allocations. In order to avoid extra indirections in assembly
// allocation helpers, the EE owns the global allocation context and the
// GC will update it when it needs to.
GVAL_DECL(ee_alloc_context, g_global_alloc_context);

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
extern "C" uint8_t *g_write_watch_table;

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
#if (defined(TARGET_X86) || defined(TARGET_AMD64)) && !defined(TARGET_UNIX)
        return s_useThreadAllocationContexts;
#else
        return true;
#endif
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
        uint8_t* table_address = &g_write_watch_table[table_byte_index];
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
        memset(&g_write_watch_table[base_index], ~0, end_index - base_index + 1);
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

