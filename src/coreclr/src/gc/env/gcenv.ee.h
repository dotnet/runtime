//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
// Interface between the GC and EE
//

#ifndef __GCENV_EE_H__
#define __GCENV_EE_H__

struct ScanContext;
class CrawlFrame;

typedef void promote_func(PTR_PTR_Object, ScanContext*, uint32_t);

typedef void enum_alloc_context_func(alloc_context*, void*);

typedef struct
{
    promote_func*  f;
    ScanContext*   sc;
    CrawlFrame *   cf;
} GCCONTEXT;


class GCToEEInterface
{
public:
    //
    // Suspend/Resume callbacks
    //
    typedef enum
    {
        SUSPEND_FOR_GC = 1,
        SUSPEND_FOR_GC_PREP = 6
    } SUSPEND_REASON;

    static void SuspendEE(SUSPEND_REASON reason);
    static void RestartEE(bool bFinishedGC); //resume threads.

    // 
    // The GC roots enumeration callback
    //
    static void GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc);

    // 
    // Callbacks issues during GC that the execution engine can do its own bookeeping
    //

    // start of GC call back - single threaded
    static void GcStartWork(int condemned, int max_gen); 

    //EE can perform post stack scanning action, while the 
    // user threads are still suspended 
    static void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc);

    // Called before BGC starts sweeping, the heap is walkable
    static void GcBeforeBGCSweepWork();

    // post-gc callback.
    static void GcDone(int condemned);

    // Promote refcounted handle callback
    static bool RefCountedHandleCallbacks(Object * pObject);

    // Sync block cache management
    static void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2);
    static void SyncBlockCacheDemote(int max_gen);
    static void SyncBlockCachePromotionsGranted(int max_gen);

    // Thread functions
    static bool IsPreemptiveGCDisabled(Thread * pThread);
    static void EnablePreemptiveGC(Thread * pThread);
    static void DisablePreemptiveGC(Thread * pThread);

    static void SetGCSpecial(Thread * pThread);
    static alloc_context * GetAllocContext(Thread * pThread);
    static bool CatchAtSafePoint(Thread * pThread);

    static void GcEnumAllocContexts(enum_alloc_context_func* fn, void* param);

    static void AttachCurrentThread(); // does not acquire thread store lock
};

#endif // __GCENV_EE_H__
