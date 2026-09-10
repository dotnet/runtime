// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * GCENV.EE.CPP
 *
 * GCToEEInterface implementation
 *
 */

#include "common.h"
#include "gcenv.h"
#include "../gc/env/gcenv.ee.h"
#include "threadsuspend.h"
#include "interoplibinterface.h"
#include "exinfo.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#endif // FEATURE_COMINTEROP

#include "gctoclreventsink.h"
#include "configuration.h"
#include "genanalysis.h"
#include "eventpipeadapter.h"
#ifdef TARGET_ARM
#include "cgensys.h"
#endif // TARGET_ARM
// Finalizes a weak reference directly.
extern void FinalizeWeakReference(Object* obj);

extern GCHeapHardLimitInfo g_gcHeapHardLimitInfo;
extern bool g_gcHeapHardLimitInfoSpecified;

#include <generatedumpflags.h>
#include "gcrefmap.h"

void GCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
    WRAPPER_NO_CONTRACT;

    static_assert(SUSPEND_FOR_GC == (int)ThreadSuspend::SUSPEND_FOR_GC);
    static_assert(SUSPEND_FOR_GC_PREP == (int)ThreadSuspend::SUSPEND_FOR_GC_PREP);

    _ASSERTE(reason == SUSPEND_FOR_GC || reason == SUSPEND_FOR_GC_PREP);

    if (g_pDebugInterface)
        g_pDebugInterface->SuspendForGarbageCollectionStarted();

    ThreadSuspend::SuspendEE((ThreadSuspend::SUSPEND_REASON)reason);

    if (g_pDebugInterface)
        g_pDebugInterface->SuspendForGarbageCollectionCompleted();
}

void GCToEEInterface::RestartEE(bool bUnused)
{
    WRAPPER_NO_CONTRACT;

    if (g_pDebugInterface)
        g_pDebugInterface->ResumeForGarbageCollectionStarted();

    ThreadSuspend::RestartEE(true /* SuspendSucceeded */);
}

VOID GCToEEInterface::SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    SyncBlockCache::GetSyncBlockCache()->GCWeakPtrScan(scanProc, lp1, lp2);
}

void GCToEEInterface::BeforeGcScanRoots(int condemned, bool is_bgc, bool is_concurrent)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef VERIFY_HEAP
    if (is_bgc)
    {
        // Validate byrefs pinned by IL stubs since the last GC.
        StubHelpers::ProcessByrefValidationList();
    }
#endif // VERIFY_HEAP

    Interop::OnBeforeGCScanRoots(is_concurrent);
}

//EE can perform post stack scanning action, while the
// user threads are still suspended
VOID GCToEEInterface::AfterGcScanRoots (int condemned, int max_gen,
                                   ScanContext* sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    // Go through all the only app domain and detach all the *unmarked* RCWs to prevent
    // the RCW cache from resurrecting them.
    ::GetAppDomain()->DetachRCWs();
#endif // FEATURE_COMINTEROP

    Interop::OnAfterGCScanRoots(sc->concurrent);
}

/*
 * Scan all stack roots
 */

static void ScanStackRoots(Thread * pThread, promote_func* fn, ScanContext* sc)
{
    GCCONTEXT   gcctx;

    gcctx.f  = fn;
    gcctx.sc = sc;
    gcctx.cf = NULL;

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // Either we are in a concurrent situation (in which case the thread is unknown to
    // us), or we are performing a synchronous GC and we are the GC thread, holding
    // the threadstore lock.

    _ASSERTE(dbgOnly_IsSpecialEEThread() ||
                GetThreadNULLOk() == NULL ||
                // this is for background GC threads which always call this when EE is suspended.
                IsGCSpecialThread() ||
                (GetThread() == ThreadSuspend::GetSuspensionThread() && ThreadStore::HoldingThreadStore()));

    Frame* pTopFrame = pThread->GetFrame();
    Object ** topStack = (Object **)pTopFrame;
    if (InlinedCallFrame::FrameHasActiveCall(pTopFrame))
    {
        // It is an InlinedCallFrame with active call. Get SP from it.
        InlinedCallFrame* pInlinedFrame = dac_cast<PTR_InlinedCallFrame>(pTopFrame);
        topStack = (Object **)pInlinedFrame->GetCallSiteSP();
    }

    // We set the stack_limit when FEF (FaultingExceptionFrame) is enabled.
    // See the comment on the stack_limit usage in the PromoteCarefully function for more details.
    sc->stack_limit = (uintptr_t)topStack;

#ifdef FEATURE_CONSERVATIVE_GC
    if (g_pConfig->GetGCConservative())
    {
        // Conservative stack root reporting
        // We will treat everything on stack as a pinned interior GC pointer
        // Since we report every thing as pinned, we don't need to run following code for relocation phase.
        if (sc->promotion)
        {
            Object ** bottomStack = (Object **) pThread->GetCachedStackBase();
            Object ** walk;
            const WriteBarrierFunctions& writeBarrierFunctions =
                GCHeapUtilities::GetWriteBarrierFunctions();
            IsInGCHeapFunction isInGCHeap = writeBarrierFunctions.is_in_gc_heap;
            void* gcContext = writeBarrierFunctions.context;
            for (walk = topStack; walk < bottomStack; walk ++)
            {
                if (((void*)*walk > (void*)bottomStack || (void*)*walk < (void*)topStack) &&
                    isInGCHeap(gcContext, *walk))
                {
                    //DbgPrintf("promote " FMT_ADDR " : " FMT_ADDR "\n", walk, *walk);
                    fn(walk, sc, GC_CALL_INTERIOR|GC_CALL_PINNED);
                }
            }
        }

        // Also ask the explicit Frames to report any references they might know about.
        // Generally these will be a subset of the objects reported below but there's
        // nothing that guarantees that and in the specific case of a GC protect frame the
        // references it protects may live at a lower address than the frame itself (and
        // thus escape the stack range we scanned above).
        Frame *pFrame = pThread->GetFrame();
        while (pFrame != FRAME_TOP)
        {
            pFrame->GcScanRoots(fn, sc);
            pFrame = pFrame->PtrNextFrame();
        }
    }
    else
#endif
    {
        unsigned flagsStackWalk = ALLOW_ASYNC_STACK_WALK | ALLOW_INVALID_OBJECTS;

        flagsStackWalk |= GC_FUNCLET_REFERENCE_REPORTING;

        gcctx.pScannedSlots = NULL;
        pThread->StackWalkFrames( GcStackCrawlCallBack, &gcctx, flagsStackWalk);
        delete gcctx.pScannedSlots;
    }

    GCFrame* pGCFrame = pThread->GetGCFrame();
    while (pGCFrame != NULL)
    {
        pGCFrame->GcScanRoots(fn, sc);
        pGCFrame = pGCFrame->PtrNextFrame();
    }

    // Scan the ExInfo chain for exception objects held by direct pointer.
    // Superseded ExInfo objects may live in logically dead parts of the stack
    // that the normal GC stackwalk skips (e.g., when one exception dispatch
    // supersedes a previous one). We keep them alive for post-mortem debugging
    // and SOS. This mirrors NativeAOT's GcScanRootsWorker (thread.cpp:569-573).
    PTR_ExInfo pExInfo = pThread->GetExceptionState()->GetCurrentExceptionTracker();
    while (pExInfo != NULL)
    {
        PTR_PTR_Object pRef = dac_cast<PTR_PTR_Object>(&pExInfo->m_exception);
        fn(pRef, sc, 0);
        pExInfo = pExInfo->GetPreviousExceptionTracker();
    }
}

static void ScanTailCallArgBufferRoots(Thread* pThread, promote_func* fn, ScanContext* sc)
{
    TailCallTls* tls = pThread->GetTailCallTls();
    // Keep loader associated with CallTailCallTarget alive.
    if (sc->promotion)
    {
#ifndef DACCESS_COMPILE
        const PortableTailCallFrame* frame = tls->GetFrame();
        if (frame->NextCall != NULL)
        {
            MethodDesc* pMD = NonVirtualEntry2MethodDesc((PCODE)frame->NextCall);
            if (pMD != NULL)
                GcReportLoaderAllocator(fn, sc, pMD->GetLoaderAllocator());
        }
#endif
    }

    TailCallArgBuffer* argBuffer = tls->GetArgBuffer();
    if (argBuffer == NULL || argBuffer->GCDesc == NULL)
        return;

    if (argBuffer->State == TAILCALLARGBUFFER_INACTIVE)
        return;

    bool instArgOnly = argBuffer->State == TAILCALLARGBUFFER_INSTARG_ONLY;

    GCRefMapDecoder decoder(static_cast<PTR_BYTE>(argBuffer->GCDesc));
    while (!decoder.AtEnd())
    {
        int pos = decoder.CurrentPos();
        int token = decoder.ReadToken();

        PTR_TADDR ppObj = dac_cast<PTR_TADDR>(((BYTE*)argBuffer->Args) + pos * sizeof(TADDR));
        switch (token)
        {
        case GCREFMAP_SKIP:
            break;
        case GCREFMAP_REF:
            if (!instArgOnly)
                fn(dac_cast<PTR_PTR_Object>(ppObj), sc, CHECK_APP_DOMAIN);
            break;
        case GCREFMAP_INTERIOR:
            if (!instArgOnly)
                PromoteCarefully(fn, dac_cast<PTR_PTR_Object>(ppObj), sc, GC_CALL_INTERIOR);
            break;
        case GCREFMAP_METHOD_PARAM:
            if (sc->promotion)
            {
#ifndef DACCESS_COMPILE
                MethodDesc *pMDReal = dac_cast<PTR_MethodDesc>(*ppObj);
                if (pMDReal != NULL)
                    GcReportLoaderAllocator(fn, sc, pMDReal->GetLoaderAllocator());
#endif
            }
            break;
        case GCREFMAP_TYPE_PARAM:
            if (sc->promotion)
            {
#ifndef DACCESS_COMPILE
                MethodTable *pMTReal = dac_cast<PTR_MethodTable>(*ppObj);
                if (pMTReal != NULL)
                    GcReportLoaderAllocator(fn, sc, pMTReal->GetLoaderAllocator());
#endif
            }
            break;
        default:
            _ASSERTE(!"Unhandled GCREFMAP token in arg buffer GC desc");
            break;
        }
    }
}

void GCToEEInterface::GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    STRESS_LOG1(LF_GCROOTS, LL_INFO10, "GCScan: Promotion Phase = %d\n", sc->promotion);

    Thread* pThread = NULL;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        gc_alloc_context* palloc_context = pThread->GetAllocContext();
        if (palloc_context != nullptr
            && GCHeapUtilities::GetGCHeap()->IsThreadUsingAllocationContextHeap(
                palloc_context, sc->thread_number))
        {
            STRESS_LOG2(LF_GC | LF_GCROOTS, LL_INFO100, "{ Starting scan of Thread %p ID = %x\n", pThread, pThread->GetThreadId());

            sc->thread_under_crawl = pThread;
#ifdef FEATURE_EVENT_TRACE
            sc->dwEtwRootKind = kEtwGCRootKindStack;
#endif // FEATURE_EVENT_TRACE
            ScanStackRoots(pThread, fn, sc);
            ScanTailCallArgBufferRoots(pThread, fn, sc);
            ScanThreadStaticRoots(pThread, fn, sc);
#ifdef FEATURE_EVENT_TRACE
            sc->dwEtwRootKind = kEtwGCRootKindOther;
#endif // FEATURE_EVENT_TRACE

            STRESS_LOG2(LF_GC | LF_GCROOTS, LL_INFO100, "Ending scan of Thread %p ID = 0x%x }\n", pThread, pThread->GetThreadId());
        }
    }

    // In server GC, we should be competing for marking the statics
    // It's better to do this *after* stack scanning, because this way
    // we can make up for imbalances in stack scanning
    // This would not apply to the initial mark phase in background GC,
    // but it would apply to blocking Gen 2 collections and the final
    // marking stage in background GC where we catch up to the user program
    if (GCHeapUtilities::MarkShouldCompeteForStatics())
    {
        if (condemned == max_gen && sc->promotion)
        {
            SystemDomain::EnumAllStaticGCRefs(fn, sc);
        }
    }
}

void GCToEEInterface::GcStartWork (int condemned, int max_gen)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef VERIFY_HEAP
    // Validate byrefs pinned by IL stubs since the last GC.
    StubHelpers::ProcessByrefValidationList();
#endif // VERIFY_HEAP

    ExecutionManager::CleanupCodeHeaps();

#ifdef FEATURE_EVENT_TRACE
    ETW::TypeSystemLog::Cleanup();
#endif

    Interop::OnGCStarted(condemned);

    if (condemned == max_gen)
    {
        ThreadStore::s_pThreadStore->OnMaxGenerationGCStarted();
    }
}

void GCToEEInterface::GcDone(int condemned)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Interop::OnGCFinished(condemned);
}

bool GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    //<REVISIT_TODO>@todo optimize the access to the ref-count
    ComCallWrapper* pWrap = ComCallWrapper::GetWrapperForObject((OBJECTREF)pObject);

    if (pWrap != NULL && pWrap->IsWrapperActive())
        return true;
#endif
#ifdef FEATURE_COMWRAPPERS
    bool isRooted = false;
    if (ComWrappersNative::IsManagedObjectComWrapper((OBJECTREF)pObject, &isRooted))
    {
        return isRooted;
    }
#endif
#ifdef FEATURE_OBJCMARSHAL
    bool isReferenced = false;
    if (ObjCMarshalNative::IsTrackedReference((OBJECTREF)pObject, &isReferenced))
        return isReferenced;
#endif

    return false;
}

void GCToEEInterface::TriggerClientBridgeProcessing(MarkCrossReferencesArgs* args)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_JAVAMARSHAL
    Interop::TriggerClientBridgeProcessing(args);
#endif // FEATURE_JAVAMARSHAL
}

bool GCToEEInterface::IsClientBridgeProcessingActive()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_JAVAMARSHAL
    return Interop::IsGCBridgeActive();
#else
    return false;
#endif // FEATURE_JAVAMARSHAL
}

void GCToEEInterface::SyncBlockCacheDemote(int max_gen)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    SyncBlockCache::GetSyncBlockCache()->GCDone(TRUE, max_gen);
}

void GCToEEInterface::SyncBlockCachePromotionsGranted(int max_gen)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    SyncBlockCache::GetSyncBlockCache()->GCDone(FALSE, max_gen);
}

uint32_t GCToEEInterface::GetActiveSyncBlockCount()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return SyncBlockCache::GetSyncBlockCache()->GetActiveCount();
}

gc_alloc_context * GCToEEInterface::GetAllocContext()
{
    WRAPPER_NO_CONTRACT;

    if (!::GetThreadNULLOk())
    {
        return nullptr;
    }

    return &t_runtime_thread_locals.alloc_context.m_GCAllocContext;
}

void GCToEEInterface::GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread * pThread = NULL;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        ee_alloc_context* palloc_context = pThread->GetEEAllocContext();
        if (palloc_context != nullptr)
        {
            gc_alloc_context* ac = &palloc_context->m_GCAllocContext;
            fn(ac, param);
            // The GC may zero the alloc_ptr and alloc_limit fields of AC during enumeration and we need to keep
            // m_CombinedLimit up-to-date. Note that the GC has multiple threads running this enumeration concurrently
            // with no synchronization. If you need to change this code think carefully about how that concurrency
            // may affect the results.
            if (ac->alloc_limit == 0 && palloc_context->m_CombinedLimit != 0)
            {
                palloc_context->m_CombinedLimit = 0;
            }
        }
    }
}


uint8_t* GCToEEInterface::GetLoaderAllocatorObjectForGC(Object* pObject)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return pObject->GetGCSafeMethodTable()->GetLoaderAllocatorObjectForGC();
}

bool GCToEEInterface::IsPreemptiveGCDisabled()
{
    WRAPPER_NO_CONTRACT;

    Thread* pThread = ::GetThreadNULLOk();
    return (pThread && pThread->PreemptiveGCDisabled());
}

bool GCToEEInterface::EnablePreemptiveGC()
{
    WRAPPER_NO_CONTRACT;

    Thread* pThread = ::GetThreadNULLOk();

    if (pThread && pThread->PreemptiveGCDisabled())
    {
        pThread->EnablePreemptiveGC();
        return true;
    }

    return false;
}

void GCToEEInterface::DisablePreemptiveGC()
{
    WRAPPER_NO_CONTRACT;

    Thread* pThread = ::GetThreadNULLOk();
    if (pThread)
    {
        pThread->DisablePreemptiveGC();
    }
}

Thread* GCToEEInterface::GetThread()
{
    WRAPPER_NO_CONTRACT;

    return ::GetThreadNULLOk();
}

//
// Diagnostics code
//

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

// Tracks all surviving objects (moved or otherwise).
inline bool ShouldTrackSurvivorsForProfilerOrEtw()
{
#ifdef GC_PROFILING
    if (CORProfilerTrackGC())
        return true;
#endif

#ifdef FEATURE_EVENT_TRACE
    if (ETW::GCLog::ShouldTrackMovementForEtw())
        return true;
#endif

    return false;
}

// Only tracks surviving objects in compacting GCs (moved or otherwise).
inline bool ShouldTrackSurvivorsInCompactingGCsForProfiler()
{
#ifdef GC_PROFILING
    if (CORProfilerTrackGCMovedObjects())
        return true;
#endif
    return false;
}
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

void ProfScanRootsHelper(Object** ppObject, ScanContext *pSC, uint32_t dwFlags)
{
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    Object *pObj = *ppObject;
    if (dwFlags & GC_CALL_INTERIOR)
    {
        pObj = GCHeapUtilities::GetGCHeap()->GetContainingObject(pObj, true);
        if (pObj == nullptr)
            return;
    }
    ScanRootsHelper(pObj, ppObject, pSC, dwFlags);
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
}

// TODO - at some point we would like to completely decouple profiling
// from ETW tracing using a pattern similar to this, where the
// ProfilingScanContext has flags about whether or not certain things
// should be tracked, and each one of these ProfilerShouldXYZ functions
// will check these flags and determine what to do based upon that.
// GCProfileWalkHeapWorker can, in turn, call those methods without fear
// of things being ifdef'd out.

// Returns TRUE if GC profiling is enabled and the profiler
// should scan dependent handles, FALSE otherwise.
BOOL ProfilerShouldTrackConditionalWeakTableElements()
{
#if defined(GC_PROFILING)
    return CORProfilerTrackConditionalWeakTableElements();
#else
    return FALSE;
#endif // defined (GC_PROFILING)
}

// If GC profiling is enabled, informs the profiler that we are done
// tracing dependent handles.
void ProfilerEndConditionalWeakTableElementReferences(void* heapId)
{
#if defined (GC_PROFILING)
    (&g_profControlBlock)->EndConditionalWeakTableElementReferences(heapId);
#else
    UNREFERENCED_PARAMETER(heapId);
#endif // defined (GC_PROFILING)
}

// If GC profiling is enabled, informs the profiler that we are done
// tracing root references.
void ProfilerEndRootReferences2(void* heapId)
{
#if defined (GC_PROFILING)
    (&g_profControlBlock)->EndRootReferences2(heapId);
#else
    UNREFERENCED_PARAMETER(heapId);
#endif // defined (GC_PROFILING)
}

void GcScanRootsForProfilerAndETW(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    Thread* pThread = NULL;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        sc->thread_under_crawl = pThread;
#ifdef FEATURE_EVENT_TRACE
        sc->dwEtwRootKind = kEtwGCRootKindStack;
#endif // FEATURE_EVENT_TRACE
        ScanStackRoots(pThread, fn, sc);
        ScanTailCallArgBufferRoots(pThread, fn, sc);
        ScanThreadStaticRoots(pThread, fn, sc);
#ifdef FEATURE_EVENT_TRACE
        sc->dwEtwRootKind = kEtwGCRootKindOther;
#endif // FEATURE_EVENT_TRACE
    }
}

void ScanHandleForProfilerAndETW(Object** pRef, Object* pSec, uint32_t flags, ScanContext* context, bool isDependent)
{
    ProfilingScanContext* pSC = (ProfilingScanContext*)context;

#ifdef GC_PROFILING
    // Give the profiler the objectref.
    if (pSC->fProfilerPinned)
    {
        if (!isDependent)
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC());
            (&g_profControlBlock)->RootReference2(
                (uint8_t *)*pRef,
                kEtwGCRootKindHandle,
                (EtwGCRootFlags)flags,
                pRef,
                &pSC->pHeapId);
            END_PROFILER_CALLBACK();
        }
        else
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackConditionalWeakTableElements());
            (&g_profControlBlock)->ConditionalWeakTableElementReference(
                (uint8_t*)*pRef,
                (uint8_t*)pSec,
                pRef,
                &pSC->pHeapId);
            END_PROFILER_CALLBACK();
        }
    }
#endif // GC_PROFILING

#if defined(FEATURE_EVENT_TRACE)
    // Notify ETW of the handle
    if (ETW::GCLog::ShouldWalkHeapRootsForEtw())
    {
        ETW::GCLog::RootReference(
            pRef,
            *pRef,          // object being rooted
            pSec,           // pSecondaryNodeForDependentHandle
            isDependent,
            pSC,
            0,              // dwGCFlags,
            flags);     // ETW handle flags
    }
#endif // defined(FEATURE_EVENT_TRACE)
}

// This is called only if we've determined that either:
//     a) The Profiling API wants to do a walk of the heap, and it has pinned the
//     profiler in place (so it cannot be detached), and it's thus safe to call into the
//     profiler, OR
//     b) ETW infrastructure wants to do a walk of the heap either to log roots,
//     objects, or both.
// This can also be called to do a single walk for BOTH a) and b) simultaneously.  Since
// ETW can ask for roots, but not objects
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
void GCProfileWalkHeapWorker(BOOL fProfilerPinned, BOOL fShouldWalkHeapRootsForEtw, BOOL fShouldWalkHeapObjectsForEtw)
{
    {
        ProfilingScanContext SC(fProfilerPinned);
        unsigned max_generation = GCHeapUtilities::GetGCHeap()->GetMaxGeneration();

        // **** Scan roots:  Only scan roots if profiling API wants them or ETW wants them.
        if (fProfilerPinned || fShouldWalkHeapRootsForEtw)
        {
            GcScanRootsForProfilerAndETW(&ProfScanRootsHelper, max_generation, max_generation, &SC);
            SC.dwEtwRootKind = kEtwGCRootKindFinalizer;
            GCHeapUtilities::GetGCHeap()->DiagScanFinalizeQueue(&ProfScanRootsHelper, &SC);

            // Handles are kept independent of wks/svr/concurrent builds
            SC.dwEtwRootKind = kEtwGCRootKindHandle;
            GCHeapUtilities::GetGCHeap()->DiagScanHandles(&ScanHandleForProfilerAndETW, max_generation, &SC);

            // indicate that regular handle scanning is over, so we can flush the buffered roots
            // to the profiler.  (This is for profapi only.  ETW will flush after the
            // entire heap was is complete, via ETW::GCLog::EndHeapDump.)
            if (fProfilerPinned)
            {
                ProfilerEndRootReferences2(&SC.pHeapId);
            }
        }

        // **** Scan dependent handles: only if the profiler supports it or ETW wants roots
        if ((fProfilerPinned && ProfilerShouldTrackConditionalWeakTableElements()) ||
            fShouldWalkHeapRootsForEtw)
        {
            // GcScanDependentHandlesForProfiler double-checks
            // CORProfilerTrackConditionalWeakTableElements() before calling into the profiler

            ProfilingScanContext* pSC = &SC;

            // we'll re-use pHeapId (which was either unused (0) or freed by EndRootReferences2
            // (-1)), so reset it to NULL
            _ASSERTE((*((size_t *)(&pSC->pHeapId)) == (size_t)(-1)) ||
                    (*((size_t *)(&pSC->pHeapId)) == (size_t)(0)));
            pSC->pHeapId = NULL;

            GCHeapUtilities::GetGCHeap()->DiagScanDependentHandles(&ScanHandleForProfilerAndETW, max_generation, &SC);

            // indicate that dependent handle scanning is over, so we can flush the buffered roots
            // to the profiler.  (This is for profapi only.  ETW will flush after the
            // entire heap was is complete, via ETW::GCLog::EndHeapDump.)
            if (fProfilerPinned && ProfilerShouldTrackConditionalWeakTableElements())
            {
                ProfilerEndConditionalWeakTableElementReferences(&SC.pHeapId);
            }
        }

        ProfilerWalkHeapContext profilerWalkHeapContext(fProfilerPinned, SC.pvEtwContext);

        // **** Walk objects on heap: only if profiling API wants them or ETW wants them.
        if (fProfilerPinned || fShouldWalkHeapObjectsForEtw)
        {
            GCHeapUtilities::GetGCHeap()->DiagWalkHeap(&HeapWalkHelper, &profilerWalkHeapContext, max_generation, true /* walk the large object heap */);
        }

#ifdef FEATURE_EVENT_TRACE
        // **** Done! Indicate to ETW helpers that the heap walk is done, so any buffers
        // should be flushed into the ETW stream
        if (fShouldWalkHeapObjectsForEtw || fShouldWalkHeapRootsForEtw)
        {
            ETW::GCLog::EndHeapDump(&profilerWalkHeapContext);
        }
#endif // FEATURE_EVENT_TRACE
    }
}
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

void GCProfileWalkHeap(bool etwOnly)
{
    BOOL fWalkedHeapForProfiler = FALSE;

#ifdef FEATURE_EVENT_TRACE
    if (ETW::GCLog::ShouldWalkStaticsAndCOMForEtw())
        ETW::GCLog::WalkStaticsAndCOMForETW();

    BOOL fShouldWalkHeapRootsForEtw = ETW::GCLog::ShouldWalkHeapRootsForEtw();
    BOOL fShouldWalkHeapObjectsForEtw = ETW::GCLog::ShouldWalkHeapObjectsForEtw();
#else // !FEATURE_EVENT_TRACE
    BOOL fShouldWalkHeapRootsForEtw = FALSE;
    BOOL fShouldWalkHeapObjectsForEtw = FALSE;
#endif // FEATURE_EVENT_TRACE

#if defined (GC_PROFILING)
    {
        BEGIN_PROFILER_CALLBACK(!etwOnly && CORProfilerTrackGC());
        GCProfileWalkHeapWorker(TRUE /* fProfilerPinned */, fShouldWalkHeapRootsForEtw, fShouldWalkHeapObjectsForEtw);
        fWalkedHeapForProfiler = TRUE;
        END_PROFILER_CALLBACK();
    }
#endif // defined (GC_PROFILING)

#if defined (GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    // we need to walk the heap if one of GC_PROFILING or FEATURE_EVENT_TRACE
    // is defined, since both of them make use of the walk heap worker.
    if (!fWalkedHeapForProfiler &&
        (fShouldWalkHeapRootsForEtw || fShouldWalkHeapObjectsForEtw))
    {
        GCProfileWalkHeapWorker(FALSE /* fProfilerPinned */, fShouldWalkHeapRootsForEtw, fShouldWalkHeapObjectsForEtw);
    }
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
}

void WalkFReachableObjects(bool isCritical, void* objectID)
{
#if defined(PROFILING_SUPPORTED)
    (&g_profControlBlock)->FinalizeableObjectQueued(isCritical, (ObjectID)objectID);
#endif // PROFILING_SUPPORTED
}

static fq_walk_fn g_FQWalkFn = &WalkFReachableObjects;

void GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
#ifdef GC_PROFILING
    DiagUpdateGenerationBounds();
    GarbageCollectionStartedCallback(gen, isInduced);
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC());
        size_t context = 0;

        // ObjectsAllocatedByClass callback can lead to enormous overhead in the case of Server GC,
        // so it was made skippable. See https://github.com/dotnet/runtime/issues/108230 for details.
        if (!CORProfilerSkipAllocatedByClassStatistic()) {
            // When we're walking objects allocated by class, then we don't want to walk the large
            // object heap because then it would count things that may have been around for a while.
            GCHeapUtilities::GetGCHeap()->DiagWalkHeap(&AllocByClassHelper, (void *)&context, 0, false);
        }

        // Notify that we've reached the end of the Gen 0 scan
        (&g_profControlBlock)->EndAllocByClass(&context);
        END_PROFILER_CALLBACK();
    }

#endif // GC_PROFILING
}

void GCToEEInterface::DiagUpdateGenerationBounds()
{
#ifdef GC_PROFILING
    if (CORProfilerTrackGC() || CORProfilerTrackBasicGC())
        UpdateGenerationBounds();
#endif // GC_PROFILING
}

void GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
#if defined(GC_PROFILING) || defined(PERFTRACING_DISABLE_THREADS)
    // We were only doing generation bounds and GC finish callback for non concurrent GCs so
    // I am keeping that behavior to not break profilers. But if BasicGC monitoring is enabled
    // we will do these for all GCs.
    if (!fConcurrent)
    {
        GCProfileWalkHeap(false);
    }
#endif // defined(GC_PROFILING) || defined(PERFTRACING_DISABLE_THREADS)

#ifdef GC_PROFILING
    if (CORProfilerTrackBasicGC() || (!fConcurrent && CORProfilerTrackGC()))
    {
        DiagUpdateGenerationBounds();
        GarbageCollectionFinishedCallback();
    }
#endif // GC_PROFILING
}

void GCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
#ifdef GC_PROFILING
    BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC());
    GCHeapUtilities::GetGCHeap()->DiagWalkFinalizeQueue(gcContext, g_FQWalkFn);
    END_PROFILER_CALLBACK();
#endif //GC_PROFILING
}

// Note on last parameter: when calling this for bgc, only ETW
// should be sending these events so that existing profapi profilers
// don't get confused.
void WalkMovedReferences(uint8_t* begin, uint8_t* end,
                         ptrdiff_t reloc,
                         void* context,
                         bool fCompacting,
                         bool fBGC)
{
    ETW::GCLog::MovedReference(begin, end,
                               (fCompacting ? reloc : 0),
                               (size_t)context,
                               fCompacting,
                               !fBGC);
}

void GCToEEInterface::DiagWalkSurvivors(void* gcContext, bool fCompacting)
{
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    if (ShouldTrackSurvivorsForProfilerOrEtw() ||
        (fCompacting && ShouldTrackSurvivorsInCompactingGCsForProfiler()))
    {
        size_t context = 0;
        ETW::GCLog::BeginMovedReferences(&context);
        GCHeapUtilities::GetGCHeap()->DiagWalkSurvivorsWithType(gcContext, &WalkMovedReferences, (void*)context, walk_for_gc);
        ETW::GCLog::EndMovedReferences(context);
    }
#endif //GC_PROFILING || FEATURE_EVENT_TRACE
}

void GCToEEInterface::DiagWalkUOHSurvivors(void* gcContext, int gen)
{
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    if (ShouldTrackSurvivorsForProfilerOrEtw())
    {
        size_t context = 0;
        ETW::GCLog::BeginMovedReferences(&context);
        GCHeapUtilities::GetGCHeap()->DiagWalkSurvivorsWithType(gcContext, &WalkMovedReferences, (void*)context, walk_for_uoh, gen);
        ETW::GCLog::EndMovedReferences(context);
    }
#endif //GC_PROFILING || FEATURE_EVENT_TRACE
}

void GCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    if (ShouldTrackSurvivorsForProfilerOrEtw())
    {
        size_t context = 0;
        ETW::GCLog::BeginMovedReferences(&context);
        GCHeapUtilities::GetGCHeap()->DiagWalkSurvivorsWithType(gcContext, &WalkMovedReferences, (void*)context, walk_for_bgc);
        ETW::GCLog::EndMovedReferences(context);
    }
#endif //GC_PROFILING || FEATURE_EVENT_TRACE
}

bool GCToEEInterface::SupportsWriteBarrierBitwiseRegion()
{
#if defined(TARGET_ARM64)
    return g_arm64_atomics_present;
#else
    return true;
#endif // TARGET_ARM64
}

uint8_t* GCToEEInterface::GetWriteBarrierCodeCopy()
{
    return ::GetWriteBarrierCodeCopy();
}

void GCToEEInterface::SetWriteBarrierHelpers(const WriteBarrierHelperDescriptor& helpers)
{
    ::SetWriteBarrierHelpers(helpers);
}

bool GCToEEInterface::IsWriteBarrierCodeCopyEnabled()
{
    return ::IsWriteBarrierCopyEnabled();
}

bool GCToEEInterface::IsServerGC()
{
#ifdef FEATURE_SVR_GC
    return GCHeapUtilities::IsServerHeap();
#else
    return false;
#endif // FEATURE_SVR_GC
}

bool GCToEEInterface::UseSlowDebugWriteBarrier()
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
#ifdef _DEBUG
    return (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) != 0;
#else
    return false;
#endif // _DEBUG
#elif defined(TARGET_X86) && defined(WRITE_BARRIER_CHECK)
    return (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) != 0;
#else
    return false;
#endif
}

void GCToEEInterface::CopyWriteBarrierCode(uint8_t* destination, const uint8_t* source, size_t size)
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    ExecutableWriterHolder<void> writer(destination, size);
#else
    ExecutableWriterHolderNoLog<void> writer(destination, size);
#endif
    memcpy(writer.GetRW(), source, size);
}

void GCToEEInterface::PatchWriteBarrierPointer(uint8_t* destination, uint8_t* value)
{
#ifdef TARGET_ARM
    ExecutableWriterHolderNoLog<uint8_t> writer(destination, 2 * sizeof(uint32_t));
    PutThumb2Mov32(
        reinterpret_cast<uint16_t*>(writer.GetRW()),
        static_cast<uint32_t>(reinterpret_cast<uintptr_t>(value)));
#else
    UpdateWriteBarrierValue(destination, reinterpret_cast<uintptr_t>(value), sizeof(uintptr_t));
#endif // TARGET_ARM
}

void GCToEEInterface::UpdateWriteBarrierValue(uint8_t* destination, uint64_t value, size_t size)
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    switch (size)
    {
        case sizeof(uint8_t):
        {
            ExecutableWriterHolder<uint8_t> writer(destination, size);
            *writer.GetRW() = static_cast<uint8_t>(value);
            break;
        }

        case sizeof(uint16_t):
        {
            ExecutableWriterHolder<uint16_t> writer(reinterpret_cast<uint16_t*>(destination), size);
            *writer.GetRW() = static_cast<uint16_t>(value);
            break;
        }

        case sizeof(uint32_t):
        {
            ExecutableWriterHolder<uint32_t> writer(reinterpret_cast<uint32_t*>(destination), size);
            *writer.GetRW() = static_cast<uint32_t>(value);
            break;
        }

        case sizeof(uint64_t):
        {
            ExecutableWriterHolder<uint64_t> writer(reinterpret_cast<uint64_t*>(destination), size);
            *writer.GetRW() = value;
            break;
        }

        default:
            _ASSERTE(!"unsupported write barrier value size");
            break;
    }
#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    _ASSERTE(size == sizeof(uint64_t));
    ExecutableWriterHolderNoLog<uint64_t> writer(reinterpret_cast<uint64_t*>(destination), size);
    VolatileStoreWithoutBarrier(writer.GetRW(), value);
#else
    _ASSERTE(size == sizeof(uint32_t));
    ExecutableWriterHolderNoLog<uint32_t> writer(reinterpret_cast<uint32_t*>(destination), size);
    *writer.GetRW() = static_cast<uint32_t>(value);
#endif
}

bool GCToEEInterface::EnterWriteBarrierPatchMode()
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    Thread* currentThread = GetThreadNULLOk();
    if (currentThread == nullptr || currentThread->PreemptiveGCDisabled())
    {
        return false;
    }

    currentThread->DisablePreemptiveGC();
    return true;
#elif defined(TARGET_X86)
    Thread* currentThread = GetThreadNULLOk();
    if (currentThread == nullptr || currentThread->PreemptiveGCDisabled())
    {
        return false;
    }

    currentThread->DisablePreemptiveGC();
    return true;
#elif defined(TARGET_ARM)
    GCStressPolicy::GlobalDisable();
    return true;
#else
    return false;
#endif
}

void GCToEEInterface::ExitWriteBarrierPatchMode(bool modeChanged)
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_X86)
    if (modeChanged)
    {
        GetThread()->EnablePreemptiveGC();
    }
#elif defined(TARGET_ARM)
    if (modeChanged)
    {
        GCStressPolicy::GlobalEnable();
    }
#else
    _ASSERTE(!modeChanged);
#endif
}

void GCToEEInterface::SuspendForWriteBarrier()
{
#ifdef TARGET_ARM
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
#else
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_GC_PREP);
#endif
}

void GCToEEInterface::RestartForWriteBarrier()
{
    ThreadSuspend::RestartEE(true /* SuspendSucceeded */);
}

void GCToEEInterface::FlushWriteBarrierInstructionCache(uint8_t* code, size_t size)
{
#ifdef TARGET_X86
    ClrFlushInstructionCache(code, size, true);
#else
    ::FlushInstructionCache(GetCurrentProcess(), code, size);
#endif
}

void GCToEEInterface::WriteBarrierAssert(void* destination, void* reference)
{
#if defined(TARGET_X86) && defined(_DEBUG)
    BYTE* destinationAddress = static_cast<BYTE*>(destination);
    Object* object = static_cast<Object*>(reference);
    static BOOL verifyHeap = -1;

    if (verifyHeap == -1)
    {
        verifyHeap = g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_GC;
    }

    if (verifyHeap)
    {
        if (object != nullptr)
        {
            object->Validate(FALSE);
        }

        if (GCHeapUtilities::GetGCHeap()->IsHeapPointer(destinationAddress))
        {
            Object* destinationObject = *reinterpret_cast<Object**>(destinationAddress);
            _ASSERTE(
                destinationObject == nullptr ||
                GCHeapUtilities::GetGCHeap()->IsHeapPointer(destinationObject));
        }
    }
    else
    {
        _ASSERTE(
            GCHeapUtilities::IsInGCHeap(destinationAddress) ||
            reinterpret_cast<size_t>(destinationAddress) < MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT);
    }
#else
    UNREFERENCED_PARAMETER(destination);
    UNREFERENCED_PARAMETER(reference);
#endif // TARGET_X86 && _DEBUG
}

void GCToEEInterface::UpdateRuntimeWriteBarrierState(const WriteBarrierParameters&)
{
}

void GCToEEInterface::EnableFinalization(bool gcHasWorkForFinalizerThread)
{
    if (gcHasWorkForFinalizerThread || FinalizerThread::HaveExtraWorkForFinalizer())
    {
        FinalizerThread::EnableFinalization();
    }
}

void GCToEEInterface::HandleFatalError(unsigned int exitCode)
{
    EEPOLICY_HANDLE_FATAL_ERROR(exitCode);
}

bool GCToEEInterface::EagerFinalized(Object* obj)
{
    MethodTable* pMT = obj->GetGCSafeMethodTable();
    if (pMT == g_pWeakReferenceClass ||
        pMT->HasSameTypeDefAs(g_pWeakReferenceOfTClass))
    {
        FinalizeWeakReference(obj);
        return true;
    }
#ifdef FEATURE_OBJCMARSHAL
    else if (pMT->IsTrackedReferenceWithFinalizer())
    {
        ObjCMarshalNative::OnEnteredFinalizerQueue((OBJECTREF)obj);
        return false;
    }
#endif // FEATURE_OBJCMARSHAL

    return false;
}

MethodTable* GCToEEInterface::GetFreeObjectMethodTable()
{
    assert(g_pFreeObjectMethodTable != nullptr);
    return g_pFreeObjectMethodTable;
}

// This is arbitrary, we shouldn't ever be having config keys
// longer than these lengths.
const size_t MaxConfigKeyLength = 255;

bool GCToEEInterface::GetBooleanConfigValue(const char* privateKey, const char* publicKey, bool* value)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#ifdef FEATURE_INTERPRETER
    if (strcmp(privateKey, "gcConservative") == 0)
    {
        *value = true;
        return true;
    }
#endif

    // these configuration values are given to us via startup flags.
    if (strcmp(privateKey, "gcServer") == 0)
    {
        *value = g_heap_type == GC_HEAP_SVR;
        return true;
    }

    if (strcmp(privateKey, "gcConcurrent") == 0)
    {
        *value = !!g_pConfig->GetGCconcurrent();
        return true;
    }

    if (strcmp(privateKey, "GCRetainVM") == 0)
    {
        *value = !!g_pConfig->GetGCRetainVM();
        return true;
    }

    WCHAR configKey[MaxConfigKeyLength];
    if (MultiByteToWideChar(CP_ACP, 0, privateKey, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
    {
        // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
        return false;
    }

    // otherwise, ask the config subsystem.
    if (CLRConfig::IsConfigOptionSpecified(configKey))
    {
        CLRConfig::ConfigDWORDInfo info { configKey , 0, CLRConfig::LookupOptions::Default };
        *value = CLRConfig::GetConfigValue(info) != 0;
        return true;
    }
    else if (publicKey != NULL)
    {
        if (MultiByteToWideChar(CP_ACP, 0, publicKey, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
        {
            // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
            return false;
        }
        if (Configuration::GetKnobStringValue(configKey) != NULL)
        {
            *value = Configuration::GetKnobBooleanValue(configKey, false);
            return true;
        }
    }

    return false;
}

bool GCToEEInterface::GetIntConfigValue(const char* privateKey, const char* publicKey, int64_t* value)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    if (g_gcHeapHardLimitInfoSpecified)
    {
        if ((g_gcHeapHardLimitInfo.heapHardLimit != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimit") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimit; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitPercent; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitSOH != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitSOH") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitSOH; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitLOH != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitLOH") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitLOH; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitPOH != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitPOH") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitPOH; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitSOHPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitSOHPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitSOHPercent; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitLOHPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitLOHPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitLOHPercent; return true; }
        if ((g_gcHeapHardLimitInfo.heapHardLimitPOHPercent != UINT64_MAX) && strcmp(privateKey, "GCHeapHardLimitPOHPercent") == 0) { *value = g_gcHeapHardLimitInfo.heapHardLimitPOHPercent; return true; }
    }

    WCHAR configKey[MaxConfigKeyLength];
    if (MultiByteToWideChar(CP_ACP, 0, privateKey, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
    {
        // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
        return false;
    }

    // There is no ConfigULONGLONGInfo, and the GC uses 64 bit values for things like GCHeapAffinitizeMask,
    // so have to fake it with getting the string and converting to uint64_t
    if (CLRConfig::IsConfigOptionSpecified(configKey))
    {
        CLRConfig::ConfigStringInfo info { configKey, CLRConfig::LookupOptions::Default };
        LPWSTR out = CLRConfig::GetConfigValue(info);
        if (!out)
        {
            // config not found
            CLRConfig::FreeConfigString(out);
            return false;
        }

        WCHAR *end;
        uint64_t result;
        errno = 0;
        result = u16_strtoui64(out, &end, 16);
        // errno is ERANGE if the number is out of range, and end is set to pvalue if
        // no valid conversion exists.
        if (errno == ERANGE || end == out)
        {
            CLRConfig::FreeConfigString(out);
            return false;
        }

        *value = static_cast<int64_t>(result);
        CLRConfig::FreeConfigString(out);
        return true;
    }
    else if (publicKey != NULL)
    {
        if (MultiByteToWideChar(CP_ACP, 0, publicKey, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
        {
            // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
            return false;
        }
        if (Configuration::GetKnobStringValue(configKey) != NULL)
        {
            *value = Configuration::GetKnobULONGLONGValue(configKey, 0);
            return true;
        }
    }

    return false;
}

bool GCToEEInterface::GetStringConfigValue(const char* privateKey, const char* publicKey, const char** value)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    WCHAR configKey[MaxConfigKeyLength];
    if (MultiByteToWideChar(CP_ACP, 0, privateKey, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
    {
        // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
        return false;
    }

    CLRConfig::ConfigStringInfo info { configKey, CLRConfig::LookupOptions::Default };
    LPWSTR fromClrConfig = CLRConfig::GetConfigValue(info);
    LPCWSTR out = fromClrConfig;
    if (out == NULL)
    {
        if (publicKey != NULL)
        {
            if (MultiByteToWideChar(CP_ACP, 0, publicKey, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
            {
                // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
                return false;
            }
            out =  Configuration::GetKnobStringValue(configKey);
            if (out == NULL)
            {
                return false;
            }
        }
    }

    int charCount = WideCharToMultiByte(CP_ACP, 0, out, -1 /* out is null-terminated */, NULL, 0, nullptr, nullptr);
    if (charCount == 0)
    {
        // this should only happen if the config subsystem gives us a string that's not valid
        // unicode.
        if (fromClrConfig)
        {
            CLRConfig::FreeConfigString(fromClrConfig);
        }
        return false;
    }

    // not allocated on the stack since it escapes this function
    AStringHolder configResult = new (nothrow) char[charCount];
    if (!configResult)
    {
        if (fromClrConfig)
        {
            CLRConfig::FreeConfigString(fromClrConfig);
        }
        return false;
    }

    if (WideCharToMultiByte(CP_ACP, 0, out, -1 /* out is null-terminated */,
          configResult.GetValue(), charCount, nullptr, nullptr) == 0)
    {
        // this should never happen, the previous call to WideCharToMultiByte that computed the charCount should
        // have caught all issues.
        assert(false);
        if (fromClrConfig)
        {
            CLRConfig::FreeConfigString(fromClrConfig);
        }
        return false;
    }

    *value = configResult.Extract();
    if (fromClrConfig)
    {
        CLRConfig::FreeConfigString(fromClrConfig);
    }
    return true;
}

void GCToEEInterface::FreeStringConfigValue(const char* value)
{
    delete [] value;
}

bool GCToEEInterface::IsGCThread()
{
    return !!::IsGCThread();
}

bool GCToEEInterface::WasCurrentThreadCreatedByGC()
{
    return !!::IsGCSpecialThread();
}

struct SuspendableThreadStubArguments
{
    void* Argument;
    void (*ThreadStart)(void*);
    class Thread* Thread;
    bool HasStarted;
    CLREvent ThreadStartedEvent;
#ifdef __APPLE__
    const WCHAR* name;
#endif //__APPLE__
};

struct ThreadStubArguments
{
    void* Argument;
    void (*ThreadStart)(void*);
    HANDLE Thread;
    bool HasStarted;
    CLREvent ThreadStartedEvent;
#ifdef __APPLE__
    const WCHAR* name;
#endif //__APPLE__
};

namespace
{
    const size_t MaxThreadNameSize = 255;

    bool CreateSuspendableThread(
        void (*threadStart)(void*),
        void* argument,
        const WCHAR* name)
    {
        LIMITED_METHOD_CONTRACT;

        SuspendableThreadStubArguments args;
        args.Argument = argument;
        args.ThreadStart = threadStart;
        args.Thread = nullptr;
        args.HasStarted = false;
#ifdef __APPLE__
        args.name = name;
#endif //__APPLE__
        if (!args.ThreadStartedEvent.CreateAutoEventNoThrow(FALSE))
        {
            return false;
        }

        EX_TRY
        {
            args.Thread = SetupUnstartedThread(SUTF_ThreadStoreLockAlreadyTaken);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH

        if (!args.Thread)
        {
            args.ThreadStartedEvent.CloseEvent();
            return false;
        }

        auto threadStub = [](void* argument) -> DWORD
        {
            SuspendableThreadStubArguments* args = static_cast<SuspendableThreadStubArguments*>(argument);
            assert(args != nullptr);

#ifdef __APPLE__
            SetThreadName(GetCurrentThread(), args->name);
#endif //__APPLE__

            ClrFlsSetThreadType(ThreadType_GC);
            args->Thread->SetGCSpecial();
            STRESS_LOG_RESERVE_MEM(GC_STRESSLOG_MULTIPLY);
            args->HasStarted = !!args->Thread->HasStarted();

            Thread* thread = args->Thread;
            auto threadStart = args->ThreadStart;
            void* threadArgument = args->Argument;
            bool hasStarted = args->HasStarted;
            args->ThreadStartedEvent.Set();

            // The stubArgs cannot be used once the event is set, since that releases wait on the
            // event in the function that created this thread and the stubArgs go out of scope.
            if (hasStarted)
            {
                threadStart(threadArgument);
                DestroyThread(thread);
            }

            return 0;
        };
        if (!args.Thread->CreateNewThread(0, threadStub, &args, name))
        {
            args.Thread->DecExternalCount(FALSE);
            args.ThreadStartedEvent.CloseEvent();
            return false;
        }

        args.Thread->SetBackground(TRUE);
        args.Thread->StartThread();

        // Wait for the thread to be in its main loop
        uint32_t res = args.ThreadStartedEvent.Wait(INFINITE, FALSE);
        args.ThreadStartedEvent.CloseEvent();
        _ASSERTE(res == WAIT_OBJECT_0);

        if (!args.HasStarted)
        {
            // The thread has failed to start and the Thread object was destroyed in the Thread::HasStarted
            // failure code path.
            return false;
        }

        return true;
    }

    bool CreateNonSuspendableThread(
        void (*threadStart)(void*),
        void* argument,
        const WCHAR* name)
    {
        LIMITED_METHOD_CONTRACT;

        ThreadStubArguments args;
        args.Argument = argument;
        args.ThreadStart = threadStart;
        args.Thread = NULL;
#ifdef __APPLE__
        args.name = name;
#endif //__APPLE__
        if (!args.ThreadStartedEvent.CreateAutoEventNoThrow(FALSE))
        {
            return false;
        }

        auto threadStub = [](void* argument) -> DWORD
        {
            ThreadStubArguments* args = static_cast<ThreadStubArguments*>(argument);
            assert(args != nullptr);

#ifdef __APPLE__
            SetThreadName(GetCurrentThread(), args->name);
#endif //__APPLE__

            ClrFlsSetThreadType(ThreadType_GC);
            STRESS_LOG_RESERVE_MEM(GC_STRESSLOG_MULTIPLY);

            args->HasStarted = true;
            auto threadStart = args->ThreadStart;
            void* threadArgument = args->Argument;
            args->ThreadStartedEvent.Set();

            // The stub args cannot be used once the event is set, since that releases wait on the
            // event in the function that created this thread and the stubArgs go out of scope.
            threadStart(threadArgument);
            return 0;
        };

        args.Thread = Thread::CreateUtilityThread(Thread::StackSize_Medium, threadStub, &args, name);
        if (args.Thread == NULL)
        {
            args.ThreadStartedEvent.CloseEvent();
            return false;
        }

        // Wait for the thread to be in its main loop
        uint32_t res = args.ThreadStartedEvent.Wait(INFINITE, FALSE);
        args.ThreadStartedEvent.CloseEvent();
        _ASSERTE(res == WAIT_OBJECT_0);

        CloseHandle(args.Thread);
        return true;
    }
} // anonymous namespace

bool GCToEEInterface::CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
{
    InlineSString<MaxThreadNameSize> wideName;
    const WCHAR* namePtr = nullptr;
    EX_TRY
    {
        if (name != nullptr)
        {
            wideName.SetUTF8(name);
            namePtr = wideName.GetUnicode();
        }
    }
        EX_CATCH
    {
        // we're not obligated to provide a name - if it's not valid,
        // just report nullptr as the name.
    }
    EX_END_CATCH

    LIMITED_METHOD_CONTRACT;
    if (is_suspendable)
    {
        return CreateSuspendableThread(threadStart, arg, namePtr);
    }
    else
    {
        return CreateNonSuspendableThread(threadStart, arg, namePtr);
    }
}

void GCToEEInterface::WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback)
{
    LIMITED_METHOD_CONTRACT;

    assert(object != nullptr);
    assert(sc != nullptr);
    assert(callback != nullptr);

    // Unused
}

void GCToEEInterface::WalkAsyncPinned(Object* object, void* context, void (*callback)(Object*, Object*, void*))
{
    LIMITED_METHOD_CONTRACT;

    assert(object != nullptr);
    assert(callback != nullptr);

    // Unused
}

IGCToCLREventSink* GCToEEInterface::EventSink()
{
    LIMITED_METHOD_CONTRACT;

    return &g_gcToClrEventSink;
}

uint32_t GCToEEInterface::GetTotalNumSizedRefHandles()
{
    LIMITED_METHOD_CONTRACT;

    return 0;
}

NormalizedTimer analysisTimer;

bool GenAwareMatchingGeneration(int condemnedGeneration)
{
    return (gcGenAnalysisState == GcGenAnalysisState::Enabled) && (condemnedGeneration == gcGenAnalysisGen);
}

bool GenAwareMatchingCondition(size_t gcIndex, int condemnedGeneration, uint64_t promoted_bytes, uint64_t elapsed)
{
    if (!GenAwareMatchingGeneration(condemnedGeneration))
    {
        return false;
    }
    if (gcIndex < (uint64_t)gcGenAnalysisIndex)
    {
        return false;
    }
    if ((gcGenAnalysisBytes > 0) && (promoted_bytes <= gcGenAnalysisBytes))
    {
        return false;
    }
    if ((gcGenAnalysisTime > 0) && (elapsed <= gcGenAnalysisTime))
    {
        return false;
    }
    return true;
}

bool GCToEEInterface::AnalyzeSurvivorsRequested(int condemnedGeneration)
{
    LIMITED_METHOD_CONTRACT;

    if (GenAwareMatchingGeneration(condemnedGeneration) && gcGenAnalysisTime > 0)
    {
        analysisTimer.Start();
    }

    GcEvtArgs gea = { GC_MARK_END, { (1<<condemnedGeneration) } };
    return GcNotifications::GetNotification(gea);
}

void GCToEEInterface::AnalyzeSurvivorsFinished(size_t gcIndex, int condemnedGeneration, uint64_t promoted_bytes, void (*reportGenerationBounds)())
{
    LIMITED_METHOD_CONTRACT;

    uint64_t elapsed = 0;
    if (GenAwareMatchingGeneration(condemnedGeneration) && gcGenAnalysisTime > 0)
    {
        analysisTimer.Stop();
        elapsed = analysisTimer.Elapsed100nsTicks();
    }

    GcEvtArgs gea = { GC_MARK_END, { (1<<condemnedGeneration) } };
    if (GcNotifications::GetNotification(gea))
    {
        DACNotify::DoGCNotification(gea);
    }

    if (gcGenAnalysisState == GcGenAnalysisState::Enabled)
    {
#ifndef GEN_ANALYSIS_STRESS
        if (GenAwareMatchingCondition(gcIndex, condemnedGeneration, promoted_bytes, elapsed))
#endif
        {
            if (gcGenAnalysisTrace)
            {
#ifdef FEATURE_PERFTRACING
                EventPipeAdapter::ResumeSession(gcGenAnalysisEventPipeSession);
                FireEtwGenAwareBegin((int)gcIndex, GetClrInstanceId());
                s_forcedGCInProgress = true;
                GCProfileWalkHeap(true);
                s_forcedGCInProgress = false;
                reportGenerationBounds();
                FireEtwGenAwareEnd((int)gcIndex, GetClrInstanceId());
                EventPipeAdapter::PauseSession(gcGenAnalysisEventPipeSession);
#endif //FEATURE_PERFTRACING
            }
            if (gcGenAnalysisDump)
            {
                EX_TRY
                {
                    WCHAR outputPath[MAX_PATH];
                    ReplacePid(GENAWARE_DUMP_FILE_NAME, outputPath, MAX_PATH);
                    GenerateDump (outputPath, 2, GenerateDumpFlagsNone, nullptr, 0);
                }
                EX_CATCH {}
                EX_END_CATCH
            }
            gcGenAnalysisState = GcGenAnalysisState::Done;
            EnableFinalization(true);
        }
    }
}

void GCToEEInterface::VerifySyncTableEntry()
{
    LIMITED_METHOD_CONTRACT;

#ifdef VERIFY_HEAP
    SyncBlockCache::GetSyncBlockCache()->VerifySyncTableEntry();
#endif // VERIFY_HEAP
}

void GCToEEInterface::UpdateGCEventStatus(int currentPublicLevel, int currentPublicKeywords, int currentPrivateLevel, int currentPrivateKeywords)
{
#if defined(__linux__) && defined(FEATURE_EVENT_TRACE)
    LIMITED_METHOD_CONTRACT;
    // LTTng does not have a notion of enabling events via "keyword"/"level" but we have to
    // somehow implement a similar behavior to it.

    // To do this, we manaully check for events that are enabled via different provider/keywords/level.
    // Ex 1. GCJoin_V2 is what we use to check whether the GC keyword is enabled in verbose level in the public provider
    // Ex 2. SetGCHandle is what we use to check whether the GCHandle keyword is enabled in informational level in the public provider
    // Refer to the comments in src/gc/gcevents.h see which events are enabled.

    // WARNING: To change an event's GC level, perfcollect script needs to be updated simultaneously to reflect it.
    BOOL keyword_gc_verbose = EventXplatEnabledGCJoin_V2() || EventPipeEventEnabledGCJoin_V2();
    BOOL keyword_gc_informational = EventXplatEnabledGCStart() || EventPipeEventEnabledGCStart();

    BOOL keyword_gc_heapsurvival_and_movement_informational = EventXplatEnabledGCGenerationRange() || EventPipeEventEnabledGCGenerationRange();
    BOOL keyword_gchandle_informational = EventXplatEnabledSetGCHandle() || EventPipeEventEnabledSetGCHandle();
    BOOL keyword_gchandle_prv_informational = EventXplatEnabledPrvSetGCHandle() || EventPipeEventEnabledPrvSetGCHandle();

    BOOL prv_gcprv_informational = EventXplatEnabledBGCBegin() || EventPipeEventEnabledBGCBegin();
    BOOL prv_gcprv_verbose = EventXplatEnabledPinPlugAtGCTime() || EventPipeEventEnabledPinPlugAtGCTime();

    int publicProviderLevel = keyword_gc_verbose ? GCEventLevel_Verbose :
                                 ((keyword_gc_informational || keyword_gc_heapsurvival_and_movement_informational) ? GCEventLevel_Information : GCEventLevel_None);
    int publicProviderKeywords = (keyword_gc_informational ? GCEventKeyword_GC : GCEventKeyword_None) |
                                 (keyword_gchandle_informational ? GCEventKeyword_GCHandle : GCEventKeyword_None) |
                                 (keyword_gc_heapsurvival_and_movement_informational ? GCEventKeyword_GCHeapSurvivalAndMovement : GCEventKeyword_None);

    int privateProviderLevel = prv_gcprv_verbose ? GCEventLevel_Verbose : (prv_gcprv_informational ? GCEventLevel_Information : GCEventLevel_None);
    int privateProviderKeywords = (prv_gcprv_informational ? GCEventKeyword_GCPrivate : GCEventKeyword_None) |
        (keyword_gchandle_prv_informational ? GCEventKeyword_GCHandlePrivate : GCEventKeyword_None);

    if (publicProviderLevel != currentPublicLevel || publicProviderKeywords != currentPublicKeywords)
    {
        GCEventLevel publicLevel = static_cast<GCEventLevel>(publicProviderLevel);
        GCEventKeyword publicKeywords = static_cast<GCEventKeyword>(publicProviderKeywords);
        GCHeapUtilities::RecordEventStateChange(true, publicKeywords, publicLevel);
    }
    if (privateProviderLevel != currentPrivateLevel || privateProviderKeywords != currentPrivateKeywords)
    {
        GCEventLevel privateLevel = static_cast<GCEventLevel>(privateProviderLevel);
        GCEventKeyword privateKeywords = static_cast<GCEventKeyword>(privateProviderKeywords);
        GCHeapUtilities::RecordEventStateChange(false, privateKeywords, privateLevel);
    }
#endif // __linux__ && FEATURE_EVENT_TRACE
}

void GCToEEInterface::LogStressMsg(unsigned level, unsigned facility, const StressLogMsg &msg)
{
    StressLog::LogMsg(level, facility, msg);
}

uint32_t GCToEEInterface::GetCurrentProcessCpuCount()
{
    return ::GetCurrentProcessCpuCount();
}

void GCToEEInterface::DiagAddNewRegion(int generation, uint8_t* rangeStart, uint8_t* rangeEnd, uint8_t* rangeEndReserved)
{
    ProfilerAddNewRegion(generation, rangeStart, rangeEnd, rangeEndReserved);
}

void GCToEEInterface::LogErrorToHost(const char *message)
{
    ::LogErrorToHost("GC: %s", message);
}

uint64_t GCToEEInterface::GetThreadOSThreadId(Thread* thread)
{
    return thread->GetOSThreadId64();
}
