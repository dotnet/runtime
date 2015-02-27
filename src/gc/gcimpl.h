//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef GCIMPL_H_
#define GCIMPL_H_

#define CLREvent CLREventStatic

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

void GCProfileWalkHeap();

class GCHeap;
class gc_heap;
class CFinalize;

// TODO : it would be easier to make this an ORed value
enum gc_reason
{
    reason_alloc_soh = 0,
    reason_induced = 1,
    reason_lowmemory = 2,
    reason_empty = 3,
    reason_alloc_loh = 4,
    reason_oos_soh = 5,
    reason_oos_loh = 6,
    reason_induced_noforce = 7, // it's an induced GC and doesn't have to be blocking.
    reason_gcstress = 8,        // this turns into reason_induced & gc_mechanisms.stress_induced = true
    reason_lowmemory_blocking = 9,
    reason_induced_compacting = 10,
    reason_max
};

class GCHeap : public ::GCHeap
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
    friend void ProfScanRootsHelper(Object** object, ScanContext *pSC, DWORD dwFlags);
    friend void GCProfileWalkHeap();

public:
    //In order to keep gc.cpp cleaner, ugly EE specific code is relegated to methods. 
    static void UpdatePreGCCounters();
    static void UpdatePostGCCounters();

public:
    GCHeap(){};
    ~GCHeap(){};

    /* BaseGCHeap Methods*/
    PER_HEAP_ISOLATED   HRESULT Shutdown ();

    size_t  GetTotalBytesInUse ();
    // Gets the amount of bytes objects currently occupy on the GC heap.
    size_t  GetCurrentObjSize();

    size_t  GetLastGCStartTime(int generation);
    size_t  GetLastGCDuration(int generation);
    size_t  GetNow();

    void  TraceGCSegments ();    
    void PublishObject(BYTE* obj);
    
    BOOL    IsGCInProgressHelper (BOOL bConsiderGCStart = FALSE);

    DWORD    WaitUntilGCComplete (BOOL bConsiderGCStart = FALSE);

    void     SetGCInProgress(BOOL fInProgress);

    CLREvent * GetWaitForGCEvent();

    HRESULT Initialize ();

    //flags can be GC_ALLOC_CONTAINS_REF GC_ALLOC_FINALIZE
    Object*  Alloc (size_t size, DWORD flags);
#ifdef FEATURE_64BIT_ALIGNMENT
    Object*  AllocAlign8 (size_t size, DWORD flags);
    Object*  AllocAlign8 (alloc_context* acontext, size_t size, DWORD flags);
private:
    Object*  AllocAlign8Common (void* hp, alloc_context* acontext, size_t size, DWORD flags);
public:
#endif // FEATURE_64BIT_ALIGNMENT
    Object*  AllocLHeap (size_t size, DWORD flags);
    Object* Alloc (alloc_context* acontext, size_t size, DWORD flags);

    void FixAllocContext (alloc_context* acontext,
                                            BOOL lockp, void* arg, void *heap);

    Object* GetContainingObject(void *pInteriorPtr);

#ifdef MULTIPLE_HEAPS
    static void AssignHeap (alloc_context* acontext);
    static GCHeap* GetHeap (int);
#endif //MULTIPLE_HEAPS

    int GetHomeHeapNumber ();
    bool IsThreadUsingAllocationContextHeap(alloc_context* acontext, int thread_number);
    int GetNumberOfHeaps ();
	void HideAllocContext(alloc_context*);
	void RevealAllocContext(alloc_context*);
   
    static BOOL IsLargeObject(MethodTable *mt);

    BOOL IsObjectInFixedHeap(Object *pObj);

    HRESULT GarbageCollect (int generation = -1, BOOL low_memory_p=FALSE, int mode=collection_blocking);

    ////
    // GC callback functions
    // Check if an argument is promoted (ONLY CALL DURING
    // THE PROMOTIONSGRANTED CALLBACK.)
    BOOL    IsPromoted (Object *object);

    size_t GetPromotedBytes (int heap_index);
    
    int CollectionCount (int generation, int get_bgc_fgc_count = 0);

    // promote an object
    PER_HEAP_ISOLATED void    Promote (Object** object, 
                                          ScanContext* sc,
                                          DWORD flags=0);

    // Find the relocation address for an object
	PER_HEAP_ISOLATED void    Relocate (Object** object,
                                           ScanContext* sc, 
                                           DWORD flags=0);


    HRESULT Init (size_t heapSize);

    //Register an object for finalization
    bool    RegisterForFinalization (int gen, Object* obj); 
    
    //Unregister an object for finalization
    void    SetFinalizationRun (Object* obj); 
    
    //returns the generation number of an object (not valid during relocation)
    unsigned WhichGeneration (Object* object);
    // returns TRUE is the object is ephemeral 
    BOOL    IsEphemeral (Object* object);
    BOOL    IsHeapPointer (void* object, BOOL small_heap_only = FALSE);
    
#ifdef VERIFY_HEAP
	void    ValidateObjectMember (Object *obj);
#endif //_DEBUG

    PER_HEAP    size_t  ApproxTotalBytesInUse(BOOL small_heap_only = FALSE);
    PER_HEAP    size_t  ApproxFreeBytes();

    unsigned GetCondemnedGeneration();

    int GetGcLatencyMode();
    int SetGcLatencyMode(int newLatencyMode);

    int GetLOHCompactionMode();
    void SetLOHCompactionMode(int newLOHCompactionyMode);

    BOOL RegisterForFullGCNotification(DWORD gen2Percentage,
                                       DWORD lohPercentage);
    BOOL CancelFullGCNotification();
    int WaitForFullGCApproach(int millisecondsTimeout);
    int WaitForFullGCComplete(int millisecondsTimeout);

    int StartNoGCRegion(ULONGLONG totalSize, BOOL lohSizeKnown, ULONGLONG lohSize, BOOL disallowFullBlockingGC);
    int EndNoGCRegion();

    PER_HEAP_ISOLATED     unsigned GetMaxGeneration();
 
    unsigned GetGcCount();

    Object* GetNextFinalizable() { return GetNextFinalizableObject(); };
    size_t GetNumberOfFinalizable() { return GetNumberFinalizableObjects(); }

    PER_HEAP_ISOLATED HRESULT GetGcCounters(int gen, gc_counters* counters);

    size_t GetValidSegmentSize(BOOL large_seg = FALSE);

    static size_t GetValidGen0MaxSize(size_t seg_size);

    void SetReservedVMLimit (size_t vmlimit);

    PER_HEAP_ISOLATED Object* GetNextFinalizableObject();
    PER_HEAP_ISOLATED size_t GetNumberFinalizableObjects();
    PER_HEAP_ISOLATED size_t GetFinalizablePromotedCount();

    void SetFinalizeQueueForShutdown(BOOL fHasLock);
    BOOL FinalizeAppDomain(AppDomain *pDomain, BOOL fRunFinalizers);
    BOOL ShouldRestartFinalizerWatchDog();

	void SetCardsAfterBulkCopy( Object**, size_t);
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    void WalkObject (Object* obj, walk_fn fn, void* context);
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

public:	// FIX 

    // Lock for finalization
    PER_HEAP_ISOLATED   
        VOLATILE(LONG)          m_GCFLock;

    PER_HEAP_ISOLATED   BOOL    GcCollectClasses;
    PER_HEAP_ISOLATED
        VOLATILE(BOOL)          GcInProgress;       // used for syncing w/GC
    PER_HEAP_ISOLATED   VOLATILE(unsigned) GcCount;
    PER_HEAP_ISOLATED   unsigned GcCondemnedGeneration;
    // calculated at the end of a GC.
    PER_HEAP_ISOLATED   size_t  totalSurvivedSize;

    // Use only for GC tracing.
    PER_HEAP    unsigned int GcDuration;

    size_t  GarbageCollectGeneration (unsigned int gen=0, gc_reason reason=reason_empty);
    // Interface with gc_heap
    size_t  GarbageCollectTry (int generation, BOOL low_memory_p=FALSE, int mode=collection_blocking);

#ifdef FEATURE_BASICFREEZE
    // frozen segment management functions
    virtual segment_handle RegisterFrozenSegment(segment_info *pseginfo);
#endif // FEATURE_BASICFREEZE

    void    WaitUntilConcurrentGCComplete ();                               // Use in managd threads
#ifndef DACCESS_COMPILE    
    HRESULT WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout);    // Use in native threads. TRUE if succeed. FALSE if failed or timeout
#endif    
    BOOL    IsConcurrentGCInProgress();

    // Enable/disable concurrent GC    
    void TemporaryEnableConcurrentGC();
    void TemporaryDisableConcurrentGC();
    BOOL IsConcurrentGCEnabled();    
  
    PER_HEAP_ISOLATED   CLREvent *WaitForGCEvent;     // used for syncing w/GC

    PER_HEAP_ISOLATED    CFinalize* m_Finalize;

    PER_HEAP_ISOLATED   gc_heap* Getgc_heap();

private:
    static bool SafeToRestartManagedThreads()
    {
        // Note: this routine should return true when the last barrier
        // to threads returning to cooperative mode is down after gc.
        // In other words, if the sequence in GCHeap::RestartEE changes,
        // the condition here may have to change as well.
        return g_TrapReturningThreads == 0;
    }
#ifndef FEATURE_REDHAWK // Redhawk forces relocation a different way
#ifdef STRESS_HEAP 
public:
    //return TRUE if GC actually happens, otherwise FALSE
    BOOL    StressHeap(alloc_context * acontext = 0);
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

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    virtual void DescrGenerationsToProfiler (gen_walk_fn fn, void *context);
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

#ifdef VERIFY_HEAP
public:
    Object * NextObj (Object * object);
#ifdef FEATURE_BASICFREEZE
    BOOL IsInFrozenSegment (Object * object);
#endif //FEATURE_BASICFREEZE
#endif //VERIFY_HEAP     
};

#endif  // GCIMPL_H_
