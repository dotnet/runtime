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

#include "common.h"

#include "gcenv.h"

#include "threadsuspend.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "rcwwalker.h"
#include "comcallablewrapper.h"
#endif // FEATURE_COMINTEROP

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

/*
 * GcEnumObject()
 *
 * This is the JIT compiler (or any remote code manager)
 * GC enumeration callback
 */

void GcEnumObject(LPVOID pData, OBJECTREF *pObj, uint32_t flags)
{
    Object ** ppObj = (Object **)pObj;
    GCCONTEXT   * pCtx  = (GCCONTEXT *) pData;

    // Since we may be asynchronously walking another thread's stack,
    // check (frequently) for stack-buffer-overrun corruptions after 
    // any long operation
    if (pCtx->cf != NULL)
        pCtx->cf->CheckGSCookies();

    //
    // Sanity check that the flags contain only these three values
    //
    assert((flags & ~(GC_CALL_INTERIOR|GC_CALL_PINNED|GC_CALL_CHECK_APP_DOMAIN)) == 0);

    // for interior pointers, we optimize the case in which
    //  it points into the current threads stack area
    //
    if (flags & GC_CALL_INTERIOR)
        PromoteCarefully(pCtx->f, ppObj, pCtx->sc, flags);
    else
        (pCtx->f)(ppObj, pCtx->sc, flags);
}

//-----------------------------------------------------------------------------
void GcReportLoaderAllocator(promote_func* fn, ScanContext* sc, LoaderAllocator *pLoaderAllocator)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (pLoaderAllocator != NULL && pLoaderAllocator->IsCollectible())
    {
        Object *refCollectionObject = OBJECTREFToObject(pLoaderAllocator->GetExposedObject());
        
#ifdef _DEBUG
        Object *oldObj = refCollectionObject;
#endif

        _ASSERTE(refCollectionObject != NULL);
        fn(&refCollectionObject, sc, CHECK_APP_DOMAIN);
        
        // We are reporting the location of a local variable, assert it doesn't change.
        _ASSERTE(oldObj == refCollectionObject);
    }
}

//-----------------------------------------------------------------------------
// Determine whether we should report the generic parameter context
// 
// This is meant to detect the situation where a ThreadAbortException is raised
// in the prolog of a managed method, before the location for the generics 
// context has been initialized; when such a TAE is raised, we are open to a
// race with the GC (e.g. while creating the managed object for the TAE).
// The GC would cause a stack walk, and if we report the stack location for
// the generic param context at this time we'd crash.
// The long term solution is to avoid raising TAEs in any non-GC safe points, 
// and to additionally ensure that we do not expose the runtime to TAE 
// starvation.
inline bool SafeToReportGenericParamContext(CrawlFrame* pCF)
{
    LIMITED_METHOD_CONTRACT;
    if (!pCF->IsFrameless() || !(pCF->IsActiveFrame() || pCF->IsInterrupted()))
    {
        return true;
    }

#ifndef USE_GC_INFO_DECODER

    ICodeManager * pEECM = pCF->GetCodeManager();
    if (pEECM != NULL && pEECM->IsInPrologOrEpilog(pCF->GetRelOffset(), pCF->GetGCInfo(), NULL))
    {
        return false;
    }

#else  // USE_GC_INFO_DECODER

    GcInfoDecoder gcInfoDecoder(pCF->GetGCInfoToken(),
            DECODE_PROLOG_LENGTH);
    UINT32 prologLength = gcInfoDecoder.GetPrologSize();
    if (pCF->GetRelOffset() < prologLength)
    {
        return false;
    }

#endif // USE_GC_INFO_DECODER

    return true;
}

#if defined(WIN64EXCEPTIONS)

struct FindFirstInterruptiblePointState
{
    unsigned offs;
    unsigned endOffs;
    unsigned returnOffs;
};

bool FindFirstInterruptiblePointStateCB(
        UINT32 startOffset,
        UINT32 stopOffset,
        LPVOID hCallback)
{
    FindFirstInterruptiblePointState* pState = (FindFirstInterruptiblePointState*)hCallback;

    _ASSERTE(startOffset < stopOffset);
    _ASSERTE(pState->offs < pState->endOffs);

    if (stopOffset <= pState->offs)
    {
        // The range ends before the requested offset.
        return false;
    }

    // The offset is in the range.
    if (startOffset <= pState->offs &&
                       pState->offs < stopOffset)
    {
        pState->returnOffs = pState->offs;
        return true;
    }

    // The range is completely after the desired offset. We use the range start offset, if
    // it comes before the given endOffs. We assume that the callback is called with ranges
    // in increasing order, so earlier ones are reported before later ones. That is, if we
    // get to this case, it will be the closest interruptible range after the requested
    // offset.

    _ASSERTE(pState->offs < startOffset);
    if (startOffset < pState->endOffs)
    {
        pState->returnOffs = startOffset;
        return true;
    }

    return false;
}

// Find the first interruptible point in the range [offs .. endOffs) (the beginning of the range is inclusive,
// the end is exclusive). Return -1 if no such point exists.
unsigned FindFirstInterruptiblePoint(CrawlFrame* pCF, unsigned offs, unsigned endOffs)
{
    GCInfoToken gcInfoToken = pCF->GetGCInfoToken();
    GcInfoDecoder gcInfoDecoder(gcInfoToken, DECODE_FOR_RANGES_CALLBACK);

    FindFirstInterruptiblePointState state;
    state.offs = offs;
    state.endOffs = endOffs;
    state.returnOffs = -1;

    gcInfoDecoder.EnumerateInterruptibleRanges(&FindFirstInterruptiblePointStateCB, &state);

    return state.returnOffs;
}

#endif // WIN64EXCEPTIONS

//-----------------------------------------------------------------------------
StackWalkAction GcStackCrawlCallBack(CrawlFrame* pCF, VOID* pData)
{
    //
    // KEEP IN SYNC WITH DacStackReferenceWalker::Callback in debug\daccess\daccess.cpp
    //

    Frame       *pFrame;
    GCCONTEXT   *gcctx = (GCCONTEXT*) pData;

#if CHECK_APP_DOMAIN_LEAKS
    gcctx->sc->pCurrentDomain = pCF->GetAppDomain();
#endif //CHECK_APP_DOMAIN_LEAKS

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    if (g_fEnableARM)
    {
        gcctx->sc->pCurrentDomain = pCF->GetAppDomain();
    }
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

    MethodDesc *pMD = pCF->GetFunction();

#ifdef GC_PROFILING
    gcctx->sc->pMD = pMD;
#endif //GC_PROFILING

    // Clear it on exit so that we never have a stale CrawlFrame
    ResetPointerHolder<CrawlFrame*> rph(&gcctx->cf);
    // put it somewhere so that GcEnumObject can get to it.
    gcctx->cf = pCF;

    bool fReportGCReferences = true;
#if defined(WIN64EXCEPTIONS)
    // We may have unwound this crawlFrame and thus, shouldn't report the invalid
    // references it may contain.
    fReportGCReferences = pCF->ShouldCrawlframeReportGCReferences();
#endif // defined(WIN64EXCEPTIONS)

    if (fReportGCReferences)
    {
        if (pCF->IsFrameless())
        {
            ICodeManager * pCM = pCF->GetCodeManager();
            _ASSERTE(pCM != NULL);

            unsigned flags = pCF->GetCodeManagerFlags();
        
    #ifdef _TARGET_X86_
            STRESS_LOG3(LF_GCROOTS, LL_INFO1000, "Scanning Frameless method %pM EIP = %p &EIP = %p\n", 
                pMD, GetControlPC(pCF->GetRegisterSet()), pCF->GetRegisterSet()->PCTAddr);
    #else
            STRESS_LOG2(LF_GCROOTS, LL_INFO1000, "Scanning Frameless method %pM ControlPC = %p\n", 
                pMD, GetControlPC(pCF->GetRegisterSet()));
    #endif

            _ASSERTE(pMD != 0);

    #ifdef _DEBUG
            LOG((LF_GCROOTS, LL_INFO1000, "Scanning Frame for method %s:%s\n",
                    pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));
    #endif // _DEBUG

            DWORD relOffsetOverride = NO_OVERRIDE_OFFSET;
#if defined(WIN64EXCEPTIONS)
            if (pCF->ShouldParentToFuncletUseUnwindTargetLocationForGCReporting())
            {
                GCInfoToken gcInfoToken = pCF->GetGCInfoToken();
                GcInfoDecoder _gcInfoDecoder(
                                    gcInfoToken,
                                    DECODE_CODE_LENGTH
                                    );
                
                if(_gcInfoDecoder.WantsReportOnlyLeaf())
                {
                    // We're in a special case of unwinding from a funclet, and resuming execution in
                    // another catch funclet associated with same parent function. We need to report roots. 
                    // Reporting at the original throw site gives incorrect liveness information. We choose to
                    // report the liveness information at the first interruptible instruction of the catch funclet 
                    // that we are going to execute. We also only report stack slots, since no registers can be
                    // live at the first instruction of a handler, except the catch object, which the VM protects 
                    // specially. If the catch funclet has not interruptible point, we fall back and just report 
                    // what we used to: at the original throw instruction. This might lead to bad GC behavior 
                    // if the liveness is not correct.
                    const EE_ILEXCEPTION_CLAUSE& ehClauseForCatch = pCF->GetEHClauseForCatch();
                    relOffsetOverride = FindFirstInterruptiblePoint(pCF, ehClauseForCatch.HandlerStartPC,
                                                                    ehClauseForCatch.HandlerEndPC);
                    _ASSERTE(relOffsetOverride != NO_OVERRIDE_OFFSET);

                    STRESS_LOG3(LF_GCROOTS, LL_INFO1000, "Setting override offset = %u for method %pM ControlPC = %p\n", 
                        relOffsetOverride, pMD, GetControlPC(pCF->GetRegisterSet()));
                }

            }
#endif // WIN64EXCEPTIONS

            pCM->EnumGcRefs(pCF->GetRegisterSet(),
                            pCF->GetCodeInfo(),
                            flags,
                            GcEnumObject,
                            pData,
                            relOffsetOverride);

        }
        else
        {
            Frame * pFrame = pCF->GetFrame();

            STRESS_LOG3(LF_GCROOTS, LL_INFO1000, 
                "Scanning ExplicitFrame %p AssocMethod = %pM frameVTable = %pV\n", 
                pFrame, pFrame->GetFunction(), *((void**) pFrame));
            pFrame->GcScanRoots( gcctx->f, gcctx->sc);
        }
    }


    // If we're executing a LCG dynamic method then we must promote the associated resolver to ensure it
    // doesn't get collected and yank the method code out from under us).

    // Be careful to only promote the reference -- we can also be called to relocate the reference and 
    // that can lead to all sorts of problems since we could be racing for the relocation with the long
    // weak handle we recover the reference from. Promoting the reference is enough, the handle in the
    // reference will be relocated properly as long as we keep it alive till the end of the collection
    // as long as the reference is actually maintained by the long weak handle.
    if (pMD && gcctx->sc->promotion)
    {
        BOOL fMaybeCollectibleMethod = TRUE;

        // If this is a frameless method then the jitmanager can answer the question of whether
        // or not this is LCG simply by looking at the heap where the code lives, however there
        // is also the prestub case where we need to explicitly look at the MD for stuff that isn't
        // ngen'd
        if (pCF->IsFrameless())
        {
            fMaybeCollectibleMethod = ExecutionManager::IsCollectibleMethod(pCF->GetMethodToken());
        }

        if (fMaybeCollectibleMethod && pMD->IsLCGMethod())
        {
            Object *refResolver = OBJECTREFToObject(pMD->AsDynamicMethodDesc()->GetLCGMethodResolver()->GetManagedResolver());
#ifdef _DEBUG
            Object *oldObj = refResolver;
#endif
            _ASSERTE(refResolver != NULL);
            (*gcctx->f)(&refResolver, gcctx->sc, CHECK_APP_DOMAIN);
            _ASSERTE(!pMD->IsSharedByGenericInstantiations());
            
            // We are reporting the location of a local variable, assert it doesn't change.
            _ASSERTE(oldObj == refResolver);
        }
        else
        {
            if (fMaybeCollectibleMethod)
            {
                GcReportLoaderAllocator(gcctx->f, gcctx->sc, pMD->GetLoaderAllocator());
            }

            if (fReportGCReferences)
            {
                GenericParamContextType paramContextType = GENERIC_PARAM_CONTEXT_NONE;

                if (pCF->IsFrameless())
                {
                    // We need to grab the Context Type here because there are cases where the MethodDesc
                    // is shared, and thus indicates there should be an instantion argument, but the JIT 
                    // was still allowed to optimize it away and we won't grab it below because we're not
                    // reporting any references from this frame.
                    paramContextType = pCF->GetCodeManager()->GetParamContextType(pCF->GetRegisterSet(), pCF->GetCodeInfo());
                }
                else
                {
                    if (pMD->RequiresInstMethodDescArg())
                        paramContextType = GENERIC_PARAM_CONTEXT_METHODDESC;
                    else if (pMD->RequiresInstMethodTableArg())
                        paramContextType = GENERIC_PARAM_CONTEXT_METHODTABLE;
                }

                if (SafeToReportGenericParamContext(pCF))
                {
                    // Handle the case where the method is a static shared generic method and we need to keep the type 
                    // of the generic parameters alive
                    if (paramContextType == GENERIC_PARAM_CONTEXT_METHODDESC)
                    {
                        MethodDesc *pMDReal = dac_cast<PTR_MethodDesc>(pCF->GetParamTypeArg());
                        _ASSERTE((pMDReal != NULL) || !pCF->IsFrameless());
                        if (pMDReal != NULL)
                        {
                            GcReportLoaderAllocator(gcctx->f, gcctx->sc, pMDReal->GetLoaderAllocator());
                        }
                    }
                    else if (paramContextType == GENERIC_PARAM_CONTEXT_METHODTABLE)
                    {
                        MethodTable *pMTReal = dac_cast<PTR_MethodTable>(pCF->GetParamTypeArg());
                        _ASSERTE((pMTReal != NULL) || !pCF->IsFrameless());
                        if (pMTReal != NULL)
                        {
                            GcReportLoaderAllocator(gcctx->f, gcctx->sc, pMTReal->GetLoaderAllocator());
                        }
                    }
                }
            }
        }
    }

    // Since we may be asynchronously walking another thread's stack,
    // check (frequently) for stack-buffer-overrun corruptions after 
    // any long operation
    pCF->CheckGSCookies();

    return SWA_CONTINUE;
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
    if (GCHeap::MarkShouldCompeteForStatics())
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

        if (GCHeap::GetGCHeap()->IsThreadUsingAllocationContextHeap(
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

alloc_context * GCToEEInterface::GetAllocContext(Thread * pThread)
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

    Thread * pThread = NULL;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        fn(pThread->GetAllocContext(), param);
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

struct BackgroundThreadStubArgs
{
    Thread* thread;
    GCBackgroundThreadFunction threadStart;
    void* arg;
    CLREvent threadStartedEvent;
    bool hasStarted;
};

DWORD BackgroundThreadStub(void* arg)
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

    if (threadStubArgs.thread->CreateNewThread(0, (LPTHREAD_START_ROUTINE)BackgroundThreadStub, &threadStubArgs))
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
