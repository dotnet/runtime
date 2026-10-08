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
// SatoriGC.h
//

#ifndef __SATORI_GC_H__
#define __SATORI_GC_H__

#include "common.h"
#include "../gc.h"

class SatoriHeap;
class SatoriAllocator;
class SatoriRecycler;

class SatoriGC : public IGCHeapInternal
{
public:
    SatoriGC()
    {
        m_heap = nullptr;
        m_gcInProgress = false;
        m_shuttingDown = false;
        m_waitForGCEvent = nullptr;
    }

private:
    SatoriHeap* m_heap;

    bool    m_gcInProgress;
    bool    m_shuttingDown;

    GCEvent* m_waitForGCEvent;

    bool CheckEscapeSatoriRange(size_t dst, size_t src, size_t len);
    void SetCardsAfterBulkCopy(size_t dst, size_t src, size_t len);

public:
    SatoriHeap* GetHeap() { return m_heap; }
    
    // Inherited via IGCHeap
    virtual bool IsValidSegmentSize(size_t size) override;
    virtual bool IsValidGen0MaxSize(size_t size) override;
    virtual size_t GetValidSegmentSize(bool large_seg = false) override;
    virtual void SetReservedVMLimit(size_t vmlimit) override;
    virtual void WaitUntilConcurrentGCComplete() override;
    virtual bool IsConcurrentGCInProgress() override;
    virtual void TemporaryEnableConcurrentGC() override;
    virtual void TemporaryDisableConcurrentGC() override;
    virtual bool IsConcurrentGCEnabled() override;
    virtual HRESULT WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout) override;
    virtual size_t GetNumberOfFinalizable() override;
    virtual Object* GetNextFinalizable() override;
    virtual int GetGcLatencyMode() override;
    virtual int SetGcLatencyMode(int newLatencyMode) override;
    virtual int GetLOHCompactionMode() override;
    virtual void SetLOHCompactionMode(int newLOHCompactionMode) override;
    virtual bool RegisterForFullGCNotification(uint32_t gen2Percentage, uint32_t lohPercentage) override;
    virtual bool CancelFullGCNotification() override;
    virtual int WaitForFullGCApproach(int millisecondsTimeout) override;
    virtual int WaitForFullGCComplete(int millisecondsTimeout) override;
    virtual unsigned WhichGeneration(Object* obj) override;
    virtual int CollectionCount(int generation, int get_bgc_fgc_coutn = 0) override;
    virtual int StartNoGCRegion(uint64_t totalSize, bool lohSizeKnown, uint64_t lohSize, bool disallowFullBlockingGC) override;
    virtual int EndNoGCRegion() override;
    virtual size_t GetTotalBytesInUse() override;
    virtual uint64_t GetTotalAllocatedBytes() override;
    virtual HRESULT GarbageCollect(int generation = -1, bool low_memory_p = false, int mode = collection_blocking) override;
    virtual unsigned GetMaxGeneration() override;
    virtual void SetFinalizationRun(Object* obj) override;
    virtual bool RegisterForFinalization(int gen, Object* obj) override;
    virtual int GetLastGCPercentTimeInGC() override;
    virtual size_t GetLastGCGenerationSize(int gen) override;
    virtual HRESULT Initialize() override;
    virtual bool IsPromoted(Object* object) override;
    virtual bool IsPromoted2(Object* object, bool bVerifyNextHeader) override;
    virtual bool IsHeapPointer(void* object, bool small_heap_only = false) override;
    virtual unsigned GetCondemnedGeneration() override;
    virtual bool IsGCInProgressHelper(bool bConsiderGCStart = false) override;
    virtual unsigned GetGcCount() override;
    virtual bool IsThreadUsingAllocationContextHeap(gc_alloc_context* acontext, int thread_number) override;
    virtual bool IsEphemeral(Object* object) override;
    virtual uint32_t WaitUntilGCComplete(bool bConsiderGCStart = false) override;
    virtual void FixAllocContext(gc_alloc_context* acontext, void* arg, void* heap) override;
    virtual size_t GetCurrentObjSize() override;
    virtual void SetGCInProgress(bool fInProgress) override;
    virtual bool RuntimeStructuresValid() override;
    virtual void SetSuspensionPending(bool fSuspensionPending) override;
    virtual void SetYieldProcessorScalingFactor(float yieldProcessorScalingFactor) override;
    virtual void Shutdown() override;
    virtual size_t GetLastGCStartTime(int generation) override;
    virtual size_t GetLastGCDuration(int generation) override;
    virtual size_t GetNow() override;
    virtual Object* Alloc(gc_alloc_context* acontext, size_t size, uint32_t flags) override;
    virtual void PublishObject(uint8_t* obj) override;
    virtual void SetWaitForGCEvent() override;
    virtual void ResetWaitForGCEvent() override;
    virtual bool IsLargeObject(Object* pObj) override;
    virtual void ValidateObjectMember(Object* obj) override;
    virtual Object* NextObj(Object* object) override;
    virtual Object* GetContainingObject(void* pInteriorPtr, bool fCollectedGenOnly) override;
    virtual void DiagWalkObject(Object* obj, walk_fn fn, void* context) override;
    virtual void DiagWalkObject2(Object* obj, walk_fn2 fn, void* context) override;
    virtual void DiagWalkHeap(walk_fn fn, void* context, int gen_number, bool walk_large_object_heap_p) override;
    virtual void DiagWalkSurvivorsWithType(void* gc_context, record_surv_fn fn, void* diag_context, walk_surv_type type, int gen_number = -1) override;
    virtual void DiagWalkFinalizeQueue(void* gc_context, fq_walk_fn fn) override;
    virtual void DiagScanFinalizeQueue(fq_scan_fn fn, ScanContext* context) override;
    virtual void DiagScanHandles(handle_scan_fn fn, int gen_number, ScanContext* context) override;
    virtual void DiagScanDependentHandles(handle_scan_fn fn, int gen_number, ScanContext* context) override;
    virtual void DiagDescrGenerations(gen_walk_fn fn, void* context) override;
    virtual void DiagTraceGCSegments() override;
    virtual bool StressHeap(gc_alloc_context* acontext) override;
    virtual segment_handle RegisterFrozenSegment(segment_info* pseginfo) override;
    virtual void UnregisterFrozenSegment(segment_handle seg) override;
    virtual bool IsInFrozenSegment(Object* object) override;
    virtual void ControlEvents(GCEventKeyword keyword, GCEventLevel level) override;
    virtual void ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level) override;

    // Inherited via IGCHeapInternal
    virtual int GetNumberOfHeaps() override;
    virtual int GetHomeHeapNumber() override;
    virtual size_t GetPromotedBytes(int heap_index) override;

    // Inherited via IGCHeapInternal
    virtual void GetMemoryInfo(uint64_t* highMemLoadThresholdBytes, uint64_t* totalAvailableMemoryBytes, uint64_t* lastRecordedMemLoadBytes, uint64_t* lastRecordedHeapSizeBytes, uint64_t* lastRecordedFragmentationBytes, uint64_t* totalCommittedBytes, uint64_t* promotedBytes, uint64_t* pinnedObjectCount, uint64_t* finalizationPendingCount, uint64_t* index, uint32_t* generation, uint32_t* pauseTimePct, bool* isCompaction, bool* isConcurrent, uint64_t* genInfoRaw, uint64_t* pauseInfoRaw, int kind) override;
    virtual uint32_t GetMemoryLoad() override;

    virtual void DiagGetGCSettings(EtwGCSettingsInfo* etw_settings) override;

    // Inherited via IGCHeapInternal
    virtual unsigned int GetGenerationWithRange(Object* object, uint8_t** ppStart, uint8_t** ppAllocated, uint8_t** ppReserved) override;

    // Inherited via IGCHeapInternal
    virtual int64_t GetTotalPauseDuration() override;
    virtual void EnumerateConfigurationValues(void* context, ConfigurationValueFunc configurationValueFunc) override;

    // Inherited via IGCHeapInternal
    virtual void UpdateFrozenSegment(segment_handle seg, uint8_t* allocated, uint8_t* committed) override;

    // Inherited via IGCHeapInternal
    void BulkMoveWithWriteBarrier(void* dst, const void* src, size_t byteCount);

    // Inherited via IGCHeapInternal
    virtual int RefreshMemoryLimit() override;
    virtual enable_no_gc_region_callback_status EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, uint64_t callback_threshold) override;
    virtual FinalizerWorkItem* GetExtraWorkForFinalization() override;

    // Inherited via IGCHeapInternal
    uint64_t GetGenerationBudget(int generation) override;

    // Inherited via IGCHeapInternal
    virtual size_t GetLOHThreshold() override;
    virtual void DiagWalkHeapWithACHandling(walk_fn fn, void *context, int gen_number, bool walk_large_object_heap_p) override;
    virtual void NullBridgeObjectsWeakRefs(size_t length, void* unreachableObjectHandles) override; 

    virtual void GetFastGCFunctions(FastGCFunctions* functions) override;

    virtual GCHeapType GetGCHeapType() override;

    // Gets the precise total number of allocated bytes.
    virtual uint64_t GetTotalAllocatedBytesPrecise() override;

    // Gets custom assign-reference helpers and their access violation locations.
    virtual void GetAssignRefFunctions(void** assignRef, size_t* assignRefSize, void** assignRefChecked, size_t* assignRefCheckedSize, uintptr_t** avLocationsList, GCWriteBarrierPatchableValue** patchableValues) override;
};

#endif
