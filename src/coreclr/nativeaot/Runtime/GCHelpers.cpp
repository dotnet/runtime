// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged helpers exposed by the System.GC managed class.
//

#include "common.h"
#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"
#include "RestrictedCallouts.h"

#include "forward_declarations.h"
#include "RhConfig.h"

#include "PalRedhawkCommon.h"
#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "interoplibinterface.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "gcdesc.h"

#ifdef FEATURE_EVENT_TRACE
   #include "clretwallmain.h"
#else // FEATURE_EVENT_TRACE
   #include "etmdummy.h"
#endif // FEATURE_EVENT_TRACE

#define RH_LARGE_OBJECT_SIZE 85000

MethodTable g_FreeObjectEEType;
GPTR_DECL(MethodTable, g_pFreeObjectEEType);

GPTR_IMPL(Thread, g_pFinalizerThread);

bool RhInitializeFinalization();

// Perform any runtime-startup initialization needed by the GC, HandleTable or environmental code in gcenv.ee.
// Returns true on success or false if a subsystem failed to initialize.
bool InitializeGC()
{
    // Initialize the special MethodTable used to mark free list entries in the GC heap.
    g_FreeObjectEEType.InitializeAsGcFreeType();
    g_pFreeObjectEEType = &g_FreeObjectEEType;

#ifdef FEATURE_SVR_GC
    g_heap_type = (g_pRhConfig->GetgcServer() && PalGetProcessCpuCount() > 1) ? GC_HEAP_SVR : GC_HEAP_WKS;
#else
    g_heap_type = GC_HEAP_WKS;
#endif

    if (g_pRhConfig->GetgcConservative())
    {
        GetRuntimeInstance()->EnableConservativeStackReporting();
    }

    HRESULT hr = GCHeapUtilities::InitializeGC();
    if (FAILED(hr))
        return false;

    // Apparently the Windows linker removes global variables if they are never
    // read from, which is a problem for g_gcDacGlobals since it's expected that
    // only the DAC will read from it. This forces the linker to include
    // g_gcDacGlobals.
    volatile void* _dummy = g_gcDacGlobals;

    // Initialize the GC subsystem.
    hr = g_pGCHeap->Initialize();
    if (FAILED(hr))
        return false;

    if (!RhInitializeFinalization())
        return false;

    // Initialize HandleTable.
    if (!GCHandleUtilities::GetGCHandleManager()->Initialize())
        return false;

    return true;
}

//-------------------------------------------------------------------------------------------------
// Used only by GC initialization, this initializes the MethodTable used to mark free entries in the GC heap. It
// should be an array type with a component size of one (so the GC can easily size it as appropriate) and
// should be marked as not containing any references. The rest of the fields don't matter: the GC does not
// query them and the rest of the runtime will never hold a reference to free object.

void MethodTable::InitializeAsGcFreeType()
{
    m_uFlags = ParameterizedEEType | HasComponentSizeFlag;
    m_usComponentSize = 1;
    m_uBaseSize = sizeof(Array) + SYNC_BLOCK_SKEW;
}

EXTERN_C void QCALLTYPE RhpCollect(uint32_t uGeneration, uint32_t uMode, UInt32_BOOL lowMemoryP)
{
    // This must be called via p/invoke rather than RuntimeImport to make the stack crawlable.

    Thread * pCurThread = ThreadStore::GetCurrentThread();

    pCurThread->DeferTransitionFrame();
    pCurThread->DisablePreemptiveMode();

    ASSERT(!pCurThread->IsDoNotTriggerGcSet());
    GCHeapUtilities::GetGCHeap()->GarbageCollect(uGeneration, lowMemoryP, uMode);

    pCurThread->EnablePreemptiveMode();
}

EXTERN_C int64_t QCALLTYPE RhpGetGcTotalMemory()
{
    // This must be called via p/invoke rather than RuntimeImport to make the stack crawlable.

    Thread * pCurThread = ThreadStore::GetCurrentThread();

    pCurThread->DeferTransitionFrame();
    pCurThread->DisablePreemptiveMode();

    int64_t ret = GCHeapUtilities::GetGCHeap()->GetTotalBytesInUse();

    pCurThread->EnablePreemptiveMode();

    return ret;
}

EXTERN_C int32_t QCALLTYPE RhpStartNoGCRegion(int64_t totalSize, UInt32_BOOL hasLohSize, int64_t lohSize, UInt32_BOOL disallowFullBlockingGC)
{
    Thread *pCurThread = ThreadStore::GetCurrentThread();
    ASSERT(!pCurThread->IsCurrentThreadInCooperativeMode());

    pCurThread->DeferTransitionFrame();
    pCurThread->DisablePreemptiveMode();

    int result = GCHeapUtilities::GetGCHeap()->StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC);

    pCurThread->EnablePreemptiveMode();

    return result;
}

EXTERN_C int32_t QCALLTYPE RhpEndNoGCRegion()
{
    ASSERT(!ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    return GCHeapUtilities::GetGCHeap()->EndNoGCRegion();
}

FCIMPL1(void, RhSuppressFinalize, OBJECTREF refObj)
{
    if (!refObj->GetMethodTable()->HasFinalizer())
        return;
    GCHeapUtilities::GetGCHeap()->SetFinalizationRun(refObj);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RhReRegisterForFinalize, OBJECTREF refObj)
{
    if (!refObj->GetMethodTable()->HasFinalizer())
        FC_RETURN_BOOL(true);
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->RegisterForFinalization(-1, refObj));
}
FCIMPLEND

FCIMPL0(int32_t, RhGetMaxGcGeneration)
{
    return GCHeapUtilities::GetGCHeap()->GetMaxGeneration();
}
FCIMPLEND

FCIMPL2(int32_t, RhGetGcCollectionCount, int32_t generation, CLR_BOOL getSpecialGCCount)
{
    return GCHeapUtilities::GetGCHeap()->CollectionCount(generation, getSpecialGCCount);
}
FCIMPLEND

FCIMPL1(int32_t, RhGetGeneration, OBJECTREF obj)
{
    return GCHeapUtilities::GetGCHeap()->WhichGeneration(obj);
}
FCIMPLEND

FCIMPL1(int64_t, RhGetGenerationSize, int32_t gen)
{
    return (int64_t)(GCHeapUtilities::GetGCHeap()->GetLastGCGenerationSize(gen));
}
FCIMPLEND

FCIMPL0(int64_t, RhGetLastGCPercentTimeInGC)
{
    return GCHeapUtilities::GetGCHeap()->GetLastGCPercentTimeInGC();
}
FCIMPLEND

FCIMPL0(int32_t, RhGetGcLatencyMode)
{
    return GCHeapUtilities::GetGCHeap()->GetGcLatencyMode();
}
FCIMPLEND

FCIMPL1(int32_t, RhSetGcLatencyMode, int32_t newLatencyMode)
{
    return GCHeapUtilities::GetGCHeap()->SetGcLatencyMode(newLatencyMode);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RhIsPromoted, OBJECTREF obj)
{
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->IsPromoted(obj));
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, RhIsServerGc)
{
    FC_RETURN_BOOL(GCHeapUtilities::IsServerHeap());
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RhRegisterGcCallout, GcRestrictedCalloutKind eKind, void * pCallout)
{
    FC_RETURN_BOOL(RestrictedCallouts::RegisterGcCallout(eKind, pCallout));
}
FCIMPLEND

FCIMPL2(void, RhUnregisterGcCallout, GcRestrictedCalloutKind eKind, void * pCallout)
{
    RestrictedCallouts::UnregisterGcCallout(eKind, pCallout);
}
FCIMPLEND

#ifdef FEATURE_OBJCMARSHAL
FCIMPL1(FC_BOOL_RET, RhRegisterObjectiveCMarshalBeginEndCallback, void * pCallback)
{
    FC_RETURN_BOOL(ObjCMarshalNative::RegisterBeginEndCallback(pCallback));
}
FCIMPLEND
#endif

FCIMPL0(int32_t, RhGetLohCompactionMode)
{
    return GCHeapUtilities::GetGCHeap()->GetLOHCompactionMode();
}
FCIMPLEND

FCIMPL1(void, RhSetLohCompactionMode, int32_t newLohCompactionMode)
{
    GCHeapUtilities::GetGCHeap()->SetLOHCompactionMode(newLohCompactionMode);
}
FCIMPLEND

FCIMPL0(int64_t, RhGetCurrentObjSize)
{
    return GCHeapUtilities::GetGCHeap()->GetCurrentObjSize();
}
FCIMPLEND

FCIMPL0(int64_t, RhGetGCNow)
{
    return GCHeapUtilities::GetGCHeap()->GetNow();
}
FCIMPLEND

FCIMPL1(int64_t, RhGetLastGCStartTime, int32_t generation)
{
    return GCHeapUtilities::GetGCHeap()->GetLastGCStartTime(generation);
}
FCIMPLEND

FCIMPL1(int64_t, RhGetLastGCDuration, int32_t generation)
{
    return GCHeapUtilities::GetGCHeap()->GetLastGCDuration(generation);
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RhRegisterForFullGCNotification, int32_t maxGenerationThreshold, int32_t largeObjectHeapThreshold)
{
    ASSERT(maxGenerationThreshold >= 1 && maxGenerationThreshold <= 99);
    ASSERT(largeObjectHeapThreshold >= 1 && largeObjectHeapThreshold <= 99);
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold));
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, RhCancelFullGCNotification)
{
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->CancelFullGCNotification());
}
FCIMPLEND

FCIMPL1(int32_t, RhWaitForFullGCApproach, int32_t millisecondsTimeout)
{
    ASSERT(millisecondsTimeout >= -1);
    ASSERT(ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    int timeout = millisecondsTimeout == -1 ? INFINITE : millisecondsTimeout;
    return GCHeapUtilities::GetGCHeap()->WaitForFullGCApproach(millisecondsTimeout);
}
FCIMPLEND

FCIMPL1(int32_t, RhWaitForFullGCComplete, int32_t millisecondsTimeout)
{
    ASSERT(millisecondsTimeout >= -1);
    ASSERT(ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    int timeout = millisecondsTimeout == -1 ? INFINITE : millisecondsTimeout;
    return GCHeapUtilities::GetGCHeap()->WaitForFullGCComplete(millisecondsTimeout);
}
FCIMPLEND

FCIMPL0(int64_t, RhGetGCSegmentSize)
{
    size_t first = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(true);
    size_t second = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(false);

    return (first > second) ? first : second;
}
FCIMPLEND

FCIMPL0(int64_t, RhGetAllocatedBytesForCurrentThread)
{
    Thread *pThread = ThreadStore::GetCurrentThread();
    gc_alloc_context *ac = pThread->GetAllocContext();
    int64_t currentAllocated = ac->alloc_bytes + ac->alloc_bytes_uoh - (ac->alloc_limit - ac->alloc_ptr);
    return currentAllocated;
}
FCIMPLEND

struct RH_GC_GENERATION_INFO
{
    uint64_t sizeBefore;
    uint64_t fragmentationBefore;
    uint64_t sizeAfter;
    uint64_t fragmentationAfter;
};

struct RH_GH_MEMORY_INFO
{
public:
    uint64_t highMemLoadThresholdBytes;
    uint64_t totalAvailableMemoryBytes;
    uint64_t lastRecordedMemLoadBytes;
    uint64_t lastRecordedHeapSizeBytes;
    uint64_t lastRecordedFragmentationBytes;
    uint64_t totalCommittedBytes;
    uint64_t promotedBytes;
    uint64_t pinnedObjectCount;
    uint64_t finalizationPendingCount;
    uint64_t index;
    uint32_t generation;
    uint32_t pauseTimePercent;
    uint8_t isCompaction;
    uint8_t isConcurrent;
    RH_GC_GENERATION_INFO generationInfo0;
    RH_GC_GENERATION_INFO generationInfo1;
    RH_GC_GENERATION_INFO generationInfo2;
    RH_GC_GENERATION_INFO generationInfo3;
    RH_GC_GENERATION_INFO generationInfo4;
    uint64_t pauseDuration0;
    uint64_t pauseDuration1;
};

FCIMPL2(void, RhGetMemoryInfo, RH_GH_MEMORY_INFO* pData, int kind)
{
    uint64_t* genInfoRaw = (uint64_t*)&(pData->generationInfo0);
    uint64_t* pauseInfoRaw = (uint64_t*)&(pData->pauseDuration0);

    return GCHeapUtilities::GetGCHeap()->GetMemoryInfo(
        &(pData->highMemLoadThresholdBytes),
        &(pData->totalAvailableMemoryBytes),
        &(pData->lastRecordedMemLoadBytes),
        &(pData->lastRecordedHeapSizeBytes),
        &(pData->lastRecordedFragmentationBytes),
        &(pData->totalCommittedBytes),
        &(pData->promotedBytes),
        &(pData->pinnedObjectCount),
        &(pData->finalizationPendingCount),
        &(pData->index),
        &(pData->generation),
        &(pData->pauseTimePercent),
        (bool*)&(pData->isCompaction),
        (bool*)&(pData->isConcurrent),
        genInfoRaw,
        pauseInfoRaw,
        kind);
}
FCIMPLEND


// The MethodTable is remembered in some slow-path allocation paths. This value is used in event tracing.
// It may statistically correlate with the most allocated type on the given stack/thread.
DECLSPEC_THREAD
MethodTable* tls_pLastAllocationEEType = NULL;

MethodTable* GetLastAllocEEType()
{
    return tls_pLastAllocationEEType;
}

FCIMPL0(int64_t, RhGetTotalAllocatedBytes)
{
    uint64_t allocated_bytes = GCHeapUtilities::GetGCHeap()->GetTotalAllocatedBytes() - Thread::GetDeadThreadsNonAllocBytes();

    // highest reported allocated_bytes. We do not want to report a value less than that even if unused_bytes has increased.
    static uint64_t high_watermark;

    uint64_t current_high = high_watermark;
    while (allocated_bytes > current_high)
    {
        uint64_t orig = PalInterlockedCompareExchange64((int64_t*)&high_watermark, allocated_bytes, current_high);
        if (orig == current_high)
            return allocated_bytes;

        current_high = orig;
    }

    return current_high;
}
FCIMPLEND

EXTERN_C void QCALLTYPE RhEnumerateConfigurationValues(void* configurationContext, ConfigurationValueFunc callback)
{
    IGCHeap* pHeap = GCHeapUtilities::GetGCHeap();
    pHeap->EnumerateConfigurationValues(configurationContext, callback);
}

GCHeapHardLimitInfo g_gcHeapHardLimitInfo;
bool g_gcHeapHardLimitInfoSpecified = false;

FCIMPL1(void, RhRefreshMemoryLimit, GCHeapHardLimitInfo heapHardLimitInfo)
{
    IGCHeap* pHeap = GCHeapUtilities::GetGCHeap();
    g_gcHeapHardLimitInfo = heapHardLimitInfo;
    g_gcHeapHardLimitInfoSpecified = true;
    pHeap->RefreshMemoryLimit();
}
FCIMPLEND

EXTERN_C uint64_t QCALLTYPE RhGetGenerationBudget(int generation)
{
    IGCHeap* pHeap = GCHeapUtilities::GetGCHeap();
    return pHeap->GetGenerationBudget(generation);
}

EXTERN_C void QCALLTYPE RhEnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* pCallback, int64_t totalSize)
{
    IGCHeap* pHeap = GCHeapUtilities::GetGCHeap();
    pHeap->EnableNoGCRegionCallback(pCallback, totalSize);
}

EXTERN_C int64_t QCALLTYPE RhGetTotalAllocatedBytesPrecise()
{
    int64_t allocated;

    // We need to suspend/restart the EE to get each thread's
    // non-allocated memory from their allocation contexts

    GCToEEInterface::SuspendEE(SUSPEND_REASON::SUSPEND_FOR_GC);

    allocated = GCHeapUtilities::GetGCHeap()->GetTotalAllocatedBytes() - Thread::GetDeadThreadsNonAllocBytes();

    FOREACH_THREAD(pThread)
    {
        gc_alloc_context* ac = pThread->GetAllocContext();
        allocated -= ac->alloc_limit - ac->alloc_ptr;
    }
    END_FOREACH_THREAD

    GCToEEInterface::RestartEE(true);

    return allocated;
}

inline void FireAllocationSampled(GC_ALLOC_FLAGS flags, size_t size, size_t samplingBudgetOffset, Object* orObject)
{
    void* typeId = GetLastAllocEEType();
    // Note: like for AllocationTick, the type name cannot be retrieved
    WCHAR* name = nullptr;

    if (typeId != nullptr)
    {
        unsigned int allocKind =
            (flags & GC_ALLOC_PINNED_OBJECT_HEAP) ? 2 :
            (flags & GC_ALLOC_LARGE_OBJECT_HEAP) ? 1 :
            0;  // SOH
        unsigned int heapIndex = 0;
#ifdef BACKGROUND_GC
        gc_heap* hp = gc_heap::heap_of((BYTE*)orObject);
        heapIndex = hp->heap_number;
#endif
        FireEtwAllocationSampled(allocKind, GetClrInstanceId(), typeId, name, heapIndex, (BYTE*)orObject, size, samplingBudgetOffset);
    }
}

inline size_t AlignUp(size_t value, uint32_t alignment)
{
    return (value + alignment - 1) & ~(size_t)(alignment - 1);
}

static Object* GcAllocInternal(MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements, Thread* pThread)
{
    ASSERT(!pThread->IsDoNotTriggerGcSet());
    ASSERT(pThread->IsCurrentThreadInCooperativeMode());

    if (pEEType->ContainsPointers())
    {
        uFlags |= GC_ALLOC_CONTAINS_REF;
        uFlags &= ~GC_ALLOC_ZEROING_OPTIONAL;
    }

    size_t cbSize = pEEType->GetBaseSize();

    if (pEEType->HasComponentSize())
    {
        // Impose limits on maximum array length to prevent corner case integer overflow bugs
        // Keep in sync with Array.MaxLength in BCL.
        if (pEEType->IsSzArray()) // multi-dimensional arrays are checked up-front
        {
            const int MaxArrayLength = 0x7FFFFFC7;
            if (numElements > MaxArrayLength)
                return NULL;
        }

#ifndef HOST_64BIT
        // if the element count is <= 0x10000, no overflow is possible because the component size is
        // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
        if (numElements > 0x10000)
        {
            // Perform the size computation using 64-bit integeres to detect overflow
            uint64_t size64 = (uint64_t)cbSize + ((uint64_t)numElements * (uint64_t)pEEType->RawGetComponentSize());
            size64 = (size64 + (sizeof(uintptr_t) - 1)) & ~(sizeof(uintptr_t) - 1);

            cbSize = (size_t)size64;
            if (cbSize != size64)
            {
                return NULL;
            }
        }
        else
#endif // !HOST_64BIT
        {
            cbSize = cbSize + ((size_t)numElements * (size_t)pEEType->RawGetComponentSize());
            cbSize = ALIGN_UP(cbSize, sizeof(uintptr_t));
        }
    }
    else
    {
        ASSERT(numElements == 0);
    }

    if ((cbSize >= RH_LARGE_OBJECT_SIZE) && (cbSize >= GCHeapUtilities::GetGCHeap()->GetLOHThreshold()))
    {
        uFlags |= GC_ALLOC_LARGE_OBJECT_HEAP;

#ifdef HOST_64BIT
        const size_t max_object_size = (INT64_MAX - 7 - min_obj_size);
#else
        const size_t max_object_size = (INT32_MAX - 7 - min_obj_size);
#endif

        if (cbSize >= max_object_size)
            return NULL;
    }

    // Save the MethodTable for instrumentation purposes.
    tls_pLastAllocationEEType = pEEType;

    // check for dynamic allocation sampling
    gc_alloc_context* acontext = pThread->GetAllocContext();
    bool isSampled = false;
    size_t availableSpace = 0;
    size_t aligned_size = 0;
    size_t samplingBudget = 0;

    bool isRandomizedSamplingEnabled = Thread::IsRandomizedSamplingEnabled();
    if (isRandomizedSamplingEnabled)
    {
        // object allocations are always padded up to pointer size
        aligned_size = AlignUp(cbSize, sizeof(uintptr_t));

        // The number bytes we can allocate before we need to emit a sampling event.
        // This calculation is only valid if combined_limit < alloc_limit.
        samplingBudget = (size_t)(*pThread->GetCombinedLimit() - acontext->alloc_ptr);

        // The number of bytes available in the current allocation context
        availableSpace = (size_t)(acontext->alloc_limit - acontext->alloc_ptr);

        // Check to see if the allocated object overlaps a sampled byte
        // in this AC. This happens when both:
        // 1) The AC contains a sampled byte (combined_limit < alloc_limit)
        // 2) The object is large enough to overlap it (samplingBudget < aligned_size)
        //
        // Note that the AC could have no remaining space for allocations (alloc_ptr =
        // alloc_limit = combined_limit). When a thread hasn't done any SOH allocations
        // yet it also starts in an empty state where alloc_ptr = alloc_limit =
        // combined_limit = nullptr. The (1) check handles both of these situations
        // properly as an empty AC can not have a sampled byte inside of it.
        isSampled =
            (*pThread->GetCombinedLimit() < acontext->alloc_limit) &&
            (samplingBudget < aligned_size);

        // if the object overflows the AC, we need to sample the remaining bytes
        // the sampling budget only included at most the bytes inside the AC
        if (aligned_size > availableSpace && !isSampled)
        {
            samplingBudget = pThread->ComputeGeometricRandom() + availableSpace;
            isSampled = (samplingBudget < aligned_size);
        }
    }

    Object* pObject = GCHeapUtilities::GetGCHeap()->Alloc(pThread->GetAllocContext(), cbSize, uFlags);
    if (pObject == NULL)
        return NULL;

    if (isSampled)
    {
        FireAllocationSampled((GC_ALLOC_FLAGS)uFlags, aligned_size, samplingBudget, pObject);
    }

    // There are a variety of conditions that may have invalidated the previous combined_limit value
    // such as not allocating the object in the AC memory region (UOH allocations), moving the AC, adding
    // extra alignment padding, allocating a new AC, or allocating an object that consumed the sampling budget.
    // Rather than test for all the different invalidation conditions individually we conservatively always
    // recompute it. If sampling isn't enabled this inlined function is just trivially setting
    // combined_limit=alloc_limit.
    pThread->UpdateCombinedLimit(isRandomizedSamplingEnabled);

    pObject->set_EEType(pEEType);
    if (pEEType->HasComponentSize())
    {
        ASSERT(numElements == (uint32_t)numElements);
        ((Array*)pObject)->InitArrayLength((uint32_t)numElements);
    }

    if (uFlags & GC_ALLOC_USER_OLD_HEAP)
        GCHeapUtilities::GetGCHeap()->PublishObject((uint8_t*)pObject);

#ifdef _DEBUG
    // We assume that the allocation quantum is never big enough for LARGE_OBJECT_SIZE.
    ASSERT(acontext->alloc_limit - acontext->alloc_ptr <= RH_LARGE_OBJECT_SIZE);
#endif

    return pObject;
}

// Allocate an object on the GC heap.
//  pEEType         -  type of the object
//  uFlags          -  GC type flags (see gc.h GC_ALLOC_*)
//  numElements     -  number of array elements
//  pTransitionFrame-  transition frame to make stack crawlable
// Returns a pointer to the object allocated or NULL on failure.
EXTERN_C void* F_CALL_CONV RhpGcAlloc(MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements, PInvokeTransitionFrame* pTransitionFrame)
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    // The allocation fast path is an asm helper that runs in coop mode and handles most allocation cases.
    // The helper can also be tail-called. That is desirable for the fast path.
    //
    // Here we are on the slow(er) path when we need to call into GC. The fast path pushes a frame and calls here.
    // In extremely rare cases the caller of the asm helper is hijacked and the helper is tail-called.
    // As a result the asm helper may capture a hijacked return address into the transition frame.
    // We do not want to put the burden of preventing such scenario on the fast path. Instead we will
    // check for "hijacked frame" here and un-hijack m_RIP.
    // We do not need to re-hijack when we are done, since m_RIP is discarded in POP_COOP_PINVOKE_FRAME
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    if (Thread::IsHijackTarget(pTransitionFrame->m_RIP))
    {
        ASSERT(pThread->IsHijacked());
        pTransitionFrame->m_RIP = pThread->GetHijackedReturnAddress();
    }
#else

    // NOTE: The x64 fixup above would not be sufficient on ARM64 and similar architectures since
    //       m_RIP is used to restore LR in POP_COOP_PINVOKE_FRAME.
    //       However, this entire scenario is not a problem on architectures where the return address is
    //       in a register as that makes tail-calling methods not hijackable.
    //       (see:GetReturnAddressHijackInfo for detailed reasons in the context of ARM64)
    ASSERT(!Thread::IsHijackTarget(pTransitionFrame->m_RIP));

#endif

    pThread->SetDeferredTransitionFrame(pTransitionFrame);

    return GcAllocInternal(pEEType, uFlags, numElements, pThread);
}

EXTERN_C void QCALLTYPE RhAllocateNewArray(MethodTable* pArrayEEType, uint32_t numElements, uint32_t flags, Array** pResult)
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    pThread->DeferTransitionFrame();
    pThread->DisablePreemptiveMode();

    ASSERT(!pThread->IsDoNotTriggerGcSet());

    *pResult = (Array*)GcAllocInternal(pArrayEEType, flags, numElements, pThread);

    pThread->EnablePreemptiveMode();
}

EXTERN_C void QCALLTYPE RhAllocateNewObject(MethodTable* pEEType, uint32_t flags, Object** pResult)
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    pThread->DeferTransitionFrame();
    pThread->DisablePreemptiveMode();

    ASSERT(!pThread->IsDoNotTriggerGcSet());

    *pResult = GcAllocInternal(pEEType, flags, 0, pThread);

    pThread->EnablePreemptiveMode();
}

FCIMPL0(int64_t, RhGetTotalPauseDuration)
{
    return GCHeapUtilities::GetGCHeap()->GetTotalPauseDuration();
}
FCIMPLEND

FCIMPL1(void, RhRegisterForGCReporting, GCFrameRegistration* pRegistration)
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    ASSERT(pRegistration->m_pThread == NULL);
    pRegistration->m_pThread = pThread;

    pThread->PushGCFrameRegistration(pRegistration);
}
FCIMPLEND

FCIMPL1(void, RhUnregisterForGCReporting, GCFrameRegistration* pRegistration)
{
    Thread* pThread = pRegistration->m_pThread;
    if (pThread == NULL)
        return;

    ASSERT(pThread == ThreadStore::GetCurrentThread());
    pThread->PopGCFrameRegistration(pRegistration);
}
FCIMPLEND

EXTERN_C void* QCALLTYPE RhRegisterFrozenSegment(void* pSection, size_t allocSize, size_t commitSize, size_t reservedSize)
{
    ASSERT(allocSize <= commitSize);
    ASSERT(commitSize <= reservedSize);

#ifdef FEATURE_BASICFREEZE
    segment_info seginfo;

    seginfo.pvMem = pSection;
    seginfo.ibFirstObject = sizeof(ObjHeader);
    seginfo.ibAllocated = allocSize;
    seginfo.ibCommit = commitSize;
    seginfo.ibReserved = reservedSize;

    return GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&seginfo);
#else // FEATURE_BASICFREEZE
    return NULL;
#endif // FEATURE_BASICFREEZE
}

EXTERN_C void QCALLTYPE RhUpdateFrozenSegment(void* pSegmentHandle, uint8_t* allocated, uint8_t* committed)
{
    ASSERT(allocated <= committed);

    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment((segment_handle)pSegmentHandle, allocated, committed);
}

EXTERN_C void QCALLTYPE RhUnregisterFrozenSegment(void* pSegmentHandle)
{
    GCHeapUtilities::GetGCHeap()->UnregisterFrozenSegment((segment_handle)pSegmentHandle);
}

FCIMPL1(uint32_t, RhGetGCDescSize, MethodTable* pMT)
{
    if (!pMT->ContainsPointersOrCollectible())
        return 0;

    return (uint32_t)CGCDesc::GetCGCDescFromMT(pMT)->GetSize();
}
FCIMPLEND

#ifdef FEATURE_GC_STRESS

// static
EXTERN_C UInt32_BOOL g_fGcStressStarted;
UInt32_BOOL g_fGcStressStarted = UInt32_FALSE; // UInt32_BOOL because asm code reads it

// static
EXTERN_C void F_CALL_CONV RhpStressGc()
{
    // The GarbageCollect operation below may trash the last win32 error. We save the error here so that it can be
    // restored after the GC operation;
    int32_t lastErrorOnEntry = PalGetLastError();

    if (g_fGcStressStarted && !ThreadStore::GetCurrentThread()->IsSuppressGcStressSet() && !ThreadStore::GetCurrentThread()->IsDoNotTriggerGcSet())
    {
        GCHeapUtilities::GetGCHeap()->GarbageCollect();
    }

    // Restore the saved error
    PalSetLastError(lastErrorOnEntry);
}

FCIMPL0(void, RhpInitializeGcStress)
{
    g_fGcStressStarted = UInt32_TRUE;
}
FCIMPLEND
#endif // FEATURE_GC_STRESS
