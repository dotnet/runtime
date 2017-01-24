// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCTOENV_EE_STANDALONE_INL__
#define __GCTOENV_EE_STANDALONE_INL__

#include "env/gcenv.ee.h"

// The singular interface instance. All calls in GCToEEInterface
// will be fowarded to this interface instance.
extern IGCToCLR* g_theGCToCLR;

// When we are building the GC in a standalone environment, we
// will be dispatching virtually against g_theGCToCLR to call
// into the EE. This class provides an identical API to the existing
// GCToEEInterface, but only forwards the call onto the global
// g_theGCToCLR instance.
inline void GCToEEInterface::SuspendEE(SUSPEND_REASON reason) 
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SuspendEE(reason);
}

inline void GCToEEInterface::RestartEE(bool bFinishedGC)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->RestartEE(bFinishedGC);
}

inline void GCToEEInterface::GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcScanRoots(fn, condemned, max_gen, sc);
}

inline void GCToEEInterface::GcStartWork(int condemned, int max_gen)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcStartWork(condemned, max_gen);
}

inline void GCToEEInterface::AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->AfterGcScanRoots(condemned, max_gen, sc);
}

inline void GCToEEInterface::GcBeforeBGCSweepWork()
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcBeforeBGCSweepWork();
}

inline void GCToEEInterface::GcDone(int condemned)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcDone(condemned);
}

inline bool GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->RefCountedHandleCallbacks(pObject);
}

inline void GCToEEInterface::SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SyncBlockCacheWeakPtrScan(scanProc, lp1, lp2);
}

inline void GCToEEInterface::SyncBlockCacheDemote(int max_gen)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SyncBlockCacheDemote(max_gen);
}

inline void GCToEEInterface::SyncBlockCachePromotionsGranted(int max_gen)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SyncBlockCachePromotionsGranted(max_gen);
}

inline bool GCToEEInterface::IsPreemptiveGCDisabled(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->IsPreemptiveGCDisabled(pThread);
}


inline void GCToEEInterface::EnablePreemptiveGC(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->EnablePreemptiveGC(pThread);
}

inline void GCToEEInterface::DisablePreemptiveGC(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DisablePreemptiveGC(pThread);
}

inline gc_alloc_context * GCToEEInterface::GetAllocContext(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->GetAllocContext(pThread);
}

inline bool GCToEEInterface::CatchAtSafePoint(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->CatchAtSafePoint(pThread);
}

inline void GCToEEInterface::GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcEnumAllocContexts(fn, param);
}

inline Thread* GCToEEInterface::CreateBackgroundThread(GCBackgroundThreadFunction threadStart, void* arg)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->CreateBackgroundThread(threadStart, arg);
}

inline void GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagGCStart(gen, isInduced);
}

inline void GCToEEInterface::DiagUpdateGenerationBounds()
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagUpdateGenerationBounds();
}

inline void GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagGCEnd(index, gen, reason, fConcurrent);
}

inline void GCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagWalkFReachableObjects(gcContext);
}

inline void GCToEEInterface::DiagWalkSurvivors(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagWalkSurvivors(gcContext);
}

inline void GCToEEInterface::DiagWalkLOHSurvivors(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagWalkLOHSurvivors(gcContext);
}

inline void GCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->DiagWalkBGCSurvivors(gcContext);
}

inline void GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->StompWriteBarrier(args);
}

inline void GCToEEInterface::EnableFinalization(bool foundFinalizers)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->EnableFinalization(foundFinalizers);
}

#endif // __GCTOENV_EE_STANDALONE_INL__
