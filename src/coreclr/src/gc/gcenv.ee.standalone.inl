// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCTOENV_EE_STANDALONE_INL__
#define __GCTOENV_EE_STANDALONE_INL__

#include "env/gcenv.ee.h"

// The singular interface instance. All calls in GCToEEInterface
// will be fowarded to this interface instance.
extern IGCToCLR* g_theGCToCLR;

// A note about this:
// In general, we don't want to pretend to be smarter than the compiler
// and force it to inline things. However, inlining is here is required
// for correctness as it stands today (though it will not always be required).
//
// The reason for this is because:
//   1) This file (and the GCToEEInterface class) define symbols that are inline
//      and static, so the symbol GCToEEInterface::XYZ defines a symbol with weak
//      linkage when the function is not inlined,
//   2) src/vm/gcenv.ee.cpp all define symbols that are not inline and instance methods
//      of GCToEEInterface, with external linkage.
//   3) When it comes time to link the GC and the VM, the linker observes the duplicate
//      symbols and discards the one with weak linkage.
//   4) All of the calls within the GC to the functions in this file are replaced by
//      the linker to calls to the implementation of a pure virtual IGCToCLR. The
//      functions implementing IGCToCLR have an extra argument (this).
//   5) Now, all calls to these functions from within the GC are doomed because of the
//      functions that actually get called expect this to be in rdi, where the compiler
//      has placed the first argument instead.
//
// For now, by forcing the compiler to inline these functions, the compiler won't actually
// emit symbols for them and we'll avoid the linker havoc.
#ifdef _MSC_VER
 #define ALWAYS_INLINE __forceinline
#else
 #define ALWAYS_INLINE __attribute__((always_inline)) inline
#endif

// When we are building the GC in a standalone environment, we
// will be dispatching virtually against g_theGCToCLR to call
// into the EE. This class provides an identical API to the existing
// GCToEEInterface, but only forwards the call onto the global
// g_theGCToCLR instance.
ALWAYS_INLINE void GCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SuspendEE(reason);
}

ALWAYS_INLINE void GCToEEInterface::RestartEE(bool bFinishedGC)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->RestartEE(bFinishedGC);
}

ALWAYS_INLINE void GCToEEInterface::GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcScanRoots(fn, condemned, max_gen, sc);
}

ALWAYS_INLINE void GCToEEInterface::GcStartWork(int condemned, int max_gen)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcStartWork(condemned, max_gen);
}

ALWAYS_INLINE void GCToEEInterface::AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->AfterGcScanRoots(condemned, max_gen, sc);
}

ALWAYS_INLINE void GCToEEInterface::GcBeforeBGCSweepWork()
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcBeforeBGCSweepWork();
}

ALWAYS_INLINE void GCToEEInterface::GcDone(int condemned)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcDone(condemned);
}

ALWAYS_INLINE bool GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->RefCountedHandleCallbacks(pObject);
}

ALWAYS_INLINE void GCToEEInterface::SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SyncBlockCacheWeakPtrScan(scanProc, lp1, lp2);
}

ALWAYS_INLINE void GCToEEInterface::SyncBlockCacheDemote(int max_gen)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SyncBlockCacheDemote(max_gen);
}

ALWAYS_INLINE void GCToEEInterface::SyncBlockCachePromotionsGranted(int max_gen)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->SyncBlockCachePromotionsGranted(max_gen);
}

ALWAYS_INLINE bool GCToEEInterface::IsPreemptiveGCDisabled(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->IsPreemptiveGCDisabled(pThread);
}


ALWAYS_INLINE void GCToEEInterface::EnablePreemptiveGC(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->EnablePreemptiveGC(pThread);
}

ALWAYS_INLINE void GCToEEInterface::DisablePreemptiveGC(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DisablePreemptiveGC(pThread);
}

ALWAYS_INLINE gc_alloc_context * GCToEEInterface::GetAllocContext(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->GetAllocContext(pThread);
}

ALWAYS_INLINE bool GCToEEInterface::CatchAtSafePoint(Thread * pThread)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->CatchAtSafePoint(pThread);
}

ALWAYS_INLINE void GCToEEInterface::GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->GcEnumAllocContexts(fn, param);
}

ALWAYS_INLINE Thread* GCToEEInterface::CreateBackgroundThread(GCBackgroundThreadFunction threadStart, void* arg)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->CreateBackgroundThread(threadStart, arg);
}

ALWAYS_INLINE void GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagGCStart(gen, isInduced);
}

ALWAYS_INLINE void GCToEEInterface::DiagUpdateGenerationBounds()
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagUpdateGenerationBounds();
}

ALWAYS_INLINE void GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagGCEnd(index, gen, reason, fConcurrent);
}

ALWAYS_INLINE void GCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagWalkFReachableObjects(gcContext);
}

ALWAYS_INLINE void GCToEEInterface::DiagWalkSurvivors(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagWalkSurvivors(gcContext);
}

ALWAYS_INLINE void GCToEEInterface::DiagWalkLOHSurvivors(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->DiagWalkLOHSurvivors(gcContext);
}

ALWAYS_INLINE void GCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
    assert(g_theGCToCLR != nullptr);
    return g_theGCToCLR->DiagWalkBGCSurvivors(gcContext);
}

ALWAYS_INLINE void GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->StompWriteBarrier(args);
}

ALWAYS_INLINE void GCToEEInterface::EnableFinalization(bool foundFinalizers)
{
    assert(g_theGCToCLR != nullptr);
    g_theGCToCLR->EnableFinalization(foundFinalizers);
}

#undef ALWAYS_INLINE

#endif // __GCTOENV_EE_STANDALONE_INL__
