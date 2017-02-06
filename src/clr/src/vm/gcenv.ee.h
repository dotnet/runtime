// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCENV_EE_H_
#define _GCENV_EE_H_

#include "gcinterface.h"

#ifdef FEATURE_STANDALONE_GC

class GCToEEInterface : public IGCToCLR {
public:
    GCToEEInterface() = default;
    ~GCToEEInterface() = default;

    void SuspendEE(SUSPEND_REASON reason);
    void RestartEE(bool bFinishedGC);
    void GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc);
    void GcStartWork(int condemned, int max_gen);
    void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc);
    void GcBeforeBGCSweepWork();
    void GcDone(int condemned);
    bool RefCountedHandleCallbacks(Object * pObject);
    void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2);
    void SyncBlockCacheDemote(int max_gen);
    void SyncBlockCachePromotionsGranted(int max_gen);
    bool IsPreemptiveGCDisabled(Thread * pThread);
    void EnablePreemptiveGC(Thread * pThread);
    void DisablePreemptiveGC(Thread * pThread);
    gc_alloc_context * GetAllocContext(Thread * pThread);
    bool CatchAtSafePoint(Thread * pThread);
    void GcEnumAllocContexts(enum_alloc_context_func* fn, void* param);
    Thread* CreateBackgroundThread(GCBackgroundThreadFunction threadStart, void* arg);

    // Diagnostics methods.
    void DiagGCStart(int gen, bool isInduced);
    void DiagUpdateGenerationBounds();
    void DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent);
    void DiagWalkFReachableObjects(void* gcContext);
    void DiagWalkSurvivors(void* gcContext);
    void DiagWalkLOHSurvivors(void* gcContext);
    void DiagWalkBGCSurvivors(void* gcContext);
    void StompWriteBarrier(WriteBarrierParameters* args);

    void EnableFinalization(bool foundFinalizers);
};

#endif // FEATURE_STANDALONE_GC

#endif // _GCENV_EE_H_