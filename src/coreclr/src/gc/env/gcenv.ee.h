// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Interface between the GC and EE
//

#ifndef __GCENV_EE_H__
#define __GCENV_EE_H__

#include "gcinterface.h"

class GCToEEInterface
{
public:
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

    static gc_alloc_context * GetAllocContext(Thread * pThread);
    static bool CatchAtSafePoint(Thread * pThread);

    static void GcEnumAllocContexts(enum_alloc_context_func* fn, void* param);

    static Thread* CreateBackgroundThread(GCBackgroundThreadFunction threadStart, void* arg);

    // Diagnostics methods.
    static void DiagGCStart(int gen, bool isInduced);
    static void DiagUpdateGenerationBounds();
    static void DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent);
    static void DiagWalkFReachableObjects(void* gcContext);
    static void DiagWalkSurvivors(void* gcContext);
    static void DiagWalkLOHSurvivors(void* gcContext);
    static void DiagWalkBGCSurvivors(void* gcContext);
    static void StompWriteBarrier(WriteBarrierParameters* args);

    static void EnableFinalization(bool foundFinalizers);
};

#endif // __GCENV_EE_H__
