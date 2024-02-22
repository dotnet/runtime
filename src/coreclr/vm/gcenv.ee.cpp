// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * GCENV.EE.CPP
 *
 * GCToEEInterface implementation
 *

 *
 */

#include "common.h"
#include "gcenv.h"
#include "../gc/env/gcenv.ee.h"
#include "threadsuspend.h"
#include "interoplibinterface.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#endif // FEATURE_COMINTEROP

#include "gctoclreventsink.h"
#include "configuration.h"
#include "genanalysis.h"
#include "eventpipeadapter.h"

// Finalizes a weak reference directly.
extern void FinalizeWeakReference(Object* obj);

extern GCHeapHardLimitInfo g_gcHeapHardLimitInfo;
extern bool g_gcHeapHardLimitInfoSpecified;

#include <generatedumpflags.h>
#include "gcrefmap.h"

void GCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
    WRAPPER_NO_CONTRACT;

    static_assert_no_msg(SUSPEND_FOR_GC == (int)ThreadSuspend::SUSPEND_FOR_GC);
    static_assert_no_msg(SUSPEND_FOR_GC_PREP == (int)ThreadSuspend::SUSPEND_FOR_GC_PREP);

    _ASSERTE(reason == SUSPEND_FOR_GC || reason == SUSPEND_FOR_GC_PREP);

    g_pDebugInterface->SuspendForGarbageCollectionStarted();

    ThreadSuspend::SuspendEE((ThreadSuspend::SUSPEND_REASON)reason);

    g_pDebugInterface->SuspendForGarbageCollectionCompleted();
}

void GCToEEInterface::RestartEE(bool bFinishedGC)
{
    WRAPPER_NO_CONTRACT;

    g_pDebugInterface->ResumeForGarbageCollectionStarted();

    ThreadSuspend::RestartEE(bFinishedGC, TRUE);
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

#if defined(FEATURE_CONSERVATIVE_GC) || defined(USE_FEF)
    Frame* pTopFrame = pThread->GetFrame();
    Object ** topStack = (Object **)pTopFrame;
    if (InlinedCallFrame::FrameHasActiveCall(pTopFrame))
    {
        // It is an InlinedCallFrame with active call. Get SP from it.
        InlinedCallFrame* pInlinedFrame = (InlinedCallFrame*)pTopFrame;
        topStack = (Object **)pInlinedFrame->GetCallSiteSP();
    }
#endif // FEATURE_CONSERVATIVE_GC || USE_FEF

#ifdef USE_FEF
    // We only set the stack_limit when FEF (FaultingExceptionFrame) is enabled, because without the
    // FEF, the code above would have to check if hardware exception is being handled and get the limit
    // from the exception frame. Since the stack_limit is strictly necessary only on Unix and FEF is
    // not enabled on Window x86 only, it is sufficient to keep the stack_limit set to 0 in this case.
    // See the comment on the stack_limit usage in the PromoteCarefully function for more details.
    sc->stack_limit = (uintptr_t)topStack;
#else // USE_FEF
    // It should be set to 0 in the ScanContext constructor
    _ASSERTE(sc->stack_limit == 0);
#endif // USE_FEF

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
            for (walk = topStack; walk < bottomStack; walk ++)
            {
                if (((void*)*walk > (void*)bottomStack || (void*)*walk < (void*)topStack) &&
                    ((void*)*walk >= (void*)g_lowest_address && (void*)*walk <= (void*)g_highest_address)
                    )
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
#if defined(FEATURE_EH_FUNCLETS)
        flagsStackWalk |= GC_FUNCLET_REFERENCE_REPORTING;
#endif // defined(FEATURE_EH_FUNCLETS)
        pThread->StackWalkFrames( GcStackCrawlCallBack, &gcctx, flagsStackWalk);
    }

    GCFrame* pGCFrame = pThread->GetGCFrame();
    while (pGCFrame != NULL)
    {
        pGCFrame->GcScanRoots(fn, sc);
        pGCFrame = pGCFrame->PtrNextFrame();
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

    if (argBuffer->State == TAILCALLARGBUFFER_ABANDONED)
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
        if (GCHeapUtilities::GetGCHeap()->IsThreadUsingAllocationContextHeap(
            pThread->GetAllocContext(), sc->thread_number))
        {
            STRESS_LOG2(LF_GC | LF_GCROOTS, LL_INFO100, "{ Starting scan of Thread %p ID = %x\n", pThread, pThread->GetThreadId());

            sc->thread_under_crawl = pThread;
#ifdef FEATURE_EVENT_TRACE
            sc->dwEtwRootKind = kEtwGCRootKindStack;
#endif // FEATURE_EVENT_TRACE
            ScanStackRoots(pThread, fn, sc);
            ScanTailCallArgBufferRoots(pThread, fn, sc);
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
    if (ComWrappersNative::HasManagedObjectComWrapper((OBJECTREF)pObject, &isRooted))
        return isRooted;
#endif
#ifdef FEATURE_OBJCMARSHAL
    bool isReferenced = false;
    if (ObjCMarshalNative::IsTrackedReference((OBJECTREF)pObject, &isReferenced))
        return isReferenced;
#endif

    return false;
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

    Thread* pThread = ::GetThreadNULLOk();
    if (!pThread)
    {
        return nullptr;
    }

    return pThread->GetAllocContext();
}

void InvokeGCAllocCallback(ee_alloc_context* pEEAllocContext, enum_alloc_context_func* fn, void* param)
{

    gc_alloc_context* pAllocContext = &pEEAllocContext->gc_alloc_context;

    // The allocation context might be modified by the callback, so we need to save
    // the remaining sampling budget and restore it after the callback.
    size_t remainingSamplingBudget = (pEEAllocContext->fast_alloc_helper_limit_ptr == nullptr) ? 0 : pEEAllocContext->fast_alloc_helper_limit_ptr - pAllocContext->alloc_ptr;

    fn(pAllocContext, param);

    if (remainingSamplingBudget != 0)
    {
        // fast_alloc_limit_ptr should be in the range [alloc_ptr, alloc_limit].
        if (remainingSamplingBudget > (size_t)(pAllocContext->alloc_limit - pAllocContext->alloc_ptr))
        {
            // the allocation context size has been reduced below the sampling threshold (not sure this is possible)
            // TODO: if remainingSamplingBudget was not 0, it means that an object would have been sampled.
            //       but now that the allocation context is smaller than the sampling limit, what should be done?
            //       maybe set fast_alloc_helper_limit_ptr to alloc_ptr to trigger the sampling at the next allocation
            //       if we don't do this, the distribution statistics will be skewed. Same if we recompute the limit.
        }
        else
        {
            // restore the remaining sampling budget
            pEEAllocContext->fast_alloc_helper_limit_ptr = pAllocContext->alloc_ptr + remainingSamplingBudget;
        }
    }
}

void GCToEEInterface::GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (GCHeapUtilities::UseThreadAllocationContexts())
    {
        Thread * pThread = NULL;
        while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
        {
            InvokeGCAllocCallback(pThread->GetEEAllocContext(), fn, param);
        }
    }
    else
    {
        InvokeGCAllocCallback(&g_global_ee_alloc_context, fn, param);
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
	(&g_profControlBlock)->FinalizeableObjectQueued(isCritical, (ObjectID)objectID);
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

        // When we're walking objects allocated by class, then we don't want to walk the large
        // object heap because then it would count things that may have been around for a while.
        GCHeapUtilities::GetGCHeap()->DiagWalkHeap(&AllocByClassHelper, (void *)&context, 0, false);

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
#ifdef GC_PROFILING
    // We were only doing generation bounds and GC finish callback for non concurrent GCs so
    // I am keeping that behavior to not break profilers. But if BasicGC monitoring is enabled
    // we will do these for all GCs.
    if (!fConcurrent)
    {
        GCProfileWalkHeap(false);
    }

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

void GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
    assert(args != nullptr);
    int stompWBCompleteActions = SWB_PASS;
    bool is_runtime_suspended = args->is_runtime_suspended;

    switch (args->operation)
    {
    case WriteBarrierOp::StompResize:
        // StompResize requires a new card table, a new lowest address, and
        // a new highest address
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);

        // We are sensitive to the order of writes here (more comments on this further in the method)
        // In particular g_card_table must be written before writing the heap bounds.
        // For platforms with weak memory ordering we will issue fences, for x64/x86 we are ok
        // as long as compiler does not reorder these writes.
        // That is unlikely since we have method calls in between.
        // Just to be robust agains possible refactoring/inlining we will do a compiler-fenced store here.
        VolatileStoreWithoutBarrier(&g_card_table, args->card_table);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(args->card_bundle_table != nullptr);
        g_card_bundle_table = args->card_bundle_table;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        if (g_sw_ww_enabled_for_gc_heap && (args->write_watch_table != nullptr))
        {
            assert(args->is_runtime_suspended);
            g_sw_ww_table = args->write_watch_table;
        }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        stompWBCompleteActions |= ::StompWriteBarrierResize(is_runtime_suspended, args->requires_upper_bounds_check);
        is_runtime_suspended = (stompWBCompleteActions & SWB_EE_RESTART) || is_runtime_suspended;

        if (stompWBCompleteActions & SWB_ICACHE_FLUSH)
        {
            // flushing/invalidating the write barrier's body for the current process
            // NOTE: the underlying API may flush more than needed or nothing at all if Icache is coherent.
            ::FlushWriteBarrierInstructionCache();
        }

        // IMPORTANT: managed heap segments may surround unmanaged/stack segments. In such cases adding another managed
        //     heap segment may put a stack/unmanaged write inside the new heap range. However the old card table would
        //     not cover it. Therefore we must ensure that the write barriers see the new table before seeing the new bounds.
        //
        //     On architectures with strong ordering, we only need to prevent compiler reordering.
        //     Otherwise we put a process-wide fence here (so that we could use an ordinary read in the barrier)

#if defined(HOST_ARM64) || defined(HOST_ARM) || defined(HOST_LOONGARCH64) || defined(HOST_RISCV64)
        if (!is_runtime_suspended)
        {
            // If runtime is not suspended, force all threads to see the changed table before seeing updated heap boundaries.
            // See: http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/346765
            FlushProcessWriteBuffers();
        }
#endif

        g_lowest_address = args->lowest_address;
        g_highest_address = args->highest_address;

#if defined(HOST_ARM64) || defined(HOST_ARM) || defined(HOST_LOONGARCH64) || defined(HOST_RISCV64)
        // Need to reupdate for changes to g_highest_address g_lowest_address
        stompWBCompleteActions |= ::StompWriteBarrierResize(is_runtime_suspended, args->requires_upper_bounds_check);

#ifdef HOST_ARM
        if (stompWBCompleteActions & SWB_ICACHE_FLUSH)
        {
            // flushing/invalidating the write barrier's body for the current process
            // NOTE: the underlying API may flush more than needed or nothing at all if Icache is coherent.
            ::FlushWriteBarrierInstructionCache();
        }
#endif
#endif

        // At this point either the old or the new set of globals (card_table, bounds etc) can be used. Card tables and card bundles allow such use.
        // When card tables are de-published (at EE suspension) all the info will be merged, so the information will not be lost.
        // Another point - we should not yet have any managed objects/addresses outside of the former bounds, so either old or new bounds are fine.
        // That is - because bounds can only become wider and we are not yet done with widening.
        //
        // However!!
        // Once we are done, a new object can (and likely will) be allocated outside of the former bounds.
        // So, before such object can be used in a write barier, we must ensure that the barrier also uses the new bounds.
        //
        // This is easy to arrange for architectures with strong memory ordering. We only need to ensure that
        // - object is allocated/published _after_ we publish bounds here
        // - write barrier reads bounds after reading the new object locations
        //
        // for architectures with strong memory ordering (x86/x64) both conditions above are naturally guaranteed.
        // Systems with weak ordering are more interesting. We could either:
        // a) issue a write fence here and pair it with a read fence in the write barrier, or
        // b) issue a process-wide full fence here and do ordinary reads in the barrier.
        //
        // We will do "b" because executing write barrier is by far more common than updating card table.
        //
        // I.E. - for weak architectures we have to do a process-wide fence.
        //
        // NOTE: suspending/resuming EE works the same as process-wide fence for our purposes here.
        //       (we care only about managed threads and suspend/resume will do full fences - good enough for us).
        //

#if defined(HOST_ARM64) || defined(HOST_ARM) || defined(HOST_LOONGARCH64) || defined(HOST_RISCV64)
        is_runtime_suspended = (stompWBCompleteActions & SWB_EE_RESTART) || is_runtime_suspended;
        if (!is_runtime_suspended)
        {
            // If runtime is not suspended, force all threads to see the changed state before observing future allocations.
            FlushProcessWriteBuffers();
        }
#endif

        if (stompWBCompleteActions & SWB_EE_RESTART)
        {
            assert(!args->is_runtime_suspended &&
                "if runtime was suspended in patching routines then it was in running state at beginning");
            ThreadSuspend::RestartEE(FALSE, TRUE);
        }
        return; // unlike other branches we have already done cleanup so bailing out here
    case WriteBarrierOp::StompEphemeral:
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
        // StompEphemeral requires a new ephemeral low and a new ephemeral high
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        g_ephemeral_low = args->ephemeral_low;
        g_ephemeral_high = args->ephemeral_high;
        g_region_to_generation_table = args->region_to_generation_table;
        g_region_shr = args->region_shr;
        g_region_use_bitwise_write_barrier = args->region_use_bitwise_write_barrier;
        stompWBCompleteActions |= ::StompWriteBarrierEphemeral(args->is_runtime_suspended);
        break;
    case WriteBarrierOp::Initialize:
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
        // This operation should only be invoked once, upon initialization.
        assert(g_card_table == nullptr);
        assert(g_lowest_address == nullptr);
        assert(g_highest_address == nullptr);
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        assert(!args->requires_upper_bounds_check && "the ephemeral generation must be at the top of the heap!");

        g_card_table = args->card_table;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(g_card_bundle_table == nullptr);
        g_card_bundle_table = args->card_bundle_table;
#endif

        g_lowest_address = args->lowest_address;
        g_highest_address = args->highest_address;
        g_region_to_generation_table = args->region_to_generation_table;
        g_region_shr = args->region_shr;
        g_region_use_bitwise_write_barrier = args->region_use_bitwise_write_barrier;
        stompWBCompleteActions |= ::StompWriteBarrierResize(true, false);

        // StompWriteBarrierResize does not necessarily bash g_ephemeral_low
        // usages, so we must do so here. This is particularly true on x86,
        // where StompWriteBarrierResize will not bash g_ephemeral_low when
        // called with the parameters (true, false), as it is above.
        g_ephemeral_low = args->ephemeral_low;
        g_ephemeral_high = args->ephemeral_high;
        stompWBCompleteActions |= ::StompWriteBarrierEphemeral(true);
        break;
    case WriteBarrierOp::SwitchToWriteWatch:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
        assert(args->write_watch_table != nullptr);
        g_sw_ww_table = args->write_watch_table;
        g_sw_ww_enabled_for_gc_heap = true;
        stompWBCompleteActions |= ::SwitchToWriteWatchBarrier(true);
#else
        assert(!"should never be called without FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP");
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        break;
    case WriteBarrierOp::SwitchToNonWriteWatch:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
        g_sw_ww_table = 0;
        g_sw_ww_enabled_for_gc_heap = false;
        stompWBCompleteActions |= ::SwitchToNonWriteWatchBarrier(true);
#else
        assert(!"should never be called without FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP");
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        break;
    default:
        assert(!"unknown WriteBarrierOp enum");
    }
    if (stompWBCompleteActions & SWB_ICACHE_FLUSH)
    {
        ::FlushWriteBarrierInstructionCache();
    }
    if (stompWBCompleteActions & SWB_EE_RESTART)
    {
        assert(!args->is_runtime_suspended &&
            "if runtime was suspended in patching routines then it was in running state at beginning");
        ThreadSuspend::RestartEE(FALSE, TRUE);
    }
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
    Thread* Thread;
    bool HasStarted;
    CLREvent ThreadStartedEvent;
};

struct ThreadStubArguments
{
    void* Argument;
    void (*ThreadStart)(void*);
    HANDLE Thread;
    bool HasStarted;
    CLREvent ThreadStartedEvent;
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
        EX_END_CATCH(SwallowAllExceptions)

        if (!args.Thread)
        {
            args.ThreadStartedEvent.CloseEvent();
            return false;
        }

        auto threadStub = [](void* argument) -> DWORD
        {
            SuspendableThreadStubArguments* args = static_cast<SuspendableThreadStubArguments*>(argument);
            assert(args != nullptr);

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
        args.Thread = INVALID_HANDLE_VALUE;
        if (!args.ThreadStartedEvent.CreateAutoEventNoThrow(FALSE))
        {
            return false;
        }

        auto threadStub = [](void* argument) -> DWORD
        {
            ThreadStubArguments* args = static_cast<ThreadStubArguments*>(argument);
            assert(args != nullptr);

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
        if (args.Thread == INVALID_HANDLE_VALUE)
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
    EX_END_CATCH(SwallowAllExceptions)

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

    return SystemDomain::System()->GetTotalNumSizedRefHandles();
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

    // Is the list active?
    GcNotifications gn(g_pGcNotificationTable);
    if (gn.IsActive())
    {
        GcEvtArgs gea = { GC_MARK_END, { (1<<condemnedGeneration) } };
        if (gn.GetNotification(gea) != 0)
        {
            return true;
        }
    }

    return false;
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

    // Is the list active?
    GcNotifications gn(g_pGcNotificationTable);
    if (gn.IsActive())
    {
        GcEvtArgs gea = { GC_MARK_END, { (1<<condemnedGeneration) } };
        if (gn.GetNotification(gea) != 0)
        {
            DACNotify::DoGCNotification(gea);
        }
    }

    if (gcGenAnalysisState == GcGenAnalysisState::Enabled)
    {
#ifndef GEN_ANALYSIS_STRESS
        if (GenAwareMatchingCondition(gcIndex, condemnedGeneration, promoted_bytes, elapsed))
#endif
        {
            if (gcGenAnalysisTrace)
            {
                EventPipeAdapter::ResumeSession(gcGenAnalysisEventPipeSession);
                FireEtwGenAwareBegin((int)gcIndex, GetClrInstanceId());
                s_forcedGCInProgress = true;
                GCProfileWalkHeap(true);
                s_forcedGCInProgress = false;
                reportGenerationBounds();
                FireEtwGenAwareEnd((int)gcIndex, GetClrInstanceId());
                EventPipeAdapter::PauseSession(gcGenAnalysisEventPipeSession);
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
                EX_END_CATCH(SwallowAllExceptions);
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
