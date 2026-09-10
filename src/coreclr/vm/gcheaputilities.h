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

    uint8_t* getAllocPtr()
    {
        LIMITED_METHOD_CONTRACT;
        return m_GCAllocContext.alloc_ptr;
    }

    void setAllocPtr(uint8_t* ptr)
    {
        LIMITED_METHOD_CONTRACT;
        m_GCAllocContext.alloc_ptr = ptr;
    }

    uint8_t* getAllocLimit()
    {
        LIMITED_METHOD_CONTRACT;
        return m_GCAllocContext.alloc_limit;
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

GVAL_DECL(GCHeapType, g_heap_type);

// Unused - kept for GC data contract c1 compatibility, see datadescriptor/datadescriptor.inc.
GVAL_DECL(ee_alloc_context, g_global_alloc_context);

#ifndef DACCESS_COMPILE
}
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

    inline static bool IsInGCHeap(void* address)
    {
#ifndef DACCESS_COMPILE
        return s_writeBarrierFunctions.is_in_gc_heap(
            s_writeBarrierFunctions.context,
            address);
#else
        return GetGCHeap()->IsInGCHeap(address);
#endif // !DACCESS_COMPILE
    }

#ifndef DACCESS_COMPILE
    static void InitializeWriteBarrierFunctions(IGCHeap* gcHeap);

    inline static void WriteBarrier(void** destination, Object* reference)
    {
        s_writeBarrierFunctions.write_barrier(
            s_writeBarrierFunctions.context,
            destination,
            reference);
    }

    inline static void CheckedWriteBarrier(void** destination, Object* reference)
    {
        s_writeBarrierFunctions.checked_write_barrier(
            s_writeBarrierFunctions.context,
            destination,
            reference);
    }

    inline static void BulkMoveWithWriteBarrier(
        void* destination,
        const void* source,
        size_t length)
    {
        s_writeBarrierFunctions.bulk_move_with_write_barrier(
            destination,
            source,
            length);
    }

    inline static const WriteBarrierFunctions& GetWriteBarrierFunctions()
    {
        return s_writeBarrierFunctions;
    }

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

#ifndef DACCESS_COMPILE
    static WriteBarrierFunctions s_writeBarrierFunctions;
#endif // !DACCESS_COMPILE
};

#endif // _GCHEAPUTILITIES_H_
