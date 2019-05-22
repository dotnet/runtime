// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * GCENV.EE.CPP
 *
 * GCToEEInterface implementation
 *

 *
 */

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
                GetThread() == NULL ||
                // this is for background GC threads which always call this when EE is suspended.
                IsGCSpecialThread() ||
                (GetThread() == ThreadSuspend::GetSuspensionThread() && ThreadStore::HoldingThreadStore()));

    pThread->SetHasPromotedBytes();

    Frame* pTopFrame = pThread->GetFrame();
    Object ** topStack = (Object **)pTopFrame;
    if ((pTopFrame != ((Frame*)-1))
        && (pTopFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr())) {
        // It is an InlinedCallFrame. Get SP from it.
        InlinedCallFrame* pInlinedFrame = (InlinedCallFrame*)pTopFrame;
        topStack = (Object **)pInlinedFrame->GetCallSiteSP();
    }

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
#if defined(WIN64EXCEPTIONS)
        flagsStackWalk |= GC_FUNCLET_REFERENCE_REPORTING;
#endif // defined(WIN64EXCEPTIONS)
        pThread->StackWalkFrames( GcStackCrawlCallBack, &gcctx, flagsStackWalk);
    }
}

void GCToEEInterface::GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    STRESS_LOG1(LF_GCROOTS, LL_INFO10, "GCScan: Promotion Phase = %d\n", sc->promotion);

    // In server GC, we should be competing for marking the statics
    if (GCHeapUtilities::MarkShouldCompeteForStatics())
    {
        if (condemned == max_gen && sc->promotion)
        {
            SystemDomain::EnumAllStaticGCRefs(fn, sc);
        }
    }

    Thread* pThread = NULL;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        STRESS_LOG2(LF_GC | LF_GCROOTS, LL_INFO100, "{ Starting scan of Thread %p ID = %x\n", pThread, pThread->GetThreadId());

        if (GCHeapUtilities::GetGCHeap()->IsThreadUsingAllocationContextHeap(
            pThread->GetAllocContext(), sc->thread_number))
        {
            sc->thread_under_crawl = pThread;
#ifdef FEATURE_EVENT_TRACE
            sc->dwEtwRootKind = kEtwGCRootKindStack;
#endif // FEATURE_EVENT_TRACE
            ScanStackRoots(pThread, fn, sc);
#ifdef FEATURE_EVENT_TRACE
            sc->dwEtwRootKind = kEtwGCRootKindOther;
#endif // FEATURE_EVENT_TRACE
        }
        STRESS_LOG2(LF_GC | LF_GCROOTS, LL_INFO100, "Ending scan of Thread %p ID = 0x%x }\n", pThread, pThread->GetThreadId());
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

#ifdef FEATURE_COMINTEROP
    //
    // Let GC detect managed/native cycles with input from jupiter
    // Jupiter will
    // 1. Report reference from RCW to CCW based on native reference in Jupiter
    // 2. Identify the subset of CCWs that needs to be rooted
    //
    // We'll build the references from RCW to CCW using
    // 1. Preallocated arrays
    // 2. Dependent handles
    //
    RCWWalker::OnGCStarted(condemned);
#endif // FEATURE_COMINTEROP

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

#ifdef FEATURE_COMINTEROP
    //
    // Tell Jupiter GC has finished
    //
    RCWWalker::OnGCFinished(condemned);
#endif // FEATURE_COMINTEROP
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

    return pWrap != NULL && pWrap->IsWrapperActive();
#else
    return false;
#endif
}

void GCToEEInterface::GcBeforeBGCSweepWork()
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

    Thread* pThread = ::GetThread();
    if (!pThread)
    {
        return nullptr;
    }

    return pThread->GetAllocContext();
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
            fn(pThread->GetAllocContext(), param);
        }
    }
    else
    {
        fn(&g_global_alloc_context, param);
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

    Thread* pThread = ::GetThread();
    if (pThread)
    {
        return !!pThread->PreemptiveGCDisabled();
    }

    return false;
}

bool GCToEEInterface::EnablePreemptiveGC()
{
    WRAPPER_NO_CONTRACT;

    bool bToggleGC = false;
    Thread* pThread = ::GetThread();

    if (pThread)
    {
        bToggleGC = !!pThread->PreemptiveGCDisabled();
        if (bToggleGC)
        {
            pThread->EnablePreemptiveGC();
        }
    }

    return bToggleGC;
}

void GCToEEInterface::DisablePreemptiveGC()
{
    WRAPPER_NO_CONTRACT;

    Thread* pThread = ::GetThread();
    if (pThread)
    {
        pThread->DisablePreemptiveGC();
    }
}

Thread* GCToEEInterface::GetThread()
{
    WRAPPER_NO_CONTRACT;

    return ::GetThread();
}

struct BackgroundThreadStubArgs
{
    Thread* thread;
    GCBackgroundThreadFunction threadStart;
    void* arg;
    CLREvent threadStartedEvent;
    bool hasStarted;
};

DWORD WINAPI BackgroundThreadStub(void* arg)
{
    BackgroundThreadStubArgs* stubArgs = (BackgroundThreadStubArgs*)arg;
    assert (stubArgs->thread != NULL);

    ClrFlsSetThreadType (ThreadType_GC);
    stubArgs->thread->SetGCSpecial(true);
    STRESS_LOG_RESERVE_MEM (GC_STRESSLOG_MULTIPLY);

    stubArgs->hasStarted = !!stubArgs->thread->HasStarted(FALSE);

    Thread* thread = stubArgs->thread;
    GCBackgroundThreadFunction realThreadStart = stubArgs->threadStart;
    void* realThreadArg = stubArgs->arg;
    bool hasStarted = stubArgs->hasStarted;

    stubArgs->threadStartedEvent.Set();
    // The stubArgs cannot be used once the event is set, since that releases wait on the
    // event in the function that created this thread and the stubArgs go out of scope.

    DWORD result = 0;

    if (hasStarted)
    {
        result = realThreadStart(realThreadArg);
        DestroyThread(thread);
    }

    return result;
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
    g_profControlBlock.pProfInterface->EndConditionalWeakTableElementReferences(heapId);
#else
    UNREFERENCED_PARAMETER(heapId);
#endif // defined (GC_PROFILING)
}

// If GC profiling is enabled, informs the profiler that we are done
// tracing root references.
void ProfilerEndRootReferences2(void* heapId)
{
#if defined (GC_PROFILING)
    g_profControlBlock.pProfInterface->EndRootReferences2(heapId);
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
            BEGIN_PIN_PROFILER(CORProfilerTrackGC());
            g_profControlBlock.pProfInterface->RootReference2(
                (uint8_t *)*pRef,
                kEtwGCRootKindHandle,
                (EtwGCRootFlags)flags,
                pRef,
                &pSC->pHeapId);
            END_PIN_PROFILER();
        }
        else
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackConditionalWeakTableElements());
            g_profControlBlock.pProfInterface->ConditionalWeakTableElementReference(
                (uint8_t*)*pRef,
                (uint8_t*)pSec,
                pRef,
                &pSC->pHeapId);
            END_PIN_PROFILER();
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

void GCProfileWalkHeap()
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
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        GCProfileWalkHeapWorker(TRUE /* fProfilerPinned */, fShouldWalkHeapRootsForEtw, fShouldWalkHeapObjectsForEtw);
        fWalkedHeapForProfiler = TRUE;
        END_PIN_PROFILER();
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
	g_profControlBlock.pProfInterface->FinalizeableObjectQueued(isCritical, (ObjectID)objectID);
}

static fq_walk_fn g_FQWalkFn = &WalkFReachableObjects;

void GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
#ifdef GC_PROFILING
    DiagUpdateGenerationBounds();
    GarbageCollectionStartedCallback(gen, isInduced);
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        size_t context = 0;

        // When we're walking objects allocated by class, then we don't want to walk the large
        // object heap because then it would count things that may have been around for a while.
        GCHeapUtilities::GetGCHeap()->DiagWalkHeap(&AllocByClassHelper, (void *)&context, 0, false);

        // Notify that we've reached the end of the Gen 0 scan
        g_profControlBlock.pProfInterface->EndAllocByClass(&context);
        END_PIN_PROFILER();
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
        GCProfileWalkHeap();
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
    if (CORProfilerTrackGC())
    {
        BEGIN_PIN_PROFILER(CORProfilerPresent());
        GCHeapUtilities::GetGCHeap()->DiagWalkFinalizeQueue(gcContext, g_FQWalkFn);
        END_PIN_PROFILER();
    }
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

void GCToEEInterface::DiagWalkLOHSurvivors(void* gcContext)
{
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    if (ShouldTrackSurvivorsForProfilerOrEtw())
    {
        size_t context = 0;
        ETW::GCLog::BeginMovedReferences(&context);
        GCHeapUtilities::GetGCHeap()->DiagWalkSurvivorsWithType(gcContext, &WalkMovedReferences, (void*)context, walk_for_loh);
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
    int stompWBCompleteActions = SWB_PASS;
    bool is_runtime_suspended = false;

    assert(args != nullptr);
    switch (args->operation)
    {
    case WriteBarrierOp::StompResize:
        // StompResize requires a new card table, a new lowest address, and
        // a new highest address
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);

        g_card_table = args->card_table;

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

        stompWBCompleteActions |= ::StompWriteBarrierResize(args->is_runtime_suspended, args->requires_upper_bounds_check);

        // We need to make sure that other threads executing checked write barriers
        // will see the g_card_table update before g_lowest/highest_address updates.
        // Otherwise, the checked write barrier may AV accessing the old card table
        // with address that it does not cover.
        //
        // Even x86's total store ordering is insufficient here because threads reading
        // g_card_table do so via the instruction cache, whereas g_lowest/highest_address
        // are read via the data cache.
        //
        // The g_card_table update is covered by section 8.1.3 of the Intel Software
        // Development Manual, Volume 3A (System Programming Guide, Part 1), about
        // "cross-modifying code": We need all _executing_ threads to invalidate
        // their instruction cache, which FlushProcessWriteBuffers achieves by sending
        // an IPI (inter-process interrupt).

        if (stompWBCompleteActions & SWB_ICACHE_FLUSH)
        {
            // flushing icache on current processor (thread)
            ::FlushWriteBarrierInstructionCache();
            // asking other processors (threads) to invalidate their icache
            FlushProcessWriteBuffers();
        }

        g_lowest_address = args->lowest_address;
        VolatileStore(&g_highest_address, args->highest_address);

#if defined(_ARM64_) || defined(_ARM_)
        // Need to reupdate for changes to g_highest_address g_lowest_address
        is_runtime_suspended = (stompWBCompleteActions & SWB_EE_RESTART) || args->is_runtime_suspended;
        stompWBCompleteActions |= ::StompWriteBarrierResize(is_runtime_suspended, args->requires_upper_bounds_check);

#ifdef _ARM_
        if (stompWBCompleteActions & SWB_ICACHE_FLUSH)
        {
            ::FlushWriteBarrierInstructionCache();
        }
#endif

        is_runtime_suspended = (stompWBCompleteActions & SWB_EE_RESTART) || args->is_runtime_suspended;
        if(!is_runtime_suspended)
        {
            // If runtime is not suspended, force updated state to be visible to all threads
            MemoryBarrier();
        }
#endif
        if (stompWBCompleteActions & SWB_EE_RESTART)
        {
            assert(!args->is_runtime_suspended &&
                "if runtime was suspended in patching routines then it was in running state at begining");
            ThreadSuspend::RestartEE(FALSE, TRUE);
        }
        return; // unlike other branches we have already done cleanup so bailing out here
    case WriteBarrierOp::StompEphemeral:
        // StompEphemeral requires a new ephemeral low and a new ephemeral high
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        g_ephemeral_low = args->ephemeral_low;
        g_ephemeral_high = args->ephemeral_high;
        stompWBCompleteActions |= ::StompWriteBarrierEphemeral(args->is_runtime_suspended);
        break;
    case WriteBarrierOp::Initialize:
        // This operation should only be invoked once, upon initialization.
        assert(g_card_table == nullptr);
        assert(g_lowest_address == nullptr);
        assert(g_highest_address == nullptr);
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
        assert(!args->requires_upper_bounds_check && "the ephemeral generation must be at the top of the heap!");

        g_card_table = args->card_table;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(g_card_bundle_table == nullptr);
        g_card_bundle_table = args->card_bundle_table;
#endif

        g_lowest_address = args->lowest_address;
        g_highest_address = args->highest_address;
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
        assert(args->write_watch_table != nullptr);
        assert(args->is_runtime_suspended && "the runtime must be suspended here!");
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
            "if runtime was suspended in patching routines then it was in running state at begining");
        ThreadSuspend::RestartEE(FALSE, TRUE);
    }
}

void GCToEEInterface::EnableFinalization(bool foundFinalizers)
{
    if (foundFinalizers || FinalizerThread::HaveExtraWorkForFinalizer())
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
    if (pMT == pWeakReferenceMT ||
        pMT->GetCanonicalMethodTable() == pWeakReferenceOfTCanonMT)
    {
        FinalizeWeakReference(obj);
        return true;
    }

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

bool GCToEEInterface::GetBooleanConfigValue(const char* key, bool* value)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // these configuration values are given to us via startup flags.
    if (strcmp(key, "gcServer") == 0)
    {
        *value = g_heap_type == GC_HEAP_SVR;
        return true;
    }

    if (strcmp(key, "gcConcurrent") == 0)
    {
        *value = !!g_pConfig->GetGCconcurrent();
        return true;
    }

    if (strcmp(key, "GCRetainVM") == 0)
    {
        *value = !!g_pConfig->GetGCRetainVM();
        return true;
    }

    WCHAR configKey[MaxConfigKeyLength];
    if (MultiByteToWideChar(CP_ACP, 0, key, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
    {
        // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
        return false;
    }

    // otherwise, ask the config subsystem.
    if (CLRConfig::IsConfigOptionSpecified(configKey))
    {
        CLRConfig::ConfigDWORDInfo info { configKey , 0, CLRConfig::EEConfig_default };
        *value = CLRConfig::GetConfigValue(info) != 0;
        return true;
    }

    return false;
}

bool GCToEEInterface::GetIntConfigValue(const char* key, int64_t* value)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    if (strcmp(key, "GCSegmentSize") == 0)
    {
        *value = g_pConfig->GetSegmentSize();
        return true;
    }

    if (strcmp(key, "GCgen0size") == 0)
    {
        *value = g_pConfig->GetGCgen0size();
        return true;
    }

    if (strcmp(key, "GCLOHThreshold") == 0)
    {
        *value = g_pConfig->GetGCLOHThreshold();
        return true;
    }

    WCHAR configKey[MaxConfigKeyLength];
    if (MultiByteToWideChar(CP_ACP, 0, key, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
    {
        // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
        return false;
    }

    // There is no ConfigULONGLONGInfo, and the GC uses 64 bit values for things like GCHeapAffinitizeMask, 
    // so have to fake it with getting the string and converting to uint64_t
    if (CLRConfig::IsConfigOptionSpecified(configKey))
    {
        CLRConfig::ConfigStringInfo info { configKey, CLRConfig::EEConfig_default };
        LPWSTR out = CLRConfig::GetConfigValue(info);
        if (!out)
        {
            // config not found
            CLRConfig::FreeConfigString(out);
            return false;
        }

        wchar_t *end;
        uint64_t result;
        errno = 0;
        result = _wcstoui64(out, &end, 16);
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

    return false;
}

bool GCToEEInterface::GetStringConfigValue(const char* key, const char** value)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    WCHAR configKey[MaxConfigKeyLength];
    if (MultiByteToWideChar(CP_ACP, 0, key, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
    {
        // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
        return false;
    }

    CLRConfig::ConfigStringInfo info { configKey, CLRConfig::EEConfig_default };
    LPWSTR out = CLRConfig::GetConfigValue(info);
    if (!out)
    {
        // config not found
        return false;
    }

    int charCount = WideCharToMultiByte(CP_ACP, 0, out, -1 /* out is null-terminated */, NULL, 0, nullptr, nullptr);
    if (charCount == 0)
    {
        // this should only happen if the config subsystem gives us a string that's not valid
        // unicode.
        CLRConfig::FreeConfigString(out);
        return false;
    }

    // not allocated on the stack since it escapes this function
    AStringHolder configResult = new (nothrow) char[charCount];
    if (!configResult)
    {
        CLRConfig::FreeConfigString(out);
        return false;
    }

    if (WideCharToMultiByte(CP_ACP, 0, out, -1 /* out is null-terminated */,
          configResult.GetValue(), charCount, nullptr, nullptr) == 0)
    {
        // this should never happen, the previous call to WideCharToMultiByte that computed the charCount should 
        // have caught all issues.
        assert(false);
        CLRConfig::FreeConfigString(out);
        return false;
    }

    *value = configResult.Extract();
    CLRConfig::FreeConfigString(out);
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
        const wchar_t* name)
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
            args.Thread = SetupUnstartedThread(FALSE);
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
            args->Thread->SetGCSpecial(true);
            STRESS_LOG_RESERVE_MEM(GC_STRESSLOG_MULTIPLY);
            args->HasStarted = !!args->Thread->HasStarted(false);

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

        args.Thread->SetBackground(TRUE, FALSE);
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
        const wchar_t* name)
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
    if (object->GetGCSafeMethodTable() != g_pOverlappedDataClass)
    {
        // not an overlapped data object - nothing to do.
        return;
    }

    // reporting the pinned user objects
    OverlappedDataObject *pOverlapped = (OverlappedDataObject *)object;
    if (pOverlapped->m_userObject != NULL)
    {
        if (pOverlapped->m_userObject->GetGCSafeMethodTable() == g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT]->GetMethodTable())
        {
            // OverlappedDataObject is very special.  An async pin handle keeps it alive.
            // During GC, we also make sure
            // 1. m_userObject itself does not move if m_userObject is not array
            // 2. Every object pointed by m_userObject does not move if m_userObject is array
            // We do not want to pin m_userObject if it is array.
            ArrayBase* pUserObject = (ArrayBase*)OBJECTREFToObject(pOverlapped->m_userObject);
            Object **ppObj = (Object**)pUserObject->GetDataPtr(TRUE);
            size_t num = pUserObject->GetNumComponents();
            for (size_t i = 0; i < num; i++)
            {
                callback(ppObj + i, sc, GC_CALL_PINNED);
            }
        }
        else
        {
            callback(&OBJECTREF_TO_UNCHECKED_OBJECTREF(pOverlapped->m_userObject), (ScanContext *)sc, GC_CALL_PINNED);
        }
    }
}

void GCToEEInterface::WalkAsyncPinned(Object* object, void* context, void (*callback)(Object*, Object*, void*))
{
    LIMITED_METHOD_CONTRACT;

    assert(object != nullptr);
    assert(callback != nullptr);

    if (object->GetGCSafeMethodTable() != g_pOverlappedDataClass)
    {
        return;
    }

    OverlappedDataObject *pOverlapped = (OverlappedDataObject *)(object);
    if (pOverlapped->m_userObject != NULL)
    {
        Object * pUserObject = OBJECTREFToObject(pOverlapped->m_userObject);
        callback(object, pUserObject, context);
        if (pOverlapped->m_userObject->GetGCSafeMethodTable() == g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT]->GetMethodTable())
        {
            ArrayBase* pUserArrayObject = (ArrayBase*)pUserObject;
            Object **pObj = (Object**)pUserArrayObject->GetDataPtr(TRUE);
            size_t num = pUserArrayObject->GetNumComponents();
            for (size_t i = 0; i < num; i ++)
            {
                callback(pUserObject, pObj[i], context);
            }
        }
    }
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


bool GCToEEInterface::AnalyzeSurvivorsRequested(int condemnedGeneration)
{
    LIMITED_METHOD_CONTRACT;

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

void GCToEEInterface::AnalyzeSurvivorsFinished(int condemnedGeneration)
{
    LIMITED_METHOD_CONTRACT;

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
    // Refer to the comments in src/vm/gcenv.ee.h next to the EXTERN C definitions to see which events are enabled.

    // WARNING: To change an event's GC level, perfcollect script needs to be updated simultaneously to reflect it.
    BOOL keyword_gc_verbose = EventXplatEnabledGCJoin_V2() || EventPipeEventEnabledGCJoin_V2();
    BOOL keyword_gc_informational = EventXplatEnabledGCStart() || EventPipeEventEnabledGCStart();

    BOOL keyword_gc_heapsurvival_and_movement_informational = EventXplatEnabledGCGenerationRange() || EventPipeEventEnabledGCGenerationRange();
    BOOL keyword_gchandle_informational = EventXplatEnabledSetGCHandle() || EventPipeEventEnabledSetGCHandle();
    BOOL keyword_gchandle_prv_informational = EventXplatEnabledPrvSetGCHandle() || EventPipeEventEnabledPrvSetGCHandle();

    BOOL prv_gcprv_informational = EventXplatEnabledBGCBegin() || EventPipeEventEnabledBGCBegin();
    BOOL prv_gcprv_verbose = EventXplatEnabledPinPlugAtGCTime() || EventPipeEventEnabledPinPlugAtGCTime();

    int publicProviderLevel = keyword_gc_verbose ? GCEventLevel_Verbose : (keyword_gc_informational ? GCEventLevel_Information : GCEventLevel_None);
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
