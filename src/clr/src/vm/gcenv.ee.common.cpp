// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "gcenv.h"

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
#ifdef USE_GC_INFO_DECODER
    GCInfoToken gcInfoToken = pCF->GetGCInfoToken();
    GcInfoDecoder gcInfoDecoder(gcInfoToken, DECODE_FOR_RANGES_CALLBACK);

    FindFirstInterruptiblePointState state;
    state.offs = offs;
    state.endOffs = endOffs;
    state.returnOffs = -1;

    gcInfoDecoder.EnumerateInterruptibleRanges(&FindFirstInterruptiblePointStateCB, &state);

    return state.returnOffs;
#else
    PORTABILITY_ASSERT("FindFirstInterruptiblePoint");
    return -1;
#endif // USE_GC_INFO_DECODER
}

#endif // WIN64EXCEPTIONS

//-----------------------------------------------------------------------------
// Determine whether we should report the generic parameter context
// 
// This is meant to detect following situations:
//
// When a ThreadAbortException is raised
// in the prolog of a managed method, before the location for the generics 
// context has been initialized; when such a TAE is raised, we are open to a
// race with the GC (e.g. while creating the managed object for the TAE).
// The GC would cause a stack walk, and if we report the stack location for
// the generic param context at this time we'd crash.
// The long term solution is to avoid raising TAEs in any non-GC safe points, 
// and to additionally ensure that we do not expose the runtime to TAE 
// starvation.
//
// When we're in the process of resolution of an interface method and the
// interface method happens to have a default implementation. Normally,
// such methods require a generic context, but since we didn't resolve the
// method to an implementation yet, we don't have the right context (in fact,
// there's no context provided by the caller).
// See code:CEEInfo::getMethodSigInternal
//
inline bool SafeToReportGenericParamContext(CrawlFrame* pCF)
{
    LIMITED_METHOD_CONTRACT;

    if (!pCF->IsFrameless() && pCF->GetFrame()->GetVTablePtr() == StubDispatchFrame::GetMethodFrameVPtr())
    {
        return !((StubDispatchFrame*)pCF->GetFrame())->SuppressParamTypeArg();
    }

    if (!pCF->IsFrameless() || !(pCF->IsActiveFrame() || pCF->IsInterrupted()))
    {
        return true;
    }

#ifndef USE_GC_INFO_DECODER

    ICodeManager * pEECM = pCF->GetCodeManager();
    if (pEECM != NULL && pEECM->IsInPrologOrEpilog(pCF->GetRelOffset(), pCF->GetGCInfoToken(), NULL))
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
    assert((flags & ~(GC_CALL_INTERIOR|GC_CALL_PINNED)) == 0);

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
StackWalkAction GcStackCrawlCallBack(CrawlFrame* pCF, VOID* pData)
{
    //
    // KEEP IN SYNC WITH DacStackReferenceWalker::Callback in debug\daccess\daccess.cpp
    //

    GCCONTEXT   *gcctx = (GCCONTEXT*) pData;

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
#if defined(WIN64EXCEPTIONS) && defined(USE_GC_INFO_DECODER)
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
#endif // WIN64EXCEPTIONS && USE_GC_INFO_DECODER

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

                if (paramContextType != GENERIC_PARAM_CONTEXT_NONE && SafeToReportGenericParamContext(pCF))
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