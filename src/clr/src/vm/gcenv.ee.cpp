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

    static_assert_no_msg(SUSPEND_FOR_GC == ThreadSuspend::SUSPEND_FOR_GC);
    static_assert_no_msg(SUSPEND_FOR_GC_PREP == ThreadSuspend::SUSPEND_FOR_GC_PREP);

    _ASSERTE(reason == SUSPEND_FOR_GC || reason == SUSPEND_FOR_GC_PREP);

    ThreadSuspend::SuspendEE((ThreadSuspend::SUSPEND_REASON)reason);
}

void GCToEEInterface::RestartEE(bool bFinishedGC)
{
    WRAPPER_NO_CONTRACT;

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
    // Go through all the app domains and for each one detach all the *unmarked* RCWs to prevent
    // the RCW cache from resurrecting them.
    UnsafeAppDomainIterator i(TRUE);
    i.Init();

    while (i.Next())
    {
        i.GetDomain()->DetachRCWs();
    }
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
            GCToEEInterface::GetAllocContext(pThread), sc->thread_number))
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

    // Update AppDomain stage here.
    SystemDomain::System()->ProcessClearingDomains();

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
    _ASSERTE(pWrap != NULL);

    return !!pWrap->IsWrapperActive();
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

gc_alloc_context * GCToEEInterface::GetAllocContext(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    return pThread->GetAllocContext();
}

bool GCToEEInterface::CatchAtSafePoint(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    return !!pThread->CatchAtSafePoint();
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

bool GCToEEInterface::IsPreemptiveGCDisabled(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    return !!pThread->PreemptiveGCDisabled();
}

void GCToEEInterface::EnablePreemptiveGC(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    pThread->EnablePreemptiveGC();
}

void GCToEEInterface::DisablePreemptiveGC(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    pThread->DisablePreemptiveGC();
}

Thread* GCToEEInterface::GetThread()
{
    WRAPPER_NO_CONTRACT;

    return ::GetThread();
}

bool GCToEEInterface::TrapReturningThreads()
{
    WRAPPER_NO_CONTRACT;
    return !!g_TrapReturningThreads;
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

Thread* GCToEEInterface::CreateBackgroundThread(GCBackgroundThreadFunction threadStart, void* arg)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    BackgroundThreadStubArgs threadStubArgs;

    threadStubArgs.arg = arg;
    threadStubArgs.thread = NULL;
    threadStubArgs.threadStart = threadStart;
    threadStubArgs.hasStarted = false;

    if (!threadStubArgs.threadStartedEvent.CreateAutoEventNoThrow(FALSE))
    {
        return NULL;
    }

    EX_TRY
    {
        threadStubArgs.thread = SetupUnstartedThread(FALSE);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (threadStubArgs.thread == NULL)
    {
        threadStubArgs.threadStartedEvent.CloseEvent();
        return NULL;
    }

    if (threadStubArgs.thread->CreateNewThread(0, (LPTHREAD_START_ROUTINE)BackgroundThreadStub, &threadStubArgs, W("Background GC")))
    {
        threadStubArgs.thread->SetBackground (TRUE, FALSE);
        threadStubArgs.thread->StartThread();

        // Wait for the thread to be in its main loop
        uint32_t res = threadStubArgs.threadStartedEvent.Wait(INFINITE, FALSE);
        threadStubArgs.threadStartedEvent.CloseEvent();
        _ASSERTE(res == WAIT_OBJECT_0);

        if (!threadStubArgs.hasStarted)
        {
            // The thread has failed to start and the Thread object was destroyed in the Thread::HasStarted
            // failure code path.
            return NULL;
        }

        return threadStubArgs.thread;
    }

    // Destroy the Thread object
    threadStubArgs.thread->DecExternalCount(FALSE);
    return NULL;
}

//
// Diagnostics code
//

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
inline BOOL ShouldTrackMovementForProfilerOrEtw()
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
    if (CORProfilerTrackGC())
        UpdateGenerationBounds();
#endif // GC_PROFILING
}

void GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
#ifdef GC_PROFILING
    if (!fConcurrent)
    {
        GCProfileWalkHeap();
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

void GCToEEInterface::DiagWalkSurvivors(void* gcContext)
{
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    if (ShouldTrackMovementForProfilerOrEtw())
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
    if (ShouldTrackMovementForProfilerOrEtw())
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
    if (ShouldTrackMovementForProfilerOrEtw())
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

#if defined(_ARM64_)
        // Need to reupdate for changes to g_highest_address g_lowest_address
        bool is_runtime_suspended = (stompWBCompleteActions & SWB_EE_RESTART) || args->is_runtime_suspended;
        stompWBCompleteActions |= ::StompWriteBarrierResize(is_runtime_suspended, args->requires_upper_bounds_check);

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

bool GCToEEInterface::ShouldFinalizeObjectForUnload(AppDomain* pDomain, Object* obj)
{
    // CoreCLR does not have appdomains, so this code path is dead. Other runtimes may
    // choose to inspect the object being finalized here.
    // [DESKTOP TODO] Desktop looks for "agile and finalizable" objects and may choose
    // to move them to a new app domain instead of finalizing them here.
    return true;
}

bool GCToEEInterface::ForceFullGCToBeBlocking()
{
    // In theory, there is nothing fundamental that requires an AppDomain unload to induce
    // a blocking GC. In the past, this workaround was done to fix an Stress AV, but the root
    // cause of the AV was never discovered and this workaround remains in place.
    //
    // It would be nice if this were not necessary. However, it's not clear if the aformentioned
    // stress bug is still lurking and will return if this workaround is removed. We should
    // do some experiments: remove this workaround and see if the stress bug still repros.
    // If so, we should find the root cause instead of relying on this.
    return !!SystemDomain::System()->RequireAppDomainCleanup();
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

// These are arbitrary, we shouldn't ever be having confrig keys or values
// longer than these lengths.
const size_t MaxConfigKeyLength = 255;
const size_t MaxConfigValueLength = 255;

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

    WCHAR configKey[MaxConfigKeyLength];
    if (MultiByteToWideChar(CP_ACP, 0, key, -1 /* key is null-terminated */, configKey, MaxConfigKeyLength) == 0)
    {
        // whatever this is... it's not something we care about. (It was too long, wasn't unicode, etc.)
        return false;
    }

    if (CLRConfig::IsConfigOptionSpecified(configKey))
    {
        CLRConfig::ConfigDWORDInfo info { configKey , 0, CLRConfig::EEConfig_default };
        *value = CLRConfig::GetConfigValue(info);
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

    // not allocated on the stack since it escapes this function
    AStringHolder configResult = new (nothrow) char[MaxConfigValueLength];
    if (!configResult)
    {
        CLRConfig::FreeConfigString(out);
        return false;
    }

    if (WideCharToMultiByte(CP_ACP, 0, out, -1 /* out is null-terminated */, 
          configResult.GetValue(), MaxConfigKeyLength, nullptr, nullptr) == 0)
    {
        // this should only happen if the config subsystem gives us a string that's not valid
        // unicode.
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
