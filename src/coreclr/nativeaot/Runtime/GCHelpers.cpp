// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged helpers exposed by the System.GC managed class.
//

#include "common.h"
#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcheaputilities.h"
#include "RestrictedCallouts.h"

#include "gcrhinterface.h"

#include "PalRedhawkCommon.h"
#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"

#include "thread.h"
#include "RWLock.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

EXTERN_C NATIVEAOT_API void __cdecl RhpCollect(uint32_t uGeneration, uint32_t uMode)
{
    // This must be called via p/invoke rather than RuntimeImport to make the stack crawlable.

    Thread * pCurThread = ThreadStore::GetCurrentThread();

    pCurThread->DeferTransitionFrame();
    pCurThread->DisablePreemptiveMode();

    ASSERT(!pCurThread->IsDoNotTriggerGcSet());
    GCHeapUtilities::GetGCHeap()->GarbageCollect(uGeneration, FALSE, uMode);

    pCurThread->EnablePreemptiveMode();
}

EXTERN_C NATIVEAOT_API int64_t __cdecl RhpGetGcTotalMemory()
{
    // This must be called via p/invoke rather than RuntimeImport to make the stack crawlable.

    Thread * pCurThread = ThreadStore::GetCurrentThread();

    pCurThread->DeferTransitionFrame();
    pCurThread->DisablePreemptiveMode();

    int64_t ret = GCHeapUtilities::GetGCHeap()->GetTotalBytesInUse();

    pCurThread->EnablePreemptiveMode();

    return ret;
}

EXTERN_C NATIVEAOT_API int32_t __cdecl RhpStartNoGCRegion(int64_t totalSize, UInt32_BOOL hasLohSize, int64_t lohSize, UInt32_BOOL disallowFullBlockingGC)
{
    Thread *pCurThread = ThreadStore::GetCurrentThread();
    ASSERT(!pCurThread->IsCurrentThreadInCooperativeMode());

    pCurThread->DeferTransitionFrame();
    pCurThread->DisablePreemptiveMode();

    int result = GCHeapUtilities::GetGCHeap()->StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC);

    pCurThread->EnablePreemptiveMode();

    return result;
}

EXTERN_C NATIVEAOT_API int32_t __cdecl RhpEndNoGCRegion()
{
    ASSERT(!ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    return GCHeapUtilities::GetGCHeap()->EndNoGCRegion();
}

COOP_PINVOKE_HELPER(void, RhSuppressFinalize, (OBJECTREF refObj))
{
    if (!refObj->get_EEType()->HasFinalizer())
        return;
    GCHeapUtilities::GetGCHeap()->SetFinalizationRun(refObj);
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhReRegisterForFinalize, (OBJECTREF refObj))
{
    if (!refObj->get_EEType()->HasFinalizer())
        FC_RETURN_BOOL(true);
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->RegisterForFinalization(-1, refObj));
}

COOP_PINVOKE_HELPER(int32_t, RhGetMaxGcGeneration, ())
{
    return GCHeapUtilities::GetGCHeap()->GetMaxGeneration();
}

COOP_PINVOKE_HELPER(int32_t, RhGetGcCollectionCount, (int32_t generation, CLR_BOOL getSpecialGCCount))
{
    return GCHeapUtilities::GetGCHeap()->CollectionCount(generation, getSpecialGCCount);
}

COOP_PINVOKE_HELPER(int32_t, RhGetGeneration, (OBJECTREF obj))
{
    return GCHeapUtilities::GetGCHeap()->WhichGeneration(obj);
}

COOP_PINVOKE_HELPER(int32_t, RhGetGcLatencyMode, ())
{
    return GCHeapUtilities::GetGCHeap()->GetGcLatencyMode();
}

COOP_PINVOKE_HELPER(int32_t, RhSetGcLatencyMode, (int32_t newLatencyMode))
{
    return GCHeapUtilities::GetGCHeap()->SetGcLatencyMode(newLatencyMode);
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhIsServerGc, ())
{
    FC_RETURN_BOOL(GCHeapUtilities::IsServerHeap());
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhRegisterGcCallout, (GcRestrictedCalloutKind eKind, void * pCallout))
{
    FC_RETURN_BOOL(RestrictedCallouts::RegisterGcCallout(eKind, pCallout));
}

COOP_PINVOKE_HELPER(void, RhUnregisterGcCallout, (GcRestrictedCalloutKind eKind, void * pCallout))
{
    RestrictedCallouts::UnregisterGcCallout(eKind, pCallout);
}

COOP_PINVOKE_HELPER(int32_t, RhGetLohCompactionMode, ())
{
    return GCHeapUtilities::GetGCHeap()->GetLOHCompactionMode();
}

COOP_PINVOKE_HELPER(void, RhSetLohCompactionMode, (int32_t newLohCompactionMode))
{
    GCHeapUtilities::GetGCHeap()->SetLOHCompactionMode(newLohCompactionMode);
}

COOP_PINVOKE_HELPER(int64_t, RhGetCurrentObjSize, ())
{
    return GCHeapUtilities::GetGCHeap()->GetCurrentObjSize();
}

COOP_PINVOKE_HELPER(int64_t, RhGetGCNow, ())
{
    return GCHeapUtilities::GetGCHeap()->GetNow();
}

COOP_PINVOKE_HELPER(int64_t, RhGetLastGCStartTime, (int32_t generation))
{
    return GCHeapUtilities::GetGCHeap()->GetLastGCStartTime(generation);
}

COOP_PINVOKE_HELPER(int64_t, RhGetLastGCDuration, (int32_t generation))
{
    return GCHeapUtilities::GetGCHeap()->GetLastGCDuration(generation);
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhRegisterForFullGCNotification, (int32_t maxGenerationThreshold, int32_t largeObjectHeapThreshold))
{
    ASSERT(maxGenerationThreshold >= 1 && maxGenerationThreshold <= 99);
    ASSERT(largeObjectHeapThreshold >= 1 && largeObjectHeapThreshold <= 99);
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold));
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhCancelFullGCNotification, ())
{
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->CancelFullGCNotification());
}

COOP_PINVOKE_HELPER(int32_t, RhWaitForFullGCApproach, (int32_t millisecondsTimeout))
{
    ASSERT(millisecondsTimeout >= -1);
    ASSERT(ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    int timeout = millisecondsTimeout == -1 ? INFINITE : millisecondsTimeout;
    return GCHeapUtilities::GetGCHeap()->WaitForFullGCApproach(millisecondsTimeout);
}

COOP_PINVOKE_HELPER(int32_t, RhWaitForFullGCComplete, (int32_t millisecondsTimeout))
{
    ASSERT(millisecondsTimeout >= -1);
    ASSERT(ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    int timeout = millisecondsTimeout == -1 ? INFINITE : millisecondsTimeout;
    return GCHeapUtilities::GetGCHeap()->WaitForFullGCComplete(millisecondsTimeout);
}

COOP_PINVOKE_HELPER(int64_t, RhGetGCSegmentSize, ())
{
    size_t first = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(true);
    size_t second = GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(false);

    return (first > second) ? first : second;
}

COOP_PINVOKE_HELPER(int64_t, RhGetAllocatedBytesForCurrentThread, ())
{
    Thread *pThread = ThreadStore::GetCurrentThread();
    gc_alloc_context *ac = pThread->GetAllocContext();
    int64_t currentAllocated = ac->alloc_bytes + ac->alloc_bytes_uoh - (ac->alloc_limit - ac->alloc_ptr);
    return currentAllocated;
}

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

COOP_PINVOKE_HELPER(void, RhGetMemoryInfo, (RH_GH_MEMORY_INFO* pData, int kind))
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

COOP_PINVOKE_HELPER(int64_t, RhGetTotalAllocatedBytes, ())
{
    uint64_t allocated_bytes = GCHeapUtilities::GetGCHeap()->GetTotalAllocatedBytes() - RedhawkGCInterface::GetDeadThreadsNonAllocBytes();

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

using EnumerateConfigurationValuesCallback = void (*)(void* context, void* name, void* publicKey, GCConfigurationType type, int64_t data);

EXTERN_C NATIVEAOT_API void __cdecl RhEnumerateConfigurationValues(void* configurationContext, EnumerateConfigurationValuesCallback callback)
{
    IGCHeap* pHeap = GCHeapUtilities::GetGCHeap();
    pHeap->EnumerateConfigurationValues(configurationContext, callback);
}

EXTERN_C NATIVEAOT_API int64_t __cdecl RhGetTotalAllocatedBytesPrecise()
{
    int64_t allocated;

    // We need to suspend/restart the EE to get each thread's
    // non-allocated memory from their allocation contexts

    GCToEEInterface::SuspendEE(SUSPEND_REASON::SUSPEND_FOR_GC);

    allocated = GCHeapUtilities::GetGCHeap()->GetTotalAllocatedBytes() - RedhawkGCInterface::GetDeadThreadsNonAllocBytes();

    FOREACH_THREAD(pThread)
    {
        gc_alloc_context* ac = pThread->GetAllocContext();
        allocated -= ac->alloc_limit - ac->alloc_ptr;
    }
    END_FOREACH_THREAD

    GCToEEInterface::RestartEE(true);

    return allocated;
}

extern Object* GcAllocInternal(MethodTable* pEEType, uint32_t uFlags, uintptr_t cbSize, Thread* pThread);

EXTERN_C NATIVEAOT_API void RhAllocateNewArray(MethodTable* pArrayEEType, uint32_t numElements, uint32_t flags, Array** pResult)
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    pThread->DeferTransitionFrame();
    pThread->DisablePreemptiveMode();

    ASSERT(!pThread->IsDoNotTriggerGcSet());

    *pResult = (Array*)GcAllocInternal(pArrayEEType, flags, numElements, pThread);

    pThread->EnablePreemptiveMode();
}

EXTERN_C NATIVEAOT_API void RhAllocateNewObject(MethodTable* pEEType, uint32_t flags, Object** pResult)
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    pThread->DeferTransitionFrame();
    pThread->DisablePreemptiveMode();

    ASSERT(!pThread->IsDoNotTriggerGcSet());

    *pResult = GcAllocInternal(pEEType, flags, 0, pThread);

    pThread->EnablePreemptiveMode();
}

COOP_PINVOKE_HELPER(int64_t, RhGetTotalPauseDuration, ())
{
    return GCHeapUtilities::GetGCHeap()->GetTotalPauseDuration();
}

COOP_PINVOKE_HELPER(void, RhRegisterForGCReporting, (GCFrameRegistration* pRegistration))
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    ASSERT(pRegistration->m_pThread == NULL);
    pRegistration->m_pThread = pThread;

    pThread->PushGCFrameRegistration(pRegistration);
}

COOP_PINVOKE_HELPER(void, RhUnregisterForGCReporting, (GCFrameRegistration* pRegistration))
{
    Thread* pThread = pRegistration->m_pThread;
    if (pThread == NULL)
        return;

    ASSERT(pThread == ThreadStore::GetCurrentThread());
    pThread->PopGCFrameRegistration(pRegistration);
}
