// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriGC.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"

#include "SatoriHandlePartitioner.h"
#include "SatoriObject.h"
#include "SatoriObject.inl"
#include "SatoriGC.h"
#include "SatoriAllocationContext.h"
#include "SatoriHeap.h"
#include "SatoriRegion.h"
#include "SatoriPage.h"
#include "SatoriRegion.inl"
#include "../gceventstatus.h"

bool SatoriGC::IsValidSegmentSize(size_t size)
{
    // N/A
    return true;
}

bool SatoriGC::IsValidGen0MaxSize(size_t size)
{
    // N/A;
    return true;
}

size_t SatoriGC::GetValidSegmentSize(bool large_seg)
{
    // Satori has no concept of a segment. This may be close enough.
    return Satori::REGION_SIZE_GRANULARITY;
}

void SatoriGC::SetReservedVMLimit(size_t vmlimit)
{
    // NYI
}

void SatoriGC::WaitUntilConcurrentGCComplete()
{
    WaitUntilGCComplete();
}

bool SatoriGC::IsConcurrentGCInProgress()
{
    // Satori may move thread local objects asynchronously,
    // but noone should see that (that is the point).
    //
    // The only thing that may get to TL objects is object verification.
    // Return "true" for now.
    return true;
}

void SatoriGC::TemporaryEnableConcurrentGC()
{
    // N/A
}

void SatoriGC::TemporaryDisableConcurrentGC()
{
    // N/A
}

bool SatoriGC::IsConcurrentGCEnabled()
{
    // N/A
    return true;
}

HRESULT SatoriGC::WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout)
{
    WaitUntilGCComplete();
    return S_OK;
}

size_t SatoriGC::GetNumberOfFinalizable()
{
    SatoriFinalizationQueue* queue = m_heap->FinalizationQueue();
    return queue->Count();
}

Object* SatoriGC::GetNextFinalizable()
{
    SatoriFinalizationQueue* queue = m_heap->FinalizationQueue();

tryAgain:
    Object* f = queue->TryGetNextItem();
    if (f == nullptr)
    {
        // transient failure to enqueue finalizables in ephemeral GC is unobservable since objects
        // could be promoted nondeterministicaly and stay alive anyways.
        // with gen2 we need to see a clean collection before we can claim there is nothing pending.
        if (queue->OverflowedGen() == 2)
        {
            m_heap->Recycler()->Collect(2, true, true);
            goto tryAgain;
        }
    }

    return f;
}

int SatoriGC::GetGcLatencyMode()
{
    return m_heap->Recycler()->IsLowLatencyMode() ?
        2 :  //pause_low_latency
        1;   //pause_interactive
}

int SatoriGC::SetGcLatencyMode(int newLatencyMode)
{
    if (newLatencyMode >= 2) //pause_low_latency
    {
        m_heap->Recycler()->IsLowLatencyMode() = true;
    }
    else
    {
        m_heap->Recycler()->IsLowLatencyMode() = false;
    }

    return 0;
}

int SatoriGC::GetLOHCompactionMode()
{
    // N/A
    return 0;
}

void SatoriGC::SetLOHCompactionMode(int newLOHCompactionMode)
{
    // N/A
}

bool SatoriGC::RegisterForFullGCNotification(uint32_t gen2Percentage, uint32_t lohPercentage)
{
    // NYI
    return false;
}

bool SatoriGC::CancelFullGCNotification()
{
    // NYI
    return false;
}

int SatoriGC::WaitForFullGCApproach(int millisecondsTimeout)
{
    // NYI
    // 1 - failed
    return 1;
}

int SatoriGC::WaitForFullGCComplete(int millisecondsTimeout)
{
    // NYI
    // 1 - failed
    return 1;
}

unsigned SatoriGC::WhichGeneration(Object* obj)
{
    SatoriObject* so = (SatoriObject*)obj;
    if (so->IsExternal())
    {
        // never collected -> 3
        return 2147483647;
    }

    return (unsigned)so->ContainingRegion()->Generation();
}

int SatoriGC::CollectionCount(int generation, int get_bgc_fgc_coutn)
{
    //get_bgc_fgc_coutn N/A
    if ((unsigned)generation > (unsigned)2)
    {
        return 0;
    }

    return (int)m_heap->Recycler()->GetCollectionCount(generation);
}

int SatoriGC::StartNoGCRegion(uint64_t totalSize, bool lohSizeKnown, uint64_t lohSize, bool disallowFullBlockingGC)
{
    // NYI
    // 1 - NotEnoughMemory
    return 1;
}

int SatoriGC::EndNoGCRegion()
{
    // NYI
    return 0;
}

size_t SatoriGC::GetTotalBytesInUse()
{
    // Returns the total number of bytes currently in use by live objects in
    // the GC heap.  This does not return the total size of the GC heap, but
    // only the live objects in the GC heap.
    return m_heap->Recycler()->GetTotalOccupancy();
}

size_t SatoriGC::GetCurrentObjSize()
{
    // Used only by mem pressure heuristic
    // It seems some rough estimate of "managed heap size"
    return GetTotalBytesInUse();
}

uint64_t SatoriGC::GetTotalAllocatedBytes()
{
    // monotonically increasing number produced by allocator when allocating objects.
    // threads know their number and we update the total when doing GCs
    return m_heap->Recycler()->GetTotalAllocatedBytes();
}

uint64_t SatoriGC::GetTotalAllocatedBytesPrecise()
{
    GarbageCollect(1);
    return GetTotalAllocatedBytes();
}

HRESULT SatoriGC::GarbageCollect(int generation, bool low_memory_p, int mode)
{
    // we do either Gen1 or Gen2 for now.
    generation = (generation < 0) ? 2 : min(generation, 2);
    generation = max(1, generation);

    // NYI: forced compaction

    m_heap->Recycler()->Collect(
        generation,
        !(mode & collection_mode::collection_optimized),
        mode & collection_mode::collection_blocking
    );

    return S_OK;
}

unsigned SatoriGC::GetMaxGeneration()
{
    return 2;
}

void SatoriGC::SetFinalizationRun(Object* obj)
{
    ((SatoriObject*)obj)->SuppressFinalization();
}

bool SatoriGC::RegisterForFinalization(int gen, Object* obj)
{
    SatoriObject* so = (SatoriObject*)obj;
    _ASSERTE(so->RawGetMethodTable()->HasFinalizer());

    if (so->IsExternal())
    {
        // noop
        return true;
    }

    if (so->IsFinalizationSuppressed())
    {
        so->UnSuppressFinalization();
        return true;
    }
    else
    {
        SatoriRegion* region = so->ContainingRegion();
        if (region->Generation() == 3)
        {
            // no need to register immortal objects
            return true;
        }

        return region->RegisterForFinalization(so);
    }
}

int SatoriGC::GetLastGCPercentTimeInGC()
{
    return m_heap->Recycler()->GetPercentTimeInGcSinceLastGc();
}

size_t SatoriGC::GetLastGCGenerationSize(int gen)
{
    return m_heap->Recycler()->GetOccupancy(gen);
}

bool IsInHeap(void* context, void* object)
{
    return SatoriHeap::IsInHeap(((size_t)object) & ~(size_t)3);
}

void CheckEscapeSatori(Object** dst, Object* ref)
{
    SatoriObject* obj = (SatoriObject*)ref;
    if (!obj || obj->IsExternal())
        return;

    // we should be the owner of the region to care about escapes
    SatoriRegion* region = obj->ContainingRegion();
    if (region->IsEscapeTrackedByCurrentThread())
    {
        if ((((size_t)dst ^ (size_t)ref) < Satori::REGION_SIZE_GRANULARITY) &&
            !(region->IsExposed((SatoriObject**)dst)))
        {
            // if assigning to the same region and location is not marked then
            // this is a trivial local assignment and nothing needs escaping.
            return;
        }

        region->EscapeRecursively(obj);
    }
}

void ErectWriteBarrier(void* context, Object **dst, Object* ref)
{
    SatoriObject* obj = (SatoriObject*)ref;
    if (!obj || obj->IsExternal())
        return;

    SatoriHeap *pHeap = ((SatoriGC*)context)->GetHeap();
    SatoriRecycler *pRecycler = pHeap->Recycler();

    // check for obj in the same region or in gen2
    if ((((size_t)dst ^ (size_t)obj) < Satori::REGION_SIZE_GRANULARITY) ||
        (obj->ContainingRegion()->Generation() == 2))
    {
         if (!pRecycler->IsBarrierConcurrent())
             return;
    }

    SatoriPage* page = pHeap->PageForAddressChecked((size_t)dst);
    if (!page)
        return;

    if (!pRecycler->IsBarrierConcurrent())
    {
        page->SetCardForAddress((size_t)dst);
        if (!pRecycler->IsBarrierConcurrent())
            return;
    }

    page->DirtyCardForAddressConcurrent((size_t)dst);
}

void BulkMoveWithWriteBarrier(void* context, void* dst, const void* src, size_t byteCount)
{
    ((SatoriGC*)context)->BulkMoveWithWriteBarrier(dst, src, byteCount);
}

void SatoriGC::GetFastGCFunctions(FastGCFunctions* functions)
{
    functions->context = this;
    functions->write_barrier = &ErectWriteBarrier;
    functions->is_in_gc_heap = &IsInHeap;
    functions->check_escape = &CheckEscapeSatori;
    functions->bulk_move_with_write_barrier = &::BulkMoveWithWriteBarrier;
}

extern "C" void RhpAssignRef(Object **dst, Object *ref);
extern "C" void RhpCheckedAssignRef(Object **dst, Object *ref);
extern "C" uint8_t RhpAssignRef_End;
extern "C" uint8_t RhpCheckedAssignRef_End;
extern "C" uint8_t RhpCheckedAssignRefAVLocation;
extern "C" uint8_t RhpAssignRefAVLocation;
extern "C" uint8_t RhpAssignRefAVLocationNotHeap;

static uintptr_t avLocations[] = 
{ 
    (uintptr_t)&RhpCheckedAssignRefAVLocation,
    (uintptr_t)&RhpAssignRefAVLocation,
    (uintptr_t)&RhpAssignRefAVLocationNotHeap,
    0
};

// The assembly write barriers cache the values of these globals (card table, card bundle table,
// write barrier state) directly in patchable slots embedded in their own code, instead of
// referencing the globals by address -- on amd64 this is the immediate operand of a MOV
// instruction, on arm64 it is a co-located literal-pool entry -- so the cached values remain
// correct no matter where the barrier code is copied to (a PC-relative reference to the actual
// global could otherwise end up out of range once the code is copied to a dynamically allocated
// buffer). The labels below mark those slots; see GCWriteBarrierPatchableValue in gcinterface.h
// and UpdateCustomWriteBarrierGlobals in writebarriermanager.cpp, which keeps them in sync with
// the globals' current values.
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
extern "C" uint8_t PatchableValue_CheckedAssignRef_CardBundleTable;
#if FEATURE_SATORI_EXTERNAL_OBJECTS
extern "C" uint8_t PatchableValue_AssignRef_CardBundleTable;
#endif
extern "C" uint8_t PatchableValue_AssignAndMarkCards_WriteBarrierState;
extern "C" uint8_t PatchableValue_MarkCards_CardTable;
extern "C" uint8_t PatchableValue_CardSet_WriteBarrierState;

extern "C" uint32_t* g_gc_card_table;
extern "C" uint32_t* g_gc_card_bundle_table;
extern "C" uint8_t* g_write_barrier_state;

static GCWriteBarrierPatchableValue assignRefPatchableValues[] =
{
    { &PatchableValue_CheckedAssignRef_CardBundleTable, &g_gc_card_bundle_table },
#if FEATURE_SATORI_EXTERNAL_OBJECTS
    { &PatchableValue_AssignRef_CardBundleTable, &g_gc_card_bundle_table },
#endif
    { &PatchableValue_AssignAndMarkCards_WriteBarrierState, &g_write_barrier_state },
    { &PatchableValue_MarkCards_CardTable, &g_gc_card_table },
    { &PatchableValue_CardSet_WriteBarrierState, &g_write_barrier_state },
    { nullptr, nullptr }
};
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64)

void SatoriGC::GetAssignRefFunctions(void** assignRef, size_t* assignRefSize, void** assignRefChecked, size_t* assignRefCheckedSize, uintptr_t** avLocationsList, GCWriteBarrierPatchableValue** patchableValues)
{
    *assignRef = (void*)&RhpAssignRef;
    *assignRefSize = (uintptr_t)&RhpAssignRef_End - (uintptr_t)&RhpAssignRef;
    *assignRefChecked = (void*)&RhpCheckedAssignRef;
    *assignRefCheckedSize = (uintptr_t)&RhpCheckedAssignRef_End - (uintptr_t)&RhpCheckedAssignRef;
    *avLocationsList = avLocations;
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    *patchableValues = assignRefPatchableValues;
#else
    *patchableValues = nullptr;
#endif
}

GCHeapType SatoriGC::GetGCHeapType()
{
    return GC_HEAP_CUSTOM;
}

HRESULT SatoriGC::Initialize()
{
    SatoriObject::Initialize();
    SatoriHandlePartitioner::Initialize();
    m_heap = SatoriHeap::Create();
    if (m_heap == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    m_waitForGCEvent = new (nothrow) GCEvent;
    if (!m_waitForGCEvent)
    {
        return E_OUTOFMEMORY;
    }

    if (!m_waitForGCEvent->CreateManualEventNoThrow(TRUE))
    {
        return E_FAIL;
    }

    return S_OK;
}

// actually checks if object is considered reachable as a result of a marking phase.
bool SatoriGC::IsPromoted(Object* object)
{
    SatoriObject* o = (SatoriObject*)object;

    // objects outside of the collected generation (including null) are considered marked.
    // (existing behavior)
    return o == nullptr ||
        o->IsExternal() ||
        o->IsMarkedOrOlderThan(m_heap->Recycler()->GetCondemnedGeneration());
}

bool SatoriGC::IsPromoted2(Object* object, bool bVerifyNextHeader)
{
    return IsPromoted(object);
}

bool SatoriGC::IsHeapPointer(void* object, bool small_heap_only)
{
    //small_heap_only is unused - there is no special heap for large objects.
    return SatoriHeap::IsInHeap((size_t)object);
}

unsigned SatoriGC::GetCondemnedGeneration()
{
    return m_heap->Recycler()->GetCondemnedGeneration();
}

bool SatoriGC::IsGCInProgressHelper(bool bConsiderGCStart)
{
    return m_gcInProgress;
}

unsigned SatoriGC::GetGcCount()
{
    if (!m_heap)
    {
        return 0;
    }

    return (unsigned)(int)m_heap->Recycler()->GetCollectionCount(/*gen*/ 1);
}

bool SatoriGC::IsThreadUsingAllocationContextHeap(gc_alloc_context* acontext, int thread_number)
{
    // we just return true if given context has not been scanned for the current scan ticket. 
    int currentScanTicket = m_heap->Recycler()->GetRootScanTicket();
    int threadScanTicket = VolatileLoadWithoutBarrier(&acontext->alloc_count);
    if (threadScanTicket != currentScanTicket)
    {
        if (Interlocked::CompareExchange(&acontext->alloc_count, currentScanTicket, threadScanTicket) == threadScanTicket)
        {
            m_heap->Recycler()->MaybeAskForHelp();
            return true;
        }
    }

    return false;
}

bool SatoriGC::IsEphemeral(Object* object)
{
    return WhichGeneration(object) != GetMaxGeneration();
}

uint32_t SatoriGC::WaitUntilGCComplete(bool bConsiderGCStart)
{
    //bConsiderGCStart does not apply for Satori
    if (m_gcInProgress)
    {
        _ASSERTE(m_waitForGCEvent->IsValid());
        return m_waitForGCEvent->Wait(INFINITE, FALSE);
    }

    return NOERROR;
}

void SatoriGC::FixAllocContext(gc_alloc_context* acontext, void* arg, void* heap)
{
    // this is only called when thread is terminating and about to clear its context.
    ((SatoriAllocationContext*)acontext)->Deactivate(m_heap->Recycler(), /*detach*/ true);
    m_heap->Recycler()->ReportThreadAllocBytes(acontext->alloc_bytes + acontext->alloc_bytes_uoh, /*isLive*/ false);
}

void SatoriGC::SetGCInProgress(bool fInProgress)
{
    m_gcInProgress = fInProgress;
}

bool SatoriGC::RuntimeStructuresValid()
{
    // N/A
    return true;
}

void SatoriGC::SetSuspensionPending(bool fSuspensionPending)
{
    // N/A
    // noop, it makes no difference.
}

void SatoriGC::SetYieldProcessorScalingFactor(float yieldProcessorScalingFactor)
{
    // N/A
}

void SatoriGC::Shutdown()
{
    m_shuttingDown = true;
    m_heap->Recycler()->ShutDown();
}

size_t SatoriGC::GetLastGCStartTime(int generation)
{
    return m_heap->Recycler()->GetGcStartMillis(generation);
}

// This is used internally in some strange AddMemoryPressure heuristics.
// I am not completely sure what we need to return here.
// We will return the last pause time in msec. Although it may often be 0 in Satori.
size_t SatoriGC::GetLastGCDuration(int generation)
{
    return m_heap->Recycler()->GetGcDurationMillis(generation);
}

size_t SatoriGC::GetNow()
{
    return m_heap->Recycler()->GetNowMillis();
}

Object* SatoriGC::Alloc(gc_alloc_context* acontext, size_t size, uint32_t flags)
{
    return m_heap->Allocator()->Alloc((SatoriAllocationContext*)acontext, size, flags);
}

void SatoriGC::PublishObject(uint8_t* obj)
{
    SatoriObject* so = (SatoriObject*)obj;
    SatoriRegion* region = so->ContainingRegion();

    // we do not attach huge regions to thread contexts,
    // but the region is not parseable until the object has a MethodTable,
    // so we delay taking the region out of generation -1 and passing to recycler
    // until we get here.
    if (region->Size() > Satori::REGION_SIZE_GRANULARITY)
    {
        _ASSERTE(!region->IsAttachedToAllocatingOwner() && region->Generation() == -1);

        // promote to gen2 and take out of -1, since now the region is parseable
        region->SetGenerationRelease(2);
        region->SetOccupancy(so->Size(), 1);
        m_heap->Recycler()->AddTenuredRegion(region);

        // the region has seen no writes, so no need to worry about cards.
        // unless the obj has a collectible type.
        // in such case we simulate retroactive write by dirtying the card for the MT location.
        if (so->RawGetMethodTable()->Collectible())
        {
            region->ContainingPage()->DirtyCardForAddressConcurrent(so->Start());
        }
    }
    else if (so->IsUnfinished())
    {
        region->DecrementUnfinishedAlloc();
        so->UnsetUnfinished();
    }

    _ASSERTE(!so->IsUnfinished());
}

void SatoriGC::SetWaitForGCEvent()
{
    m_waitForGCEvent->Set();
}

void SatoriGC::ResetWaitForGCEvent()
{
    m_waitForGCEvent->Reset();
}

bool SatoriGC::IsLargeObject(Object* pObj)
{
    // N/A
    return false;
}

void SatoriGC::ValidateObjectMember(Object* obj)
{
    // NYI
}

Object* SatoriGC::NextObj(Object* object)
{
    // N/A
    return nullptr;
}

Object* SatoriGC::GetContainingObject(void* pInteriorPtr, bool fCollectedGenOnly)
{
    SatoriRegion* region = m_heap->RegionForAddressChecked((size_t)pInteriorPtr);
    if (!region)
    {
        return nullptr;
    }

    if (fCollectedGenOnly &&
        region->Generation() > m_heap->Recycler()->GetCondemnedGeneration())
    {
        return nullptr;
    }

    return region->FindObject((size_t)pInteriorPtr);
}

void SatoriGC::DiagWalkObject(Object* obj, walk_fn fn, void* context)
{
    // NYI
}

void SatoriGC::DiagWalkObject2(Object* obj, walk_fn2 fn, void* context)
{
    // NYI
}

void SatoriGC::DiagWalkHeap(walk_fn fn, void* context, int gen_number, bool walk_large_object_heap_p)
{
    // NYI
}

void SatoriGC::DiagWalkSurvivorsWithType(void* gc_context, record_surv_fn fn, void* diag_context, walk_surv_type type, int gen_number)
{
    // NYI
}

void SatoriGC::DiagWalkFinalizeQueue(void* gc_context, fq_walk_fn fn)
{
    // NYI
}

void SatoriGC::DiagScanFinalizeQueue(fq_scan_fn fn, ScanContext* context)
{
    // NYI
}

void SatoriGC::DiagScanHandles(handle_scan_fn fn, int gen_number, ScanContext* context)
{
    // NYI
}

void SatoriGC::DiagScanDependentHandles(handle_scan_fn fn, int gen_number, ScanContext* context)
{
    // NYI
}

void SatoriGC::DiagDescrGenerations(gen_walk_fn fn, void* context)
{
    // NYI
}

void SatoriGC::DiagTraceGCSegments()
{
    // NYI
}

bool SatoriGC::StressHeap(gc_alloc_context* acontext)
{
    // N/A
    return false;
}

segment_handle SatoriGC::RegisterFrozenSegment(segment_info* pseginfo)
{
    // N/A
    // non-null means success;
#if FEATURE_SATORI_EXTERNAL_OBJECTS
    return (segment_handle)1;
#else
    return NULL;
#endif
}

void SatoriGC::UnregisterFrozenSegment(segment_handle seg)
{
    // N/A
}

void SatoriGC::UpdateFrozenSegment(segment_handle seg, uint8_t* allocated, uint8_t* committed)
{
    // N/A
}

bool SatoriGC::IsInFrozenSegment(Object* object)
{
    // is never collected (immortal)
    SatoriObject* so = (SatoriObject*)object;
    if (so->IsExternal())
    {
        return true;
    }

    return (unsigned)so->ContainingRegion()->Generation() == 3;
}

void SatoriGC::ControlEvents(GCEventKeyword keyword, GCEventLevel level)
{
    GCEventStatus::Set(GCEventProvider_Default, keyword, level);
}

void SatoriGC::ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level)
{
    GCEventStatus::Set(GCEventProvider_Private, keyword, level);
}

int SatoriGC::GetNumberOfHeaps()
{
    return SatoriHandlePartitioner::PartitionCount();
}

int SatoriGC::GetHomeHeapNumber()
{
    return SatoriHandlePartitioner::CurrentThreadPartition();
}

size_t SatoriGC::GetPromotedBytes(int heap_index)
{
    // NYI
    return 0;
}

static uint64_t g_totalLimit;

void SatoriGC::GetMemoryInfo(uint64_t* highMemLoadThresholdBytes, uint64_t* totalAvailableMemoryBytes, uint64_t* lastRecordedMemLoadBytes, uint64_t* lastRecordedHeapSizeBytes, uint64_t* lastRecordedFragmentationBytes, uint64_t* totalCommittedBytes, uint64_t* promotedBytes, uint64_t* pinnedObjectCount, uint64_t* finalizationPendingCount, uint64_t* index, uint32_t* generation, uint32_t* pauseTimePct, bool* isCompaction, bool* isConcurrent, uint64_t* genInfoRaw, uint64_t* pauseInfoRaw, int kind)
{
    LastRecordedGcInfo* lastGcInfo = m_heap->Recycler()->GetLastGcInfo((gc_kind)kind);

    if (g_totalLimit == 0)
        g_totalLimit = GCToOSInterface::GetPhysicalMemoryLimit();

    uint64_t totalLimit = g_totalLimit;
    *highMemLoadThresholdBytes = totalLimit * 99 / 100; // just say 99% for now
    *totalAvailableMemoryBytes = totalLimit;

    uint32_t memLoad = GetMemoryLoad();
    *lastRecordedMemLoadBytes = memLoad * totalLimit / 100;

    *lastRecordedHeapSizeBytes = GetTotalBytesInUse();
    *finalizationPendingCount = GetNumberOfFinalizable();

    *lastRecordedFragmentationBytes = 0;
    *totalCommittedBytes = m_heap->GetBytesCommitted();
    *promotedBytes = 0;
    *pinnedObjectCount = 0;
    *index = lastGcInfo->m_index;
    *generation = lastGcInfo->m_condemnedGeneration;
    // this is %% spent in STW from the moment of GC initialization, in wall-clock terms.
    *pauseTimePct = lastGcInfo->m_pausePercentage;
    *isCompaction = lastGcInfo->m_compaction;
    *isConcurrent = lastGcInfo->m_concurrent;
    *genInfoRaw = 0;
    for (int i = 0; i < 2; i++)
    {
        // convert it to 100-ns units that TimeSpan needs.
        pauseInfoRaw[i] = (uint64_t)(lastGcInfo->m_pauseDurations[i]) * 10;
    }
}

static uint32_t g_memLoad;
static size_t g_memLoadMsec;

uint32_t SatoriGC::GetMemoryLoad()
{
    size_t time = GetNow();

    // limit querying frequency to once per 16 msec.
    if ((time >> 4) != (g_memLoadMsec >> 4))
    {
        uint64_t availPhysical, availPage;
        GCToOSInterface::GetMemoryStatus(0, &g_memLoad, &availPhysical, &availPage);
        g_memLoadMsec = time;
    }

    return g_memLoad;
}

void SatoriGC::DiagGetGCSettings(EtwGCSettingsInfo* etw_settings)
{
    // NYI
}

unsigned int SatoriGC::GetGenerationWithRange(Object* object, uint8_t** ppStart, uint8_t** ppAllocated, uint8_t** ppReserved)
{
    // NYI
    return WhichGeneration(object);
}

int64_t SatoriGC::GetTotalPauseDuration()
{
    return (m_heap->Recycler()->GetGcAccumulatingDurationMillis(0)
        + m_heap->Recycler()->GetGcAccumulatingDurationMillis(1)
        + m_heap->Recycler()->GetGcAccumulatingDurationMillis(2)) * 10;
}

void SatoriGC::EnumerateConfigurationValues(void* context, ConfigurationValueFunc configurationValueFunc)
{
    GCConfig::EnumerateConfigurationValues(context, configurationValueFunc);
    configurationValueFunc(context, "SatoriGC", "SatoriGC", GCConfigurationType::Boolean, true);
}

bool SatoriGC::CheckEscapeSatoriRange(size_t dst, size_t src, size_t len)
{
    SatoriRegion* curRegion = (SatoriRegion*)GCToEEInterface::GetAllocContext()->gc_reserved_1;
    if (!curRegion || !curRegion->IsEscapeTracking())
    {
        // not tracking escapes, not a local assignment.
        return false;
    }

    _ASSERTE(curRegion->IsEscapeTrackedByCurrentThread());

    // if dst is within the curRegion and is not exposed, we are done
    if ((dst ^ curRegion->Start()) < Satori::REGION_SIZE_GRANULARITY)
    {
        if (!curRegion->AnyExposed(dst, len))
        {
            // thread-local assignment
            return true;
        }
    }

    if (!SatoriHeap::IsInHeap(dst))
    {
        // dest not in heap, must be stack, so, local
        return true;
    }

    if ((src ^ curRegion->Start()) < Satori::REGION_SIZE_GRANULARITY)
    {
        // if src is in current region, the elements could be escaping
        if (!curRegion->AnyExposed(src, len))
        {
            // one-element array copy is embarrasingly common. specialcase that.
            if (len == sizeof(size_t))
            {
                SatoriObject* obj = *(SatoriObject**)src;
                if (obj->SameRegion(curRegion))
                {
                    curRegion->EscapeRecursively(obj);
                }
            }
            else
            {
                SatoriObject* containingSrcObj = curRegion->FindObject(src);
                containingSrcObj->ForEachObjectRef(
                    [&](SatoriObject** ref)
                    {
                        SatoriObject* child = *ref;
                        if (child->SameRegion(curRegion))
                        {
                            curRegion->EscapeRecursively(child);
                        }
                    },
                    src,
                    src + len
                );
            }
        }

        return false;
    }

    if (SatoriHeap::IsInHeap(src))
    {
        // src is not in current region but in heap,
        // it can't escape anything that belongs to the current thread, but it is not a local assignment.
        return false;
    }

    // This is a very rare case where we are copying refs out of non-heap area like stack or native heap.
    // We do not have a containing type and that is somewhat inconvenient.
    // 
    // There are not many scenarios that lead here. In particular, boxing uses a newly
    // allocated and not yet escaped target, so it does not end up here.
    // One possible way to get here is a copy-back after a reflection call with a boxed nullable
    // argument that happen to escape.
    // 
    // We could handle this is by conservatively escaping any value that matches an unescaped pointer in curRegion.
    // However, considering how uncommon this is, we will just give up tracking.
    curRegion->StopEscapeTracking();
    return false;
}

void SatoriGC::SetCardsAfterBulkCopy(size_t dst, size_t src, size_t len)
{
    SatoriPage* page = m_heap->PageForAddressChecked(dst);
    if (page)
    {
        SatoriRecycler* recycler = m_heap->Recycler();
        if (!recycler->IsBarrierConcurrent())
        {
            page->SetCardsForRange(dst, dst + len);
        }

        if (recycler->IsBarrierConcurrent())
        {
            page->DirtyCardsForRangeConcurrent(dst, dst + len);
        }
    }
}

void SatoriGC::BulkMoveWithWriteBarrier(void* dst, const void* src, size_t byteCount)
{
    if (dst == src || byteCount == 0)
        return;

    // Make sure everything is pointer aligned
    _ASSERTE(((size_t)dst & (sizeof(size_t) - 1)) == 0);
    _ASSERTE(((size_t)src & (sizeof(size_t) - 1)) == 0);
    _ASSERTE(((size_t)byteCount & (sizeof(size_t) - 1)) == 0);

    bool localAssignment = false;
    if (byteCount >= sizeof(size_t))
    {
        localAssignment = CheckEscapeSatoriRange((size_t)dst, (size_t)src, byteCount);
    }

    if (!localAssignment)
    {
#if !defined(TARGET_X86) && !defined(TARGET_AMD64)
        MemoryBarrier();
#endif
    }

    // NOTE! memmove needs to copy with size_t granularity
    // I do not see how it would not, since everything is aligned.
    // If we need to handle unaligned moves, we may need to write our own memmove
    memmove(dst, src, byteCount);

    if (byteCount >= sizeof(size_t) &&
       (!(localAssignment || m_heap->Recycler()->IsNextGcFullGc()) ||
           m_heap->Recycler()->IsBarrierConcurrent()))
    {
        SetCardsAfterBulkCopy((size_t)dst, (size_t)src, byteCount);
    }
}

int SatoriGC::RefreshMemoryLimit()
{
    return 0;
}

enable_no_gc_region_callback_status SatoriGC::EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, uint64_t callback_threshold)
{
    return enable_no_gc_region_callback_status();
}

FinalizerWorkItem* SatoriGC::GetExtraWorkForFinalization()
{
    return nullptr;
}

uint64_t SatoriGC::GetGenerationBudget(int generation)
{
    // same as Mono impl
    // avoid IDE0060: Remove unused parameter 'generation'
    return -1 + 0 * generation;
}

size_t SatoriGC::GetLOHThreshold()
{
    return Satori::LARGE_OBJECT_THRESHOLD;
}

void SatoriGC::DiagWalkHeapWithACHandling(walk_fn fn, void *context, int gen_number, bool walk_large_object_heap_p)
{
}

void SatoriGC::NullBridgeObjectsWeakRefs(size_t length, void* unreachableObjectHandles)
{
#ifdef FEATURE_JAVAMARSHAL
    Ref_NullBridgeObjectsWeakRefs(length, unreachableObjectHandles);
#else
    assert(false);
#endif
}
