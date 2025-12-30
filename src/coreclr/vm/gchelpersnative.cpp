// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: GCHelpersNative.cpp
//

//
// Purpose: Native methods for GC
//

#include "common.h"
#include "object.h"
#include "excep.h"
#include "vars.hpp"
#include "gchelpersnative.h"

#include "utilcode.h"
#include "frames.h"
#include "field.h"
#include "winwrap.h"
#include "gcheaputilities.h"
#include "fcall.h"
#include "invokeutil.h"
#include "eeconfig.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#include <optsmallperfcritical.h>

//

// GCInterface
//
INT32    GCInterface::m_gc_counts[3] = {0,0,0};
UINT64   GCInterface::m_addPressure[MEM_PRESSURE_COUNT] = {0, 0, 0, 0};   // history of memory pressure additions
UINT64   GCInterface::m_remPressure[MEM_PRESSURE_COUNT] = {0, 0, 0, 0};   // history of memory pressure removals

// incremented after a gen2 GC has been detected,
// (m_iteration % MEM_PRESSURE_COUNT) is used as an index into m_addPressure and m_remPressure
UINT     GCInterface::m_iteration = 0;

FCIMPL0(INT64, GCInterface::GetTotalPauseDuration)
{
    FCALL_CONTRACT;

    return GCHeapUtilities::GetGCHeap()->GetTotalPauseDuration();
}
FCIMPLEND

FCIMPL2(void, GCInterface::GetMemoryInfo, Object* objUNSAFE, int kind)
{
    FCALL_CONTRACT;

    GCMEMORYINFODATAREF objGCMemoryInfo = (GCMEMORYINFODATAREF)(ObjectToOBJECTREF (objUNSAFE));

    UINT64* genInfoRaw = (UINT64*)&(objGCMemoryInfo->generationInfo0);
    UINT64* pauseInfoRaw = (UINT64*)&(objGCMemoryInfo->pauseDuration0);

    return GCHeapUtilities::GetGCHeap()->GetMemoryInfo(
        &(objGCMemoryInfo->highMemLoadThresholdBytes),
        &(objGCMemoryInfo->totalAvailableMemoryBytes),
        &(objGCMemoryInfo->lastRecordedMemLoadBytes),
        &(objGCMemoryInfo->lastRecordedHeapSizeBytes),
        &(objGCMemoryInfo->lastRecordedFragmentationBytes),
        &(objGCMemoryInfo->totalCommittedBytes),
        &(objGCMemoryInfo->promotedBytes),
        &(objGCMemoryInfo->pinnedObjectCount),
        &(objGCMemoryInfo->finalizationPendingCount),
        &(objGCMemoryInfo->index),
        &(objGCMemoryInfo->generation),
        &(objGCMemoryInfo->pauseTimePercent),
        (bool*)&(objGCMemoryInfo->isCompaction),
        (bool*)&(objGCMemoryInfo->isConcurrent),
        genInfoRaw,
        pauseInfoRaw,
        kind);
}
FCIMPLEND

FCIMPL0(UINT32, GCInterface::GetMemoryLoad)
{
    FCALL_CONTRACT;

    int result = (INT32)GCHeapUtilities::GetGCHeap()->GetMemoryLoad();
    return result;
}
FCIMPLEND

FCIMPL0(int, GCInterface::GetGcLatencyMode)
{
    FCALL_CONTRACT;

    int result = (INT32)GCHeapUtilities::GetGCHeap()->GetGcLatencyMode();
    return result;
}
FCIMPLEND

FCIMPL1(int, GCInterface::SetGcLatencyMode, int newLatencyMode)
{
    FCALL_CONTRACT;

    return GCHeapUtilities::GetGCHeap()->SetGcLatencyMode(newLatencyMode);
}
FCIMPLEND

FCIMPL0(int, GCInterface::GetLOHCompactionMode)
{
    FCALL_CONTRACT;

    int result = (INT32)GCHeapUtilities::GetGCHeap()->GetLOHCompactionMode();
    return result;
}
FCIMPLEND

FCIMPL1(void, GCInterface::SetLOHCompactionMode, int newLOHCompactionyMode)
{
    FCALL_CONTRACT;

    GCHeapUtilities::GetGCHeap()->SetLOHCompactionMode(newLOHCompactionyMode);
}
FCIMPLEND


FCIMPL2(FC_BOOL_RET, GCInterface::RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->RegisterForFullGCNotification(gen2Percentage, lohPercentage));
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, GCInterface::CancelFullGCNotification)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->CancelFullGCNotification());
}
FCIMPLEND

extern "C" int QCALLTYPE GCInterface_WaitForFullGCApproach(int millisecondsTimeout)
{
    QCALL_CONTRACT;

    int result = 0;

    BEGIN_QCALL;

    GCX_COOP();

    DWORD dwMilliseconds = ((millisecondsTimeout == -1) ? INFINITE : millisecondsTimeout);
    result = GCHeapUtilities::GetGCHeap()->WaitForFullGCApproach(dwMilliseconds);

    END_QCALL;

    return result;
}

extern "C" int QCALLTYPE GCInterface_WaitForFullGCComplete(int millisecondsTimeout)
{
    QCALL_CONTRACT;

    int result = 0;

    BEGIN_QCALL;

    GCX_COOP();

    DWORD dwMilliseconds = ((millisecondsTimeout == -1) ? INFINITE : millisecondsTimeout);
    result = GCHeapUtilities::GetGCHeap()->WaitForFullGCComplete(dwMilliseconds);

    END_QCALL;

    return result;
}

/*================================GetGenerationInternal=================================
**Action: Returns the generation in which args->obj is found.
**Returns: The generation in which args->obj is found.
**Arguments: args->obj -- The object to locate.
==============================================================================*/
FCIMPL1(int, GCInterface::GetGenerationInternal, Object* objUNSAFE)
{
    FCALL_CONTRACT;
    _ASSERTE(objUNSAFE != NULL);
    return (INT32)GCHeapUtilities::GetGCHeap()->WhichGeneration(objUNSAFE);
}
FCIMPLEND

/*================================GetSegmentSize========-=======================
**Action: Returns the maximum GC heap segment size
**Returns: The maximum segment size of either the normal heap or the large object heap, whichever is bigger
==============================================================================*/
FCIMPL0(UINT64, GCInterface::GetSegmentSize)
{
    FCALL_CONTRACT;

    IGCHeap * pGC = GCHeapUtilities::GetGCHeap();
    size_t segment_size = pGC->GetValidSegmentSize(false);
    size_t large_segment_size = pGC->GetValidSegmentSize(true);
    _ASSERTE(segment_size < SIZE_T_MAX && large_segment_size < SIZE_T_MAX);
    if (segment_size < large_segment_size)
        segment_size = large_segment_size;
    return (UINT64) segment_size;
}
FCIMPLEND

/*================================CollectionCount=================================
**Action: Returns the number of collections for this generation since the beginning of the life of the process
**Returns: The collection count.
**Arguments: args->generation -- The generation
**Exceptions: Argument exception if args->generation is < 0 or > GetMaxGeneration();
==============================================================================*/
FCIMPL2(int, GCInterface::CollectionCount, INT32 generation, INT32 getSpecialGCCount)
{
    FCALL_CONTRACT;

    //We've already checked this in GC.cs, so we'll just assert it here.
    _ASSERTE(generation >= 0);

    //We don't need to check the top end because the GC will take care of that.
    return (INT32)GCHeapUtilities::GetGCHeap()->CollectionCount(generation, getSpecialGCCount);
}
FCIMPLEND

extern "C" int QCALLTYPE GCInterface_StartNoGCRegion(INT64 totalSize, BOOL lohSizeKnown, INT64 lohSize, BOOL disallowFullBlockingGC)
{
    QCALL_CONTRACT;

    int retVal = 0;

    BEGIN_QCALL;

    GCX_COOP();

    retVal = GCHeapUtilities::GetGCHeap()->StartNoGCRegion((ULONGLONG)totalSize,
                                                  !!lohSizeKnown,
                                                  (ULONGLONG)lohSize,
                                                  !!disallowFullBlockingGC);

    END_QCALL;

    return retVal;
}

extern "C" int QCALLTYPE GCInterface_EndNoGCRegion()
{
    QCALL_CONTRACT;

    int retVal = FALSE;

    BEGIN_QCALL;

    retVal = GCHeapUtilities::GetGCHeap()->EndNoGCRegion();

    END_QCALL;

    return retVal;
}

FCIMPL0(int, GCInterface::GetLastGCPercentTimeInGC)
{
    FCALL_CONTRACT;

    return GCHeapUtilities::GetGCHeap()->GetLastGCPercentTimeInGC();
}
FCIMPLEND

FCIMPL1(UINT64, GCInterface::GetGenerationSize, int gen)
{
    FCALL_CONTRACT;

    return (UINT64)(GCHeapUtilities::GetGCHeap()->GetLastGCGenerationSize(gen));
}
FCIMPLEND

/*================================GetTotalMemory================================
**Action: Returns the total number of bytes in use
**Returns: The total number of bytes in use
**Arguments: None
**Exceptions: None
==============================================================================*/
extern "C" INT64 QCALLTYPE GCInterface_GetTotalMemory()
{
    QCALL_CONTRACT;

    INT64 iRetVal = 0;

    BEGIN_QCALL;

    GCX_COOP();
    iRetVal = (INT64) GCHeapUtilities::GetGCHeap()->GetTotalBytesInUse();

    END_QCALL;

    return iRetVal;
}

/*==============================Collect=========================================
**Action: Collects all generations <= args->generation
**Returns: void
**Arguments: args->generation:  The maximum generation to collect
**Exceptions: Argument exception if args->generation is < 0 or > GetMaxGeneration();
==============================================================================*/
extern "C" void QCALLTYPE GCInterface_Collect(INT32 generation, INT32 mode, CLR_BOOL lowMemoryPressure)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    //We've already checked this in GC.cs, so we'll just assert it here.
    _ASSERTE(generation >= -1);

    //We don't need to check the top end because the GC will take care of that.

    GCX_COOP();
    GCHeapUtilities::GetGCHeap()->GarbageCollect(generation, lowMemoryPressure, mode);

    END_QCALL;
}

extern "C" void* QCALLTYPE GCInterface_GetNextFinalizableObject(QCall::ObjectHandleOnStack pObj)
{
    QCALL_CONTRACT;

    PCODE funcPtr = 0;

    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF target = FinalizerThread::GetNextFinalizableObject();

    if (target != NULL)
    {
        pObj.Set(target);

        MethodTable* pMT = target->GetMethodTable();

        funcPtr = pMT->GetRestoredSlot(g_pObjectFinalizerMD->GetSlot());
    }

    END_QCALL;

    return (void*)funcPtr;
}

/*==========================WaitForPendingFinalizers============================
**Action: Run all Finalizers that haven't been run.
**Arguments: None
**Exceptions: None
==============================================================================*/
extern "C" void QCALLTYPE GCInterface_WaitForPendingFinalizers()
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FinalizerThread::FinalizerThreadWait();

    END_QCALL;
}


/*===============================GetMaxGeneration===============================
**Action: Returns the largest GC generation
**Returns: The largest GC Generation
**Arguments: None
**Exceptions: None
==============================================================================*/
FCIMPL0(int, GCInterface::GetMaxGeneration)
{
    FCALL_CONTRACT;

    return(INT32)GCHeapUtilities::GetGCHeap()->GetMaxGeneration();
}
FCIMPLEND

/*===============================IsServerGC===============================
**Action: Returns true if the garbage collector is a server GC
**Returns: True if server GC, false otherwise
**Arguments: None
**Exceptions: None
==============================================================================*/
FCIMPL0(FC_BOOL_RET, GCInterface::IsServerGC)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(GCHeapUtilities::IsServerHeap());
}
FCIMPLEND

/*===============================GetAllocatedBytesForCurrentThread===============================
**Action: Computes the allocated bytes so far on the current thread
**Returns: The allocated bytes so far on the current thread
**Arguments: None
**Exceptions: None
==============================================================================*/
FCIMPL0(INT64, GCInterface::GetAllocatedBytesForCurrentThread)
{
    FCALL_CONTRACT;

    INT64 currentAllocated = 0;
    Thread *pThread = GetThread();
    gc_alloc_context* ac = &t_runtime_thread_locals.alloc_context.m_GCAllocContext;
    currentAllocated = ac->alloc_bytes + ac->alloc_bytes_uoh - (ac->alloc_limit - ac->alloc_ptr);

    return currentAllocated;
}
FCIMPLEND

/*===============================AllocateNewArray===============================
**Action: Allocates a new array object. Allows passing extra flags
**Arguments: typeHandlePtr -> TypeHandle pointer of array,
**           length -> Number of elements,
**           flags -> Flags that impact allocated memory,
**           ret -> The allocated array.
**Exceptions: IDS_EE_ARRAY_DIMENSIONS_EXCEEDED when size is too large. OOM if can't allocate.
==============================================================================*/
extern "C" void QCALLTYPE GCInterface_AllocateNewArray(void* typeHandlePtr, INT32 length, INT32 flags, QCall::ObjectHandleOnStack ret)
{
    QCALL_CONTRACT;
    _ASSERTE(typeHandlePtr != NULL);

    BEGIN_QCALL;

    GCX_COOP();

    TypeHandle typeHandle = TypeHandle::FromPtr(typeHandlePtr);
    _ASSERTE(typeHandle.IsArray());

    //Only the following flags are used by GC.cs, so we'll just assert it here.
    _ASSERTE((flags & ~(GC_ALLOC_ZEROING_OPTIONAL | GC_ALLOC_PINNED_OBJECT_HEAP)) == 0);
    ret.Set(AllocateSzArray(typeHandle, length, (GC_ALLOC_FLAGS)flags));

    END_QCALL;
}

FCIMPL0(INT64, GCInterface::GetTotalAllocatedBytesApproximate)
{
    FCALL_CONTRACT;

#ifdef TARGET_64BIT
    uint64_t unused_bytes = Thread::dead_threads_non_alloc_bytes;
#else
    // As it could be noticed we read 64bit values that may be concurrently updated.
    // Such reads are not guaranteed to be atomic on 32bit so extra care should be taken.
    uint64_t unused_bytes = InterlockedCompareExchange64((LONG64*)& Thread::dead_threads_non_alloc_bytes, 0, 0);
#endif

    uint64_t allocated_bytes = GCHeapUtilities::GetGCHeap()->GetTotalAllocatedBytes() - unused_bytes;

    // highest reported allocated_bytes. We do not want to report a value less than that even if unused_bytes has increased.
    static uint64_t high_watermark;

    uint64_t current_high = high_watermark;
    while (allocated_bytes > current_high)
    {
        uint64_t orig = InterlockedCompareExchange64((LONG64*)& high_watermark, allocated_bytes, current_high);
        if (orig == current_high)
            return allocated_bytes;

        current_high = orig;
    }

    return current_high;
}
FCIMPLEND;

extern "C" INT64 QCALLTYPE GCInterface_GetTotalAllocatedBytesPrecise()
{
    INT64 allocated = 0;

    BEGIN_QCALL;

    GCX_COOP();

    // We need to suspend/restart the EE to get each thread's
    // non-allocated memory from their allocation contexts

    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);

    allocated = GCHeapUtilities::GetGCHeap()->GetTotalAllocatedBytes() - Thread::dead_threads_non_alloc_bytes;

    for (Thread *pThread = ThreadStore::GetThreadList(NULL); pThread; pThread = ThreadStore::GetThreadList(pThread))
    {
        gc_alloc_context* ac = pThread->GetAllocContext();
        if (ac != nullptr)
        {
            allocated -= ac->alloc_limit - ac->alloc_ptr;
        }
    }

    ThreadSuspend::RestartEE(FALSE, TRUE);

    END_QCALL;

    return allocated;
}

#ifdef FEATURE_BASICFREEZE

/*===============================RegisterFrozenSegment===============================
**Action: Registers the frozen segment
**Returns: segment_handle
**Arguments: args-> pointer to section, size of section
**Exceptions: None
==============================================================================*/
extern "C" void* QCALLTYPE GCInterface_RegisterFrozenSegment(void* pSection, SIZE_T sizeSection)
{
    QCALL_CONTRACT;

    void* retVal = nullptr;

    BEGIN_QCALL;

    _ASSERTE(pSection != nullptr);
    _ASSERTE(sizeSection > 0);

    GCX_COOP();

    segment_info seginfo;
    seginfo.pvMem           = pSection;
    seginfo.ibFirstObject   = sizeof(ObjHeader);
    seginfo.ibAllocated     = sizeSection;
    seginfo.ibCommit        = seginfo.ibAllocated;
    seginfo.ibReserved      = seginfo.ibAllocated;

    retVal = (void*)GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&seginfo);

    END_QCALL;

    return retVal;
}

/*===============================UnregisterFrozenSegment===============================
**Action: Unregisters the frozen segment
**Returns: void
**Arguments: args-> segment handle
**Exceptions: None
==============================================================================*/
extern "C" void QCALLTYPE GCInterface_UnregisterFrozenSegment(void* segment)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    _ASSERTE(segment != nullptr);

    GCX_COOP();

    GCHeapUtilities::GetGCHeap()->UnregisterFrozenSegment((segment_handle)segment);

    END_QCALL;
}

#endif // FEATURE_BASICFREEZE

/*==============================SuppressFinalize================================
**Action: Indicate that an object's finalizer should not be run by the system
**Arguments: Object of interest
**Exceptions: None
==============================================================================*/
FCIMPL1(void, GCInterface::SuppressFinalize, Object *obj)
{
    FCALL_CONTRACT;

    _ASSERTE(obj->GetMethodTable ()->HasFinalizer());
    GCHeapUtilities::GetGCHeap()->SetFinalizationRun(obj);
}
FCIMPLEND


/*============================ReRegisterForFinalize==============================
**Action: Indicate that an object's finalizer should be run by the system.
**Arguments: Object of interest
**Exceptions: None
==============================================================================*/
extern "C" void QCALLTYPE GCInterface_ReRegisterForFinalize(QCall::ObjectHandleOnStack pObj)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    // Checked by the caller
    _ASSERTE(pObj.Get() != NULL);
    _ASSERTE(pObj.Get()->GetMethodTable()->HasFinalizer());

    if (!GCHeapUtilities::GetGCHeap()->RegisterForFinalization(-1, OBJECTREFToObject(pObj.Get())))
    {
        ThrowOutOfMemory();
    }
    END_QCALL;
}

FORCEINLINE UINT64 GCInterface::InterlockedAdd (UINT64 *pAugend, UINT64 addend) {
    WRAPPER_NO_CONTRACT;

    UINT64 oldMemValue;
    UINT64 newMemValue;

    do {
        oldMemValue = *pAugend;
        newMemValue = oldMemValue + addend;

        // check for overflow
        if (newMemValue < oldMemValue)
        {
            newMemValue = UINT64_MAX;
        }
    } while (InterlockedCompareExchange64((LONGLONG*) pAugend, (LONGLONG) newMemValue, (LONGLONG) oldMemValue) != (LONGLONG) oldMemValue);

    return newMemValue;
}

FORCEINLINE UINT64 GCInterface::InterlockedSub(UINT64 *pMinuend, UINT64 subtrahend) {
    WRAPPER_NO_CONTRACT;

    UINT64 oldMemValue;
    UINT64 newMemValue;

    do {
        oldMemValue = *pMinuend;
        newMemValue = oldMemValue - subtrahend;

        // check for underflow
        if (newMemValue > oldMemValue)
            newMemValue = 0;

    } while (InterlockedCompareExchange64((LONGLONG*) pMinuend, (LONGLONG) newMemValue, (LONGLONG) oldMemValue) != (LONGLONG) oldMemValue);

    return newMemValue;
}

extern "C" void QCALLTYPE GCInterface_AddMemoryPressure(UINT64 bytesAllocated)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCInterface::AddMemoryPressure(bytesAllocated);
    END_QCALL;
}

extern "C" void QCALLTYPE GCInterface_EnumerateConfigurationValues(void* configurationContext, EnumerateConfigurationValuesCallback callback)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCInterface::EnumerateConfigurationValues(configurationContext, callback);
    END_QCALL;
}

void GCInterface::EnumerateConfigurationValues(void* configurationContext, EnumerateConfigurationValuesCallback callback)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(configurationContext != nullptr);
        PRECONDITION(callback != nullptr);
    }
    CONTRACTL_END;

    IGCHeap* pHeap = GCHeapUtilities::GetGCHeap();
    pHeap->EnumerateConfigurationValues(configurationContext, callback);
}

GCHeapHardLimitInfo g_gcHeapHardLimitInfo;
bool g_gcHeapHardLimitInfoSpecified = false;

extern "C" int QCALLTYPE GCInterface_RefreshMemoryLimit(GCHeapHardLimitInfo heapHardLimitInfo)
{
    QCALL_CONTRACT;

    int result = 0;

    BEGIN_QCALL;
    g_gcHeapHardLimitInfo = heapHardLimitInfo;
    g_gcHeapHardLimitInfoSpecified = true;
    result = GCInterface::RefreshMemoryLimit();
    END_QCALL;

    return result;
}

int GCInterface::RefreshMemoryLimit()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    return GCHeapUtilities::GetGCHeap()->RefreshMemoryLimit();
}

extern "C" enable_no_gc_region_callback_status QCALLTYPE GCInterface_EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, INT64 totalSize)
{
    enable_no_gc_region_callback_status status = enable_no_gc_region_callback_status::succeed;
    QCALL_CONTRACT;

    BEGIN_QCALL;
    status = GCInterface::EnableNoGCRegionCallback(callback, totalSize);
    END_QCALL;
    return status;
}

enable_no_gc_region_callback_status GCInterface::EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, INT64 totalSize)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    return GCHeapUtilities::GetGCHeap()->EnableNoGCRegionCallback(callback, totalSize);
}

extern "C" uint64_t QCALLTYPE GCInterface_GetGenerationBudget(int generation)
{
    uint64_t result = 0;
    QCALL_CONTRACT;

    BEGIN_QCALL;
    result = GCInterface::GetGenerationBudget(generation);
    END_QCALL;

    return result;
}

uint64_t GCInterface::GetGenerationBudget(int generation)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    return GCHeapUtilities::GetGCHeap()->GetGenerationBudget(generation);
}

#ifdef HOST_64BIT
const unsigned MIN_MEMORYPRESSURE_BUDGET = 4 * 1024 * 1024;        // 4 MB
#else // HOST_64BIT
const unsigned MIN_MEMORYPRESSURE_BUDGET = 3 * 1024 * 1024;        // 3 MB
#endif // HOST_64BIT

const unsigned MAX_MEMORYPRESSURE_RATIO = 10;                      // 40 MB or 30 MB


// Resets pressure accounting after a gen2 GC has occurred.
void GCInterface::CheckCollectionCount()
{
    LIMITED_METHOD_CONTRACT;

    IGCHeap * pHeap = GCHeapUtilities::GetGCHeap();

    if (m_gc_counts[2] != pHeap->CollectionCount(2))
    {
        for (int i = 0; i < 3; i++)
        {
            m_gc_counts[i] = pHeap->CollectionCount(i);
        }

        m_iteration++;

        UINT p = m_iteration % MEM_PRESSURE_COUNT;

        m_addPressure[p] = 0;   // new pressure will be accumulated here
        m_remPressure[p] = 0;
    }
}

// AddMemoryPressure implementation
//
//   1. Start budget - MIN_MEMORYPRESSURE_BUDGET
//   2. Focuses more on newly added memory pressure
//   3. Budget adjusted by effectiveness of last 3 triggered GC (add / remove ratio, max 10x)
//   4. Budget maxed with 30% of current managed GC size
//   5. If Gen2 GC is happening naturally, ignore past pressure
//
// Here's a brief description of the ideal algorithm for Add/Remove memory pressure:
// Do a GC when (HeapStart < X * MemPressureGrowth) where
// - HeapStart is GC Heap size after doing the last GC
// - MemPressureGrowth is the net of Add and Remove since the last GC
// - X is proportional to our guess of the ummanaged memory death rate per GC interval,
//   and would be calculated based on historic data using standard exponential approximation:
//   Xnew = UMDeath/UMTotal * 0.5 + Xprev
//
void GCInterface::AddMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CheckCollectionCount();

    UINT p = m_iteration % MEM_PRESSURE_COUNT;

    UINT64 newMemValue = InterlockedAdd(&m_addPressure[p], bytesAllocated);

    static_assert(MEM_PRESSURE_COUNT == 4, "AddMemoryPressure contains unrolled loops which depend on MEM_PRESSURE_COUNT");

    UINT64 add = m_addPressure[0] + m_addPressure[1] + m_addPressure[2] + m_addPressure[3] - m_addPressure[p];
    UINT64 rem = m_remPressure[0] + m_remPressure[1] + m_remPressure[2] + m_remPressure[3] - m_remPressure[p];

    STRESS_LOG4(LF_GCINFO, LL_INFO10000, "AMP Add: %llu => added=%llu total_added=%llu total_removed=%llu",
        bytesAllocated, newMemValue, add, rem);

    SendEtwAddMemoryPressureEvent(bytesAllocated);

    if (newMemValue >= MIN_MEMORYPRESSURE_BUDGET)
    {
        UINT64 budget = MIN_MEMORYPRESSURE_BUDGET;

        if (m_iteration >= MEM_PRESSURE_COUNT) // wait until we have enough data points
        {
            // Adjust according to effectiveness of GC
            // Scale budget according to past m_addPressure / m_remPressure ratio
            if (add >= rem * MAX_MEMORYPRESSURE_RATIO)
            {
                budget = MIN_MEMORYPRESSURE_BUDGET * MAX_MEMORYPRESSURE_RATIO;
            }
            else if (add > rem)
            {
                CONSISTENCY_CHECK(rem != 0);

                // Avoid overflow by calculating addPressure / remPressure as fixed point (1 = 1024)
                budget = (add * 1024 / rem) * budget / 1024;
            }
        }

        // If still over budget, check current managed heap size
        if (newMemValue >= budget)
        {
            IGCHeap *pGCHeap = GCHeapUtilities::GetGCHeap();
            UINT64 heapOver3 = pGCHeap->GetCurrentObjSize() / 3;

            if (budget < heapOver3) // Max
            {
                budget = heapOver3;
            }

            if (newMemValue >= budget)
            {
                // last check - if we would exceed 20% of GC "duty cycle", do not trigger GC at this time
                if ((size_t)(pGCHeap->GetNow() - pGCHeap->GetLastGCStartTime(2)) > (pGCHeap->GetLastGCDuration(2) * 5))
                {
                    STRESS_LOG6(LF_GCINFO, LL_INFO10000, "AMP Budget: pressure=%llu ? budget=%llu (total_added=%llu, total_removed=%llu, mng_heap=%llu) pos=%d",
                        newMemValue, budget, add, rem, heapOver3 * 3, m_iteration);

                    GarbageCollectModeAny(2);

                    CheckCollectionCount();
                }
            }
        }
    }
}

extern "C" void QCALLTYPE GCInterface_RemoveMemoryPressure(UINT64 bytesAllocated)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCInterface::RemoveMemoryPressure(bytesAllocated);
    END_QCALL;
}

void GCInterface::RemoveMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CheckCollectionCount();

    UINT p = m_iteration % MEM_PRESSURE_COUNT;

    SendEtwRemoveMemoryPressureEvent(bytesAllocated);

    InterlockedAdd(&m_remPressure[p], bytesAllocated);

    STRESS_LOG2(LF_GCINFO, LL_INFO10000, "AMP Remove: %llu => removed=%llu",
        bytesAllocated, m_remPressure[p]);
}

inline void GCInterface::SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    FireEtwIncreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
}

// Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
NOINLINE void GCInterface::SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_TRY
    {
        FireEtwDecreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
    }
    EX_CATCH
    {
        // Ignore failures
    }
    EX_END_CATCH
}

// Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
NOINLINE void GCInterface::GarbageCollectModeAny(int generation)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();
    GCHeapUtilities::GetGCHeap()->GarbageCollect(generation, false, collection_non_blocking);
}

