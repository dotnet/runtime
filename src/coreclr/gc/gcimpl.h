// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef GCIMPL_H_
#define GCIMPL_H_

#ifdef SERVER_GC
#define MULTIPLE_HEAPS 1
#endif  // SERVER_GC

#ifdef MULTIPLE_HEAPS

#define PER_HEAP

#else //MULTIPLE_HEAPS

#define PER_HEAP static

#endif // MULTIPLE_HEAPS

#define PER_HEAP_ISOLATED static

#if defined(WRITE_BARRIER_CHECK) && !defined (MULTIPLE_HEAPS)
void initGCShadow();
void deleteGCShadow();
void checkGCWriteBarrier();
#else
inline void initGCShadow() {}
inline void deleteGCShadow() {}
inline void checkGCWriteBarrier() {}
#endif

void GCProfileWalkHeap(bool etwOnly);

class gc_heap;
class CFinalize;

extern bool g_fFinalizerRunOnShutDown;
extern bool g_built_with_svr_gc;
extern uint8_t g_build_variant;
extern VOLATILE(int32_t) g_no_gc_lock;

class GCHeap : public IGCHeapInternal
{
protected:

#ifdef MULTIPLE_HEAPS
    gc_heap*    pGenGCHeap;
#else
    #define pGenGCHeap ((gc_heap*)0)
#endif //MULTIPLE_HEAPS

    friend class CFinalize;
    friend class gc_heap;
    friend struct ::alloc_context;
    friend void EnterAllocLock();
    friend void LeaveAllocLock();
    friend void ProfScanRootsHelper(Object** object, ScanContext *pSC, uint32_t dwFlags);
    friend void GCProfileWalkHeap(bool etwOnly);

public:
    //In order to keep gc.cpp cleaner, ugly EE specific code is relegated to methods.
    static void UpdatePreGCCounters();
    static void UpdatePostGCCounters();

public:
    GCHeap(){};
    ~GCHeap(){};

    /* BaseGCHeap Methods*/
    PER_HEAP_ISOLATED   HRESULT StaticShutdown ();

    size_t  GetTotalBytesInUse ();
    // Gets the amount of bytes objects currently occupy on the GC heap.
    size_t  GetCurrentObjSize();

    uint64_t GetTotalAllocatedBytes();

    size_t  GetLastGCStartTime(int generation);
    size_t  GetLastGCDuration(int generation);
    size_t  GetNow();

    void  DiagTraceGCSegments ();
    void PublishObject(uint8_t* obj);

    bool IsGCInProgressHelper (bool bConsiderGCStart = false);

    uint32_t    WaitUntilGCComplete (bool bConsiderGCStart = false);

    void     SetGCInProgress(bool fInProgress);

    bool RuntimeStructuresValid();

    void SetSuspensionPending(bool fSuspensionPending);

    void SetYieldProcessorScalingFactor(float yieldProcessorScalingFactor);

    void SetWaitForGCEvent();
    void ResetWaitForGCEvent();

    HRESULT Initialize ();

    Object* Alloc (gc_alloc_context* acontext, size_t size, uint32_t flags);

    void FixAllocContext (gc_alloc_context* acontext, void* arg, void *heap);

    Object* GetContainingObject(void *pInteriorPtr, bool fCollectedGenOnly);

#ifdef MULTIPLE_HEAPS
    static void AssignHeap (alloc_context* acontext);
    static GCHeap* GetHeap (int);
#endif //MULTIPLE_HEAPS

    int GetHomeHeapNumber ();
    bool IsThreadUsingAllocationContextHeap(gc_alloc_context* acontext, int thread_number);
    int GetNumberOfHeaps ();
    void HideAllocContext(alloc_context*);
    void RevealAllocContext(alloc_context*);

    bool IsLargeObject(Object *pObj);

    HRESULT GarbageCollect (int generation = -1, bool low_memory_p=false, int mode=collection_blocking);

    ////
    // GC callback functions
    // Check if an argument is promoted (ONLY CALL DURING
    // THE PROMOTIONSGRANTED CALLBACK.)
    bool IsPromoted (Object *object);

    size_t GetPromotedBytes (int heap_index);

    int CollectionCount (int generation, int get_bgc_fgc_count = 0);

    // promote an object
    PER_HEAP_ISOLATED void    Promote (Object** object,
                                          ScanContext* sc,
                                          uint32_t flags=0);

    // Find the relocation address for an object
    PER_HEAP_ISOLATED void    Relocate (Object** object,
                                           ScanContext* sc,
                                           uint32_t flags=0);


    HRESULT Init (size_t heapSize);

    //Register an object for finalization
    bool    RegisterForFinalization (int gen, Object* obj);

    //Unregister an object for finalization
    void    SetFinalizationRun (Object* obj);

    //returns the generation number of an object (not valid during relocation)
    unsigned WhichGeneration (Object* object);
    // returns TRUE is the object is ephemeral
    bool IsEphemeral (Object* object);
    bool IsHeapPointer (void* object, bool small_heap_only = false);

    void    ValidateObjectMember (Object *obj);

    PER_HEAP    size_t  ApproxTotalBytesInUse(BOOL small_heap_only = FALSE);
    PER_HEAP    size_t  ApproxFreeBytes();

    unsigned GetCondemnedGeneration();

    void GetMemoryInfo(uint64_t* highMemLoadThresholdBytes,
                       uint64_t* totalAvailableMemoryBytes,
                       uint64_t* lastRecordedMemLoadBytes,
                       uint64_t* lastRecordedHeapSizeBytes,
                       uint64_t* lastRecordedFragmentationBytes,
                       uint64_t* totalCommittedBytes,
                       uint64_t* promotedBytes,
                       uint64_t* pinnedObjectCount,
                       uint64_t* finalizationPendingCount,
                       uint64_t* index,
                       uint32_t* generation,
                       uint32_t* pauseTimePct,
                       bool* isCompaction,
                       bool* isConcurrent,
                       uint64_t* genInfoRaw,
                       uint64_t* pauseInfoRaw,
                       int kind);;

    uint32_t GetMemoryLoad();

    int GetGcLatencyMode();
    int SetGcLatencyMode(int newLatencyMode);

    int GetLOHCompactionMode();
    void SetLOHCompactionMode(int newLOHCompactionyMode);

    bool RegisterForFullGCNotification(uint32_t gen2Percentage,
                                       uint32_t lohPercentage);
    bool CancelFullGCNotification();
    int WaitForFullGCApproach(int millisecondsTimeout);
    int WaitForFullGCComplete(int millisecondsTimeout);

    int StartNoGCRegion(uint64_t totalSize, bool lohSizeKnown, uint64_t lohSize, bool disallowFullBlockingGC);
    int EndNoGCRegion();

    unsigned GetGcCount();

    Object* GetNextFinalizable() { return GetNextFinalizableObject(); };
    size_t GetNumberOfFinalizable() { return GetNumberFinalizableObjects(); }

    PER_HEAP_ISOLATED HRESULT GetGcCounters(int gen, gc_counters* counters);

    size_t GetValidSegmentSize(bool large_seg = false);

    void SetReservedVMLimit (size_t vmlimit);

    PER_HEAP_ISOLATED Object* GetNextFinalizableObject();
    PER_HEAP_ISOLATED size_t GetNumberFinalizableObjects();
    PER_HEAP_ISOLATED size_t GetFinalizablePromotedCount();

    void DiagWalkObject (Object* obj, walk_fn fn, void* context);
    void DiagWalkObject2 (Object* obj, walk_fn2 fn, void* context);

public:	// FIX

    // Lock for finalization
    PER_HEAP_ISOLATED
        VOLATILE(int32_t)          m_GCFLock;

    PER_HEAP_ISOLATED   BOOL    GcCollectClasses;
    PER_HEAP_ISOLATED
        VOLATILE(BOOL)          GcInProgress;       // used for syncing w/GC
    PER_HEAP_ISOLATED   VOLATILE(unsigned) GcCount;
    PER_HEAP_ISOLATED   unsigned GcCondemnedGeneration;
    // calculated at the end of a GC.
    PER_HEAP_ISOLATED   size_t  totalSurvivedSize;

    // Use only for GC tracing.
    PER_HEAP    uint64_t GcDuration;

    size_t  GarbageCollectGeneration (unsigned int gen=0, gc_reason reason=reason_empty);
    // Interface with gc_heap
    size_t  GarbageCollectTry (int generation, BOOL low_memory_p=FALSE, int mode=collection_blocking);

    // frozen segment management functions
    virtual segment_handle RegisterFrozenSegment(segment_info *pseginfo);
    virtual void UnregisterFrozenSegment(segment_handle seg);
    virtual bool IsInFrozenSegment(Object *object);

    // Event control functions
    void ControlEvents(GCEventKeyword keyword, GCEventLevel level);
    void ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level);

    void    WaitUntilConcurrentGCComplete ();                               // Use in managd threads
#ifndef DACCESS_COMPILE
    HRESULT WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout);    // Use in native threads. TRUE if succeed. FALSE if failed or timeout
#endif
    bool IsConcurrentGCInProgress();

    // Enable/disable concurrent GC
    void TemporaryEnableConcurrentGC();
    void TemporaryDisableConcurrentGC();
    bool IsConcurrentGCEnabled();

    PER_HEAP_ISOLATED   GCEvent *WaitForGCEvent;     // used for syncing w/GC

    PER_HEAP_ISOLATED    CFinalize* m_Finalize;

    PER_HEAP_ISOLATED   gc_heap* Getgc_heap();

private:
    static bool SafeToRestartManagedThreads()
    {
        // Note: this routine should return true when the last barrier
        // to threads returning to cooperative mode is down after gc.
        // In other words, if the sequence in GCHeap::RestartEE changes,
        // the condition here may have to change as well.
        return g_fSuspensionPending == 0;
    }
public:
    //return TRUE if GC actually happens, otherwise FALSE
    bool StressHeap(gc_alloc_context * acontext);

#ifndef FEATURE_REDHAWK // Redhawk forces relocation a different way
#ifdef STRESS_HEAP
protected:

    // only used in BACKGROUND_GC, but the symbol is not defined yet...
    PER_HEAP_ISOLATED int gc_stress_fgcs_in_bgc;

#if !defined(MULTIPLE_HEAPS)
    // handles to hold the string objects that will force GC movement
    enum { NUM_HEAP_STRESS_OBJS = 8 };
    PER_HEAP OBJECTHANDLE m_StressObjs[NUM_HEAP_STRESS_OBJS];
    PER_HEAP int m_CurStressObj;
#endif  // !defined(MULTIPLE_HEAPS)
#endif  // STRESS_HEAP
#endif // FEATURE_REDHAWK

    virtual void DiagDescrGenerations (gen_walk_fn fn, void *context);

    virtual void DiagWalkSurvivorsWithType (void* gc_context, record_surv_fn fn, void* diag_context, walk_surv_type type, int gen_number=-1);

    virtual void DiagWalkFinalizeQueue (void* gc_context, fq_walk_fn fn);

    virtual void DiagScanFinalizeQueue (fq_scan_fn fn, ScanContext* context);

    virtual void DiagScanHandles (handle_scan_fn fn, int gen_number, ScanContext* context);

    virtual void DiagScanDependentHandles (handle_scan_fn fn, int gen_number, ScanContext* context);

    virtual void DiagWalkHeap(walk_fn fn, void* context, int gen_number, bool walk_large_object_heap_p);

    virtual void DiagGetGCSettings(EtwGCSettingsInfo* etw_settings);

    virtual unsigned int GetGenerationWithRange(Object* object, uint8_t** ppStart, uint8_t** ppAllocated, uint8_t** ppReserved);
public:
    Object * NextObj (Object * object);

    int GetLastGCPercentTimeInGC();

    size_t GetLastGCGenerationSize(int gen);

    virtual void Shutdown();

    static void ReportGenerationBounds();
};

#endif  // GCIMPL_H_
