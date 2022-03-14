// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: frameinfo.cpp
//

//
// Code to find control info about a stack frame.
//
//*****************************************************************************

#include "stdafx.h"

// Include so we can get information out of ComMethodFrame
#ifdef FEATURE_COMINTEROP
#include "COMToClrCall.h"
#endif

// Get a frame pointer from a RegDisplay.
// This is mostly used for chains and stub frames (i.e. internal frames), where we don't need an exact
// frame pointer.  This is why it is okay to use the current SP instead of the caller SP on IA64.
// We should really rename this and possibly roll it into GetFramePointer() when we move the stackwalker
// to OOP.
FramePointer GetSP(REGDISPLAY * pRDSrc)
{
    FramePointer fp = FramePointer::MakeFramePointer(
        (LPVOID)GetRegdisplaySP(pRDSrc));

    return fp;
}

// Get a frame pointer from a RegDisplay.
FramePointer GetFramePointer(REGDISPLAY * pRDSrc)
{
    return FramePointer::MakeFramePointer(GetRegdisplaySP(pRDSrc));
}

//---------------------------------------------------------------------------------------
//
// Convert a FramePointer to a StackFrame and return it.
//
// Arguments:
//    fp    - the FramePointer to be converted
//
// Return Value:
//    a StackFrame equivalent to the given FramePointer
//
// Notes:
//    We really should consolidate the two abstractions for "stack frame identifiers"
//    (StackFrame and FramePointer) when we move the debugger stackwalker to OOP.
//

FORCEINLINE StackFrame ConvertFPToStackFrame(FramePointer fp)
{
    return StackFrame((UINT_PTR)fp.GetSPValue());
}

/* ------------------------------------------------------------------------- *
 * DebuggerFrameInfo routines
 * ------------------------------------------------------------------------- */

//struct DebuggerFrameData:  Contains info used by the DebuggerWalkStackProc
// to do a stack walk.  The info and pData fields are handed to the pCallback
// routine at each frame,
struct DebuggerFrameData
{
    // Initialize this struct. Only done at the start of a stackwalk.
    void Init(
        Thread * _pThread,
        FramePointer _targetFP,
        BOOL fIgnoreNonmethodFrames,        // generally true for stackwalking and false for stepping
        DebuggerStackCallback _pCallback,
        void                    *_pData
    )
    {
        LIMITED_METHOD_CONTRACT;

        this->pCallback = _pCallback;
        this->pData = _pData;

        this->cRealCounter = 0;

        this->thread = _pThread;
        this->targetFP = _targetFP;
        this->targetFound = (_targetFP == LEAF_MOST_FRAME);

        this->ignoreNonmethodFrames = fIgnoreNonmethodFrames;

        // For now, we can tie these to flags together.
        // In everett, we disable SIS (For backwards compat).
        this->fProvideInternalFrames = (fIgnoreNonmethodFrames != 0);

        this->fNeedToSendEnterManagedChain = false;
        this->fTrackingUMChain = false;
        this->fHitExitFrame = false;

        this->info.eStubFrameType = STUBFRAME_NONE;
        this->info.quickUnwind = false;

        this->info.frame     = NULL;
        this->needParentInfo = false;

#ifdef FEATURE_EH_FUNCLETS
        this->fpParent        = LEAF_MOST_FRAME;
        this->info.fIsLeaf    = true;
        this->info.fIsFunclet = false;
        this->info.fIsFilter  = false;
#endif // FEATURE_EH_FUNCLETS

        // Look strange?  Go to definition of this field.  I dare you.
        this->info.fIgnoreThisFrameIfSuppressingUMChainFromComPlusMethodFrameGeneric = false;

#if defined(_DEBUG)
        this->previousFP = LEAF_MOST_FRAME;
#endif // _DEBUG
    }

    // True if we need the next CrawlFrame to fill out part of this FrameInfo's data.
    bool                    needParentInfo;

    // The FrameInfo that we'll dispatch to the pCallback. This matches against
    // the CrawlFrame for that frame that the callback belongs too.
    FrameInfo               info;

    // Regdisplay that the EE stackwalker is updating.
    REGDISPLAY              regDisplay;


#ifdef FEATURE_EH_FUNCLETS
    // This is used to skip funclets in a stackwalk.  It marks the frame pointer to which we should skip.
    FramePointer            fpParent;
#endif // FEATURE_EH_FUNCLETS
#if defined(_DEBUG)
    // For debugging, track the previous FramePointer so we can assert that we're
    // making progress through the stack.
    FramePointer            previousFP;
#endif // _DEBUG

    // whether we have hit an exit frame or not (i.e. a M2U frame)
    bool                    fHitExitFrame;

private:
    // The scope of this field is each section of managed method frames on the stack.
    bool                    fNeedToSendEnterManagedChain;

    // Flag set when we first stack-walk to decide if we want to ignore certain frames.
    // Stepping doesn't ignore these frames; end user stacktraces do.
    BOOL                    ignoreNonmethodFrames;

    // Do we want callbacks for internal frames?
    // Steppers generally don't. User stack-walk does.
    bool                    fProvideInternalFrames;

    // Info for tracking unmanaged chains.
    // We track the starting (leaf) context for an unmanaged chain, as well as the
    // ending (root) framepointer.
    bool                    fTrackingUMChain;
    REGDISPLAY              rdUMChainStart;
    FramePointer            fpUMChainEnd;

    // Thread that the stackwalk is for.
    Thread                  *thread;


    // Target FP indicates at what point in the stackwalk we'll start dispatching callbacks.
    // Naturally, if this is LEAF_MOST_FRAME, then all callbacks will be dispatched
    FramePointer            targetFP;
    bool                    targetFound;

    // Count # of callbacks we could have dispatched (assuming targetFP==LEAF_MOST_FRAME).
    // Useful for detecting leaf.
    int                     cRealCounter;

    // Callback & user-data supplied to that callback.
    DebuggerStackCallback   pCallback;
    void                    *pData;

    private:

    // Raw invoke. This just does some consistency asserts,
    // and invokes the callback if we're in the requested target range.
    StackWalkAction RawInvokeCallback(FrameInfo * pInfo)
    {
#ifdef _DEBUG
        _ASSERTE(pInfo != NULL);
        MethodDesc * md = pInfo->md;
        // Invoke the callback to the user. Log what we're invoking.
        LOG((LF_CORDB, LL_INFO10000, "DSWCallback: MD=%s,0x%p, Chain=%x, Stub=%x, Frame=0x%p, Internal=%d\n",
            ((md == NULL) ? "None" : md->m_pszDebugMethodName), md,
            pInfo->chainReason,
            pInfo->eStubFrameType,
            pInfo->frame, pInfo->internal));

        // Make sure we're providing a valid FrameInfo for the callback.
        pInfo->AssertValid();
#endif
        // Update counter. This provides a convenient check for leaf FrameInfo.
        this->cRealCounter++;


        // Only invoke if we're past the target.
        if (!this->targetFound && IsEqualOrCloserToLeaf(this->targetFP, this->info.fp))
        {
            this->targetFound = true;
        }

        if (this->targetFound)
        {
            return (pCallback)(pInfo, pData);
        }
        else
        {
            LOG((LF_CORDB, LL_INFO10000, "Not invoking yet.\n"));
        }

        return SWA_CONTINUE;
    }

public:
    // Invoke a callback. This may do extra logic to preserve the interface between
    // the LS stackwalker and the LS:
    // - don't invoke if we're not at the target yet
    // - send EnterManagedChains if we need it.
    StackWalkAction InvokeCallback(FrameInfo * pInfo)
    {
        // Track if we've sent any managed code yet.
        // If we haven't, then don't send the enter-managed chain. This catches cases
        // when we have leaf-most unmanaged chain.
        if ((pInfo->frame == NULL) && (pInfo->md != NULL))
        {
            this->fNeedToSendEnterManagedChain = true;
        }


        // Do tracking to decide if we need to send a Enter-Managed chain.
        if (pInfo->HasChainMarker())
        {
            if (pInfo->managed)
            {
                // If we're dispatching a managed-chain, then we don't need to send another one.
                fNeedToSendEnterManagedChain = false;
            }
            else
            {
                // If we're dispatching an UM chain, then send the Managed one.
                // Note that the only unmanaged chains are ThreadStart chains and UM chains.
                if (fNeedToSendEnterManagedChain)
                {
                    fNeedToSendEnterManagedChain = false;

                    FrameInfo f;

                    // Assume entry chain's FP is one pointer-width after the upcoming UM chain.
                    FramePointer fpRoot = FramePointer::MakeFramePointer(
                        (BYTE*) GetRegdisplaySP(&pInfo->registers) - sizeof(DWORD*));

                    f.InitForEnterManagedChain(fpRoot);
                    if (RawInvokeCallback(&f) == SWA_ABORT)
                    {
                        return SWA_ABORT;
                    }
                }
            }
        }

        return RawInvokeCallback(pInfo);
    }

    // Note that we should start tracking an Unmanaged Chain.
    void BeginTrackingUMChain(FramePointer fpRoot, REGDISPLAY * pRDSrc)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(!this->fTrackingUMChain);

        CopyREGDISPLAY(&this->rdUMChainStart, pRDSrc);

        this->fTrackingUMChain = true;
        this->fpUMChainEnd = fpRoot;
        this->fHitExitFrame = false;

        LOG((LF_CORDB, LL_EVERYTHING, "UM Chain starting at Frame=0x%p\n", this->fpUMChainEnd.GetSPValue()));

        // This UM chain may get cancelled later, so don't even worry about toggling the fNeedToSendEnterManagedChain bit here.
        // Invoke() will track whether to send an Enter-Managed chain or not.
    }

    // For various heuristics, we may not want to send an UM chain.
    void CancelUMChain()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(this->fTrackingUMChain);
        this->fTrackingUMChain = false;
    }

    // True iff we're currently tracking an unmanaged chain.
    bool IsTrackingUMChain()
    {
        LIMITED_METHOD_CONTRACT;

        return this->fTrackingUMChain;
    }



    // Get/Set Regdisplay that starts an Unmanaged chain.
    REGDISPLAY * GetUMChainStartRD()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(fTrackingUMChain);
        return &rdUMChainStart;
    }

    // Get/Set FramePointer that ends an unmanaged chain.
    void SetUMChainEnd(FramePointer fp)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(fTrackingUMChain);
        fpUMChainEnd = fp;
    }

    FramePointer GetUMChainEnd()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(fTrackingUMChain);
        return fpUMChainEnd;
    }

    // Get thread we're currently tracing.
    Thread * GetThread()
    {
        LIMITED_METHOD_CONTRACT;
        return thread;
    }

    // Returns true if we're on the leaf-callback (ie, we haven't dispatched a callback yet.
    bool IsLeafCallback()
    {
        LIMITED_METHOD_CONTRACT;
        return cRealCounter == 0;
    }

    bool ShouldProvideInternalFrames()
    {
        LIMITED_METHOD_CONTRACT;
        return fProvideInternalFrames;
    }
    bool ShouldIgnoreNonmethodFrames()
    {
        LIMITED_METHOD_CONTRACT;
        return ignoreNonmethodFrames != 0;
    }
};


//---------------------------------------------------------------------------------------
//
// On IA64, the offset given by the OS during stackwalking is actually the offset at the call instruction.
// This is different from x86 and X64, where the offset is immediately after the call instruction.  In order
// to have a uniform behaviour, we need to do adjust the relative offset on IA64.  This function is a nop on
// other platforms.
//
// Arguments:
//    pCF       - the CrawlFrame for the current method frame
//    pInfo     - This is the FrameInfo for the current method frame.  We need to use the fIsLeaf field,
//                since no adjustment is necessary for leaf frames.
//
// Return Value:
//    returns the adjusted relative offset
//

inline ULONG AdjustRelOffset(CrawlFrame *pCF,
                             FrameInfo  *pInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pCF != NULL);
    }
    CONTRACTL_END;

#if defined(TARGET_ARM)
    return pCF->GetRelOffset() & ~THUMB_CODE;
#else
    return pCF->GetRelOffset();
#endif
}


//---------------------------------------------------------------------------------------
//
// Even when there is an exit frame in the explicit frame chain, it does not necessarily mean that we have
// actually called out to unmanaged code yet or that we actually have a managed call site.  Given an exit
// frame, this function determines if we have a managed call site and have already called out to unmanaged
// code.  If we have, then we return the caller SP as the potential frame pointer.  Otherwise we return
// LEAF_MOST_FRAME.
//
// Arguments:
//    pFrame        - the exit frame to be checked
//    pData         - the state of the current frame maintained by the debugger stackwalker
//    pPotentialFP  - This is an out parameter.  It returns the caller SP of the last managed caller if
//                    there is a managed call site and we have already called out to unmanaged code.
//                    Otherwise, LEAF_MOST_FRAME is returned.
//
// Return Value:
//    true  - we have a managed call site and we have called out to unmanaged code
//    false - otherwise
//

bool HasExitRuntime(Frame *pFrame, DebuggerFrameData *pData, FramePointer *pPotentialFP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER; // Callers demand this function be GC_NOTRIGGER.
        MODE_ANY;
        PRECONDITION(pFrame->GetFrameType() == Frame::TYPE_EXIT);
    }
    CONTRACTL_END;

#ifdef TARGET_X86
    TADDR returnIP, returnSP;

    EX_TRY
    {
        // This is a real issue. This may be called while holding GC-forbid locks, and so
        // this function can't trigger a GC. However, the only impl we have calls GC-trigger functions.
        CONTRACT_VIOLATION(GCViolation);
        pFrame->GetUnmanagedCallSite(NULL, &returnIP, &returnSP);
    }
    EX_CATCH
    {
        // We never expect an actual exception here (maybe in oom).
        // If we get an exception, then simulate the default behavior for GetUnmanagedCallSite.
        returnIP = NULL;
        returnSP = NULL; // this will cause us to return true.
    }
    EX_END_CATCH(SwallowAllExceptions);

    LOG((LF_CORDB, LL_INFO100000,
         "DWSP: TYPE_EXIT: returnIP=0x%08x, returnSP=0x%08x, frame=0x%08x, threadFrame=0x%08x, regSP=0x%08x\n",
         returnIP, returnSP, pFrame, pData->GetThread()->GetFrame(), GetRegdisplaySP(&pData->regDisplay)));

    if (pPotentialFP != NULL)
    {
        *pPotentialFP = FramePointer::MakeFramePointer((void*)returnSP);
    }

    return ((pFrame != pData->GetThread()->GetFrame()) ||
            (returnSP == NULL) ||
            ((TADDR)GetRegdisplaySP(&pData->regDisplay) <= returnSP));

#else // TARGET_X86
    // DebuggerExitFrame always return a NULL returnSP on x86.
    if (pFrame->GetVTablePtr() == DebuggerExitFrame::GetMethodFrameVPtr())
    {
        if (pPotentialFP != NULL)
        {
            *pPotentialFP = LEAF_MOST_FRAME;
        }
        return true;
    }
    else if (pFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr())
    {
        InlinedCallFrame *pInlinedFrame = static_cast<InlinedCallFrame *>(pFrame);
        LPVOID sp = (LPVOID)pInlinedFrame->GetCallSiteSP();

        // The sp returned below is the sp of the caller, which is either an IL stub in the normal case
        // or a normal managed method in the inlined pinvoke case.
        // This sp may be the same as the frame's address, so we need to use the largest
        // possible bsp value to make sure that this frame pointer is closer to the root than
        // the frame pointer made from the frame address itself.
        if (pPotentialFP != NULL)
        {
            *pPotentialFP = FramePointer::MakeFramePointer( (LPVOID)sp );
        }

        return ((pFrame != pData->GetThread()->GetFrame()) ||
            InlinedCallFrame::FrameHasActiveCall(pInlinedFrame));

    }
    else
    {
        // It'll be nice if there's a way to assert that the current frame is indeed of a
        // derived class of TransitionFrame.
        TransitionFrame *pTransFrame = static_cast<TransitionFrame*>(pFrame);
        LPVOID sp = (LPVOID)pTransFrame->GetSP();

        // The sp returned below is the sp of the caller, which is either an IL stub in the normal case
        // or a normal managed method in the inlined pinvoke case.
        // This sp may be the same as the frame's address, so we need to use the largest
        // possible bsp value to make sure that this frame pointer is closer to the root than
        // the frame pointer made from the frame address itself.
        if (pPotentialFP != NULL)
        {
            *pPotentialFP = FramePointer::MakeFramePointer( (LPVOID)sp );
        }

        return true;
    }
#endif // TARGET_X86
}

#ifdef _DEBUG

//-----------------------------------------------------------------------------
// Debug helpers to get name of Frame.
//-----------------------------------------------------------------------------
LPCUTF8 FrameInfo::DbgGetClassName()
{
    return (md == NULL) ? ("None") : (md->m_pszDebugClassName);
}
LPCUTF8 FrameInfo::DbgGetMethodName()
{
    return (md == NULL) ? ("None") : (md->m_pszDebugMethodName);
}


//-----------------------------------------------------------------------------
// Debug helper to asserts invariants about a FrameInfo before we dispatch it.
//-----------------------------------------------------------------------------
void FrameInfo::AssertValid()
{
    LIMITED_METHOD_CONTRACT;

    bool fMethod    = this->HasMethodFrame();
    bool fStub      = this->HasStubFrame();
    bool fChain     = this->HasChainMarker();

    // Can't be both Stub & Chain
    _ASSERTE(!fStub || !fChain);

    // Must be at least a Method, Stub or Chain or Internal
    _ASSERTE(fMethod || fStub || fChain || this->internal);

    // Check Managed status is consistent
    if (fMethod)
    {
        _ASSERTE(this->managed); // We only report managed methods
    }
    if (fChain)
    {
        if (!managed)
        {
            // Only certain chains can be unmanaged
            _ASSERTE((this->chainReason == CHAIN_THREAD_START) ||
                     (this->chainReason == CHAIN_ENTER_UNMANAGED));
        }
        else
        {
            // UM chains can never be managed.
            _ASSERTE((this->chainReason != CHAIN_ENTER_UNMANAGED));
        }

    }

    // FramePointer should be valid
    _ASSERTE(this->fp != LEAF_MOST_FRAME);
    _ASSERTE((this->fp != ROOT_MOST_FRAME) || (chainReason== CHAIN_THREAD_START) || (chainReason == CHAIN_ENTER_UNMANAGED));

    // If we have a Method, then we need an AppDomain.
    // (RS will need it to do lookup)
    if (fMethod)
    {
        _ASSERTE(currentAppDomain != NULL);
        _ASSERTE(managed);
        // Stubs may have a method w/o any code (eg, PInvoke wrapper).
        // @todo - Frame::TYPE_TP_METHOD_FRAME breaks this assert. Are there other cases too?
        //_ASSERTE(fStub || (pIJM != NULL));
    }

    if (fStub)
    {
        // All stubs (except LightWeightFunctions) match up w/a Frame.
        _ASSERTE(this->frame || (eStubFrameType == STUBFRAME_LIGHTWEIGHT_FUNCTION));
    }
}
#endif

//-----------------------------------------------------------------------------
// Get the DJI associated w/ this frame. This is a convenience function.
// This is recommended over using MethodDescs because DJI's are version-aware.
//-----------------------------------------------------------------------------
DebuggerJitInfo * FrameInfo::GetJitInfoFromFrame() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Not all FrameInfo objects correspond to actual code.
    if (HasChainMarker() || HasStubFrame() || (frame != NULL))
    {
        return NULL;
    }

    DebuggerJitInfo *ji = NULL;

    EX_TRY
    {
        _ASSERTE(this->md != NULL);
        ji = g_pDebugger->GetJitInfo(this->md, (const BYTE*)GetControlPC(&(this->registers)));
        _ASSERTE(ji == NULL || ji->m_nativeCodeVersion.GetMethodDesc() == this->md);
    }
    EX_CATCH
    {
        ji = NULL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return ji;
}

//-----------------------------------------------------------------------------
// Get the DMI associated w/ this frame. This is a convenience function.
// DMIs are 1:1 with the (token, module) pair.
//-----------------------------------------------------------------------------
DebuggerMethodInfo * FrameInfo::GetMethodInfoFromFrameOrThrow()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    MethodDesc * pDesc = this->md;
    mdMethodDef token = pDesc-> GetMemberDef();
    Module * pRuntimeModule = pDesc->GetModule();

    DebuggerMethodInfo *dmi = g_pDebugger->GetOrCreateMethodInfo(pRuntimeModule, token);
    return dmi;
}


//-----------------------------------------------------------------------------
// Init a FrameInfo for a UM chain.
// We need a stackrange to give to an unmanaged debugger.
// pRDSrc->Esp will provide the start (leaf) marker.
// fpRoot will provide the end (root) portion.
//-----------------------------------------------------------------------------
void FrameInfo::InitForUMChain(FramePointer fpRoot, REGDISPLAY * pRDSrc)
{
    _ASSERTE(pRDSrc != NULL);

    // Mark that we're an UM Chain (and nothing else).
    this->frame = NULL;
    this->md = NULL;

    // Fp will be the end (root) of the stack range.
    // pRDSrc->Sp will be the start (leaf) of the stack range.
    CopyREGDISPLAY(&(this->registers), pRDSrc);
    this->fp = fpRoot;

    this->quickUnwind = false;
    this->internal = false;
    this->managed = false;

    // These parts of the FrameInfo can be ignored for a UM chain.
    this->relOffset = 0;
    this->pIJM = NULL;
    this->MethodToken = METHODTOKEN(NULL, 0);
    this->currentAppDomain = NULL;
    this->exactGenericArgsToken = NULL;

    InitForScratchFrameInfo();

    this->chainReason    = CHAIN_ENTER_UNMANAGED;
    this->eStubFrameType = STUBFRAME_NONE;

#ifdef _DEBUG
    FramePointer fpLeaf = GetSP(pRDSrc);
    _ASSERTE(IsCloserToLeaf(fpLeaf, fpRoot));
#endif

#ifdef _DEBUG
    // After we just init it, it had better be valid.
    this->AssertValid();
#endif
}


//---------------------------------------------------------------------------------------
//
// This is just a small helper to initialize the fields which are specific to 64-bit.  Note that you should
// only call this function on a scratch FrameInfo.  Never call it on the FrameInfo used by the debugger
// stackwalker to store information on the current frame.
//

void FrameInfo::InitForScratchFrameInfo()
{
#ifdef FEATURE_EH_FUNCLETS
    // The following flags cannot be trashed when we are calling this function on the curret FrameInfo
    // (the one we keep track of across multiple stackwalker callbacks).  Thus, make sure you do not call
    // this function from InitForDynamicMethod().  In all other cases, we can call this method after we
    // call InitFromStubHelper() because we are working on a local scratch variable.
    this->fIsLeaf    = false;
    this->fIsFunclet = false;
    this->fIsFilter  = false;
#endif // FEATURE_EH_FUNCLETS
}


//-----------------------------------------------------------------------------
//
// Init a FrameInfo for a stub.  Stub frames map to internal frames on the RS.  Stubs which we care about
// usually contain an explicit frame which translates to an internal frame on the RS.  Dynamic method is
// the sole exception.
//
// Arguments:
//    pCF       - the CrawlFrame containing the state of the current frame
//    pMDHint   - some stubs have associated MethodDesc but others don't,
//                which is why this argument can be NULL
//    type      - the type of the stub/internal frame
//

void FrameInfo::InitFromStubHelper(
    CrawlFrame * pCF,
    MethodDesc * pMDHint, // NULL ok
    CorDebugInternalFrameType type
)
{
    _ASSERTE(pCF != NULL);

    Frame * pFrame = pCF->GetFrame();

    LOG((LF_CORDB, LL_EVERYTHING, "InitFromStubHelper. Frame=0x%p, type=%d\n", pFrame, type));

    // All Stubs have a Frame except for LightWeight methods
    _ASSERTE((type == STUBFRAME_LIGHTWEIGHT_FUNCTION) || (pFrame != NULL));
    REGDISPLAY *pRDSrc = pCF->GetRegisterSet();

    this->frame = pFrame;

    // Stub frames may be associated w/ a Method (as a hint). However this method
    // will never have a JitManager b/c it will never have IL (if it had IL, we'd be a
    // regulare frame, not a stub frame)
    this->md = pMDHint;

    CopyREGDISPLAY(&this->registers, pRDSrc);

    // FramePointer must match up w/ an EE Frame b/c that's how we match
    // we Exception callbacks.
    if (pFrame != NULL)
    {
        this->fp = FramePointer::MakeFramePointer(
            (LPVOID) pFrame);
    }
    else
    {
        this->fp = GetSP(pRDSrc);
    }

    this->quickUnwind = false;
    this->internal    = false;
    this->managed     = true;
    this->relOffset   = 0;
    this->ambientSP   = NULL;


    // Method associated w/a stub will never have a JitManager.
    this->pIJM        = NULL;
    this->MethodToken = METHODTOKEN(NULL, 0);
    this->currentAppDomain      = AppDomain::GetCurrentDomain();
    this->exactGenericArgsToken = NULL;

    // Stub frames are mutually exclusive with chain markers.
    this->chainReason    = CHAIN_NONE;
    this->eStubFrameType = type;

#ifdef _DEBUG
    // After we just init it, it had better be valid.
    this->AssertValid();
#endif
}

//-----------------------------------------------------------------------------
// Initialize a FrameInfo to be used for an "InternalFrame"
// Frame should be a derived class of FramedMethodFrame.
// FrameInfo's MethodDesc will be for managed wrapper for native call.
//-----------------------------------------------------------------------------
void FrameInfo::InitForM2UInternalFrame(CrawlFrame * pCF)
{
    // For a M2U call, there's a managed method wrapping the unmanaged call. Use that.
    Frame * pFrame = pCF->GetFrame();
    _ASSERTE(pFrame->GetTransitionType() == Frame::TT_M2U);
    FramedMethodFrame * pM2U = static_cast<FramedMethodFrame*> (pFrame);
    MethodDesc * pMDWrapper = pM2U->GetFunction();

    // Soem M2U transitions may not have a function associated w/ them,
    // so pMDWrapper may be NULL. PInvokeCalliFrame is an example.

    InitFromStubHelper(pCF, pMDWrapper, STUBFRAME_M2U);
    InitForScratchFrameInfo();
}

//-----------------------------------------------------------------------------
// Initialize for the U2M case...
//-----------------------------------------------------------------------------
void FrameInfo::InitForU2MInternalFrame(CrawlFrame * pCF)
{
    PREFIX_ASSUME(pCF != NULL);
    MethodDesc * pMDHint = NULL;

#ifdef FEATURE_COMINTEROP
    Frame * pFrame = pCF->GetFrame();
    PREFIX_ASSUME(pFrame != NULL);


    // For regular U2M PInvoke cases, we don't care about MD b/c it's just going to
    // be the next frame.
    // If we're a COM2CLR call, perhaps we can get the MD for the interface.
    if (pFrame->GetVTablePtr() == ComMethodFrame::GetMethodFrameVPtr())
    {
        ComMethodFrame* pCOMFrame = static_cast<ComMethodFrame*> (pFrame);
        ComCallMethodDesc* pCMD = reinterpret_cast<ComCallMethodDesc *> (pCOMFrame->ComMethodFrame::GetDatum());
        pMDHint = pCMD->GetInterfaceMethodDesc();

        // Some COM-interop cases don't have an intermediate interface method desc, so
        // pMDHint may be null.
    }
#endif

    InitFromStubHelper(pCF, pMDHint, STUBFRAME_U2M);
    InitForScratchFrameInfo();
}

//-----------------------------------------------------------------------------
// Init for an AD transition
//-----------------------------------------------------------------------------
void FrameInfo::InitForADTransition(CrawlFrame * pCF)
{
    Frame * pFrame;
    pFrame = pCF->GetFrame();
    _ASSERTE(pFrame->GetTransitionType() == Frame::TT_AppDomain);
    MethodDesc * pMDWrapper = NULL;

    InitFromStubHelper(pCF, pMDWrapper, STUBFRAME_APPDOMAIN_TRANSITION);
    InitForScratchFrameInfo();
}


//-----------------------------------------------------------------------------
// Init frame for a dynamic method.
//-----------------------------------------------------------------------------
void FrameInfo::InitForDynamicMethod(CrawlFrame * pCF)
{
    // These are just stack markers that there's a dynamic method on the callstack.
    InitFromStubHelper(pCF, NULL, STUBFRAME_LIGHTWEIGHT_FUNCTION);
    // Do not call InitForScratchFrameInfo() here!  Please refer to the comment in that function.
}

//-----------------------------------------------------------------------------
// Init an internal frame to mark a func-eval.
//-----------------------------------------------------------------------------
void FrameInfo::InitForFuncEval(CrawlFrame * pCF)
{
    // We don't store a MethodDesc hint referring to the method we're going to invoke because
    // uses of stub frames will assume the MD is relative to the AppDomain the frame is in.
    // For cross-AD funcevals, we're invoking a method in a domain other than the one this frame
    // is in.
    MethodDesc * pMDHint = NULL;

    // Add a stub frame here to mark that there is a FuncEvalFrame on the stack.
    InitFromStubHelper(pCF, pMDHint, STUBFRAME_FUNC_EVAL);
    InitForScratchFrameInfo();
}


//---------------------------------------------------------------------------------------
//
// Initialize a FrameInfo for sending the CHAIN_THREAD_START reason.
// The common case is that the chain is NOT managed, since the lowest (closest to the root) managed method
// is usually called from unmanaged code.  In fact, in Whidbey, we should never have a managed chain.
//
// Arguments:
//    pRDSrc    - a REGDISPLAY for the beginning (the leafmost frame) of the chain
//
void FrameInfo::InitForThreadStart(Thread * pThread, REGDISPLAY * pRDSrc)
{
    this->frame = (Frame *) FRAME_TOP;
    this->md = NULL;
    CopyREGDISPLAY(&(this->registers), pRDSrc);
    this->fp    = FramePointer::MakeFramePointer(pThread->GetCachedStackBase());
    this->quickUnwind = false;
    this->internal = false;
    this->managed     = false;
    this->relOffset   = 0;
    this->pIJM        = NULL;
    this->MethodToken = METHODTOKEN(NULL, 0);

    this->currentAppDomain = NULL;
    this->exactGenericArgsToken = NULL;

    InitForScratchFrameInfo();

    this->chainReason    = CHAIN_THREAD_START;
    this->eStubFrameType = STUBFRAME_NONE;

#ifdef _DEBUG
    // After we just init it, it had better be valid.
    this->AssertValid();
#endif
}


//---------------------------------------------------------------------------------------
//
// Initialize a FrameInfo for sending a CHAIN_ENTER_MANAGED.
// A Enter-Managed chain is always sent immediately before an UM chain, meaning that the Enter-Managed chain
// is closer to the leaf than the UM chain.
//
// Arguments:
//    fpRoot    - This is the frame pointer for the Enter-Managed chain.  It is currently arbitrarily set
//                to be one stack slot higher (closer to the leaf) than the frame pointer of the beginning
//                of the upcoming UM chain.
//

void FrameInfo::InitForEnterManagedChain(FramePointer fpRoot)
{
    // Nobody should use a EnterManagedChain's Frame*, but there's no
    // good value to enforce that.
    this->frame = (Frame *) FRAME_TOP;
    this->md    = NULL;
    memset((void *)&this->registers, 0, sizeof(this->registers));
    this->fp = fpRoot;

    this->quickUnwind = true;
    this->internal    = false;
    this->managed     = true;
    this->relOffset   = 0;
    this->pIJM        = NULL;
    this->MethodToken = METHODTOKEN(NULL, 0);

    this->currentAppDomain = NULL;
    this->exactGenericArgsToken = NULL;

    InitForScratchFrameInfo();

    this->chainReason    = CHAIN_ENTER_MANAGED;
    this->eStubFrameType = STUBFRAME_NONE;
}

//-----------------------------------------------------------------------------
// Do tracking for UM chains.
// This may invoke the UMChain callback and M2U callback.
//-----------------------------------------------------------------------------
StackWalkAction TrackUMChain(CrawlFrame *pCF, DebuggerFrameData *d)
{
    Frame *frame = g_pEEInterface->GetFrame(pCF);

    // If we encounter an ExitFrame out in the wild, then we'll convert it to an UM chain.
    if (!d->IsTrackingUMChain())
    {
        if ((frame != NULL) && (frame != FRAME_TOP) && (frame->GetFrameType() == Frame::TYPE_EXIT))
        {
            LOG((LF_CORDB, LL_EVERYTHING, "DWSP. ExitFrame while not tracking\n"));
            REGDISPLAY* pRDSrc = pCF->GetRegisterSet();

            d->BeginTrackingUMChain(GetSP(pRDSrc), pRDSrc);

            // fall through and we'll send the UM chain.
        }
        else
        {
            return SWA_CONTINUE;
        }
    }

    _ASSERTE(d->IsTrackingUMChain());


    // If we're tracking an UM chain, then we need to:
    // - possibly refine the start & end values as we get new information in the stacktrace.
    // - possibly cancel the UM chain for various heuristics.
    // - possibly dispatch if we've hit managed code again.

    bool fDispatchUMChain = false;
    // UM Chain stops when managed code starts again.
    if (frame != NULL)
    {
        // If it's just a EE Frame, then update this as a possible end of stack range for the UM chain.
        // (The end of a stack range is closer to the root.)
        d->SetUMChainEnd(FramePointer::MakeFramePointer((LPVOID)(frame)));


        Frame::ETransitionType t = frame->GetTransitionType();
        int ft      = frame->GetFrameType();


        // Sometimes we may not want to show an UM chain b/c we know it's just
        // code inside of mscorwks. (Eg: Funcevals & AD transitions both fall into this category).
        // These are perfectly valid UM chains and we could give them if we wanted to.
        if ((t == Frame::TT_AppDomain) || (ft == Frame::TYPE_FUNC_EVAL))
        {
            d->CancelUMChain();
            return SWA_CONTINUE;
        }

        // If we hit an M2U frame, then go ahead and dispatch the UM chain now.
        // This will likely also be an exit frame.
        if (t == Frame::TT_M2U)
        {
            fDispatchUMChain = true;
        }

        // If we get an Exit frame, we can use that to "prune" the UM chain to a more friendly state.
        // This heuristic is optional, it just eliminates lots of internal mscorwks frames from the callstack.
        // Note that this heuristic is only useful if we get a callback on the entry frame
        // (e.g. UMThkCallFrame) between the callback on the native marker and the callback on the exit frame.
        // Otherwise the REGDISPLAY will be the same.
        if (ft == Frame::TYPE_EXIT)
        {
            // If we have a valid reg-display (non-null IP) then update it.
            // We may have an invalid reg-display if we have an exit frame on an inactive thread.
            REGDISPLAY * pNewRD = pCF->GetRegisterSet();
            if (GetControlPC(pNewRD) != NULL)
            {
                LOG((LF_CORDB, LL_EVERYTHING, "DWSP. updating RD while tracking UM chain\n"));
                CopyREGDISPLAY(d->GetUMChainStartRD(), pNewRD);
            }

            FramePointer fpLeaf = GetSP(d->GetUMChainStartRD());
            _ASSERTE(IsCloserToLeaf(fpLeaf, d->GetUMChainEnd()));


            _ASSERTE(!d->fHitExitFrame); // should only have 1 exit frame per UM chain code.
            d->fHitExitFrame = true;

            FramePointer potentialFP;

            FramePointer fpNewChainEnd = d->GetUMChainEnd();

            // Check to see if we are inside the unmanaged call. We want to make sure we only report an exit frame after
            // we've really exited. There is a short period between where we setup the frame and when we actually exit
            // the runtime. This check is intended to ensure we're actually outside now.
            if (HasExitRuntime(frame, d, &potentialFP))
            {
                LOG((LF_CORDB, LL_EVERYTHING, "HasExitRuntime. potentialFP=0x%p\n", potentialFP.GetSPValue()));

                // If we have no call site, manufacture a FP using the current frame.
                // If we do have a call site, then the FP is actually going to be the caller SP,
                // where the caller is the last managed method before calling out to unmanaged code.
                if (potentialFP == LEAF_MOST_FRAME)
                {
                    fpNewChainEnd = FramePointer::MakeFramePointer((LPVOID)((BYTE*)frame - sizeof(LPVOID)));
                }
                else
                {
                    fpNewChainEnd = potentialFP;
                }

            }
            // For IL stubs, we may actually push an uninitialized InlinedCallFrame frame onto the frame chain
            // in jitted managed code, and then later on initialize it in a native runtime helper. In this case, if
            // HasExitRuntime() is false (meaning the frame is uninitialized), then we are actually still in managed
            // code and have not made the call to native code yet, so we should report an unmanaged chain.
            else
            {
                d->CancelUMChain();
                return SWA_CONTINUE;
            }

            fDispatchUMChain = true;

            // If we got a valid chain end, then prune the UM chain accordingly.
            // Note that some EE Frames will give invalid info back so we have to check.
            // PInvokeCalliFrame is one example (when doing MC++ function pointers)
            if (IsCloserToRoot(fpNewChainEnd, fpLeaf))
            {
                d->SetUMChainEnd(fpNewChainEnd);
            }
            else
            {
                _ASSERTE(IsCloserToLeaf(fpLeaf, d->GetUMChainEnd()));
            }
        } // end ExitFrame

        // Only CLR internal code / stubs can push Frames onto the Frame chain.
        // So if we hit a raw interceptor frame before we hit any managed frame, then this whole
        // UM chain must still be in CLR internal code.
        // Either way, this UM chain has ended (and some new chain based off the frame has started)
        // so we need to either Cancel the chain or dispatch it.
        if (frame->GetInterception() != Frame::INTERCEPTION_NONE)
        {
            // Interceptors may contain calls out to unmanaged code (such as unmanaged dllmain when
            // loading a new dll), so we need to dispatch these.
            // These extra UM chains don't show in Everett, and so everett debuggers on whidbey
            // may see new chains.
            // We need to ensure that whidbey debuggers are updated first.
            fDispatchUMChain = true;
        }
    }
    else
    {
        // If it's a real method (not just an EE Frame), then the UM chain is over.
        fDispatchUMChain = true;
    }


    if (fDispatchUMChain)
    {
        // Check if we should cancel the UM chain.

        // We need to discriminate between the following 2 cases:
        // 1) Managed -(a)-> mscorwks -(b)-> Managed  (leaf)
        // 2) Native  -(a)-> mscorwks -(b)-> Managed  (leaf)
        //
        // --INCORRECT RATIONALE SEE "CORRECTION" BELOW--
        // Case 1 could happen if a managed call injects a stub (such as w/ delegates).
        // In both cases, the (mscorwks-(b)->managed) transition causes a IsNativeMarker callback
        // which initiates a UM chain. In case 1, we want to cancel the UM chain, but
        // in case 2 we want to dispatch it.
        // The difference is case #2 will have some EE Frame at (b) and case #1 won't.
        // That EE Frame should have caused us to dispatch the call for the managed method, and
        // thus by the time we get around to dispatching the UM Chain, we shouldn't have a managed
        // method waiting to be dispatched in the DebuggerFrameData.
        // --END INCORRECT RATIONALE--
        //
        // This is kind of messed up.  First of all, the assertions on case 2 is not true on 64-bit.
        // We won't have an explicit frame at (b).  Secondly, case 1 is not always true either.
        // Consider the case where we are calling a cctor at prestub time.  This is what the stack may
        // look like: managed -> PrestubMethodFrame -> GCFrame -> managed (cctor) (leaf).  In this case,
        // we will actually send the UM chain because we will have dispatched the call for the managed
        // method (the cctor) when we get a callback for the GCFrame.
        //
        // --INCORRECT SEE "CORRECTION" BELOW--
        // Keep in mind that this is just a heuristic to reduce the number of UM chains we are sending
        // over to the RS.
        // --END INCORRECT --
        //
        // CORRECTION: These UM chains also feed into the results of at least ControllerStackInfo and probably other
        // places. Issue 650903 is a concrete example of how not filtering a UM chain causes correctness
        // issues in the LS. This code may still have bugs in it based on those incorrect assumptions.
        // A narrow fix for 650903 is the only thing that was changed at the time of adding this comment.
        if (d->needParentInfo && d->info.HasMethodFrame())
        {
            LOG((LF_CORDB, LL_EVERYTHING, "Cancelling UM Chain b/c it's internal\n"));
            d->CancelUMChain();
            return SWA_CONTINUE;
        }

        // If we're NOT ignoring non-method frames, and we didn't get an explicit ExitFrame somewhere
        // in this chain, then don't send the non-leaf UM chain.
        // The practical cause here is that w/o an exit frame, we don't know where the UM chain
        // is starting (could be from anywhere in mscorwks). And we can't patch any random spot in
        // mscorwks.
        // Sending leaf-UM chains is OK b/c we can't step-out to them (they're the leaf, duh).
        // (ignoreNonmethodFrames is generally false for stepping and true for regular
        // end-user stacktraces.)
        //
        // This check is probably unnecessary.  The client of the debugger stackwalker should make
        // the decision themselves as to what to do with the UM chain callbacks.
        //
        // -- INCORRECT SEE SEE "CORRECTION" BELOW --
        // Currently, both
        // ControllerStackInfo and InterceptorStackInfo ignore UM chains completely anyway.
        // (For an example, refer to the cctor example in the previous comment.)
        // -- END INCORRECT --
        //
        // CORRECTION: See issue 650903 for a concrete example of ControllerStackInfo getting a different
        // result based on a UM chain that wasn't filtered. This code may still have issues in
        // it based on those incorrect assumptions. A narrow fix for 650903 is the only thing
        // that was changed at the time of adding this comment.
        if (!d->fHitExitFrame && !d->ShouldIgnoreNonmethodFrames() && !d->IsLeafCallback())
        {
            LOG((LF_CORDB, LL_EVERYTHING, "Cancelling UM Chain b/c it's stepper not requested\n"));
            d->CancelUMChain();
            return SWA_CONTINUE;
        }


        // Ok, we haven't cancelled it yet, so go ahead and send the UM chain.
        FrameInfo f;
        FramePointer fpRoot = d->GetUMChainEnd();
        FramePointer fpLeaf = GetSP(d->GetUMChainStartRD());

        // If we didn't actually get any range, then don't bother sending it.
        if (fpRoot == fpLeaf)
        {
            d->CancelUMChain();
            return SWA_CONTINUE;
        }

        f.InitForUMChain(fpRoot, d->GetUMChainStartRD());

#ifdef FEATURE_COMINTEROP
        if ((frame != NULL) &&
            (frame->GetVTablePtr() == ComPlusMethodFrame::GetMethodFrameVPtr()))
        {
            // This condition is part of the fix for 650903. (See
            // code:ControllerStackInfo::WalkStack and code:DebuggerStepper::TrapStepOut
            // for the other parts.) Here, we know that the frame we're looking it may be
            // a ComPlusMethodFrameGeneric (this info is not otherwise plubmed down into
            // the walker; even though the walker does get to see "f.frame", that may not
            // be "frame"). Given this, if the walker chooses to ignore these frames
            // (while doing a Step Out during managed-only debugging), then it can ignore
            // this frame.
            f.fIgnoreThisFrameIfSuppressingUMChainFromComPlusMethodFrameGeneric = true;
        }
#endif // FEATURE_COMINTEROP

        if (d->InvokeCallback(&f) == SWA_ABORT)
        {
            // don't need to cancel if they abort.
            return SWA_ABORT;
        }
        d->CancelUMChain(); // now that we've sent it, we're done.


        // Check for a M2U internal frame.
        if (d->ShouldProvideInternalFrames() && (frame != NULL) && (frame != FRAME_TOP))
        {
            // We want to dispatch a M2U transition right after we dispatch the UM chain.
            Frame::ETransitionType t = frame->GetTransitionType();
            if (t == Frame::TT_M2U)
            {
                // Frame for a M2U transition.
                FrameInfo fM2U;
                fM2U.InitForM2UInternalFrame(pCF);
                if (d->InvokeCallback(&fM2U) == SWA_ABORT)
                {
                    return SWA_ABORT;
                }
            }
        }


    }

    return SWA_CONTINUE;
}

//---------------------------------------------------------------------------------------
//
// A frame pointer is a unique identifier for a particular stack location.  This function returns the
// frame pointer for the current frame, whether it is a method frame or an explicit frame.
//
// Arguments:
//    pData - the state of the current frame maintained by the debugger stackwalker
//    pCF   - the CrawlFrame for the current callback by the real stackwalker (i.e. StackWalkFramesEx());
//            this is NULL for the case where we fake an extra callbakc to top off a debugger stackwalk
//
// Return Value:
//    the frame pointer for the current frame
//

FramePointer GetFramePointerForDebugger(DebuggerFrameData* pData, CrawlFrame* pCF)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    FramePointer fpResult;

#if defined(FEATURE_EH_FUNCLETS)
    if (pData->info.frame == NULL)
    {
        // This is a managed method frame.
        fpResult = FramePointer::MakeFramePointer((LPVOID)GetRegdisplayStackMark(&pData->info.registers));
    }
    else
    {
        // This is an actual frame.
        fpResult = FramePointer::MakeFramePointer((LPVOID)(pData->info.frame));
    }

#else  // !FEATURE_EH_FUNCLETS
    if ((pCF == NULL || !pCF->IsFrameless()) && pData->info.frame != NULL)
    {
        //
        // If we're in an explicit frame now, and the previous frame was
        // also an explicit frame, pPC will not have been updated.  So
        // use the address of the frame itself as fp.
        //
        fpResult = FramePointer::MakeFramePointer((LPVOID)(pData->info.frame));

        LOG((LF_CORDB, LL_INFO100000, "GFPFD: Two explicit frames in a row; using frame address 0x%p\n",
             pData->info.frame));
    }
    else
    {
        //
        // Otherwise use pPC as the frame pointer, as this will be
        // pointing to the return address on the stack.
        //
        fpResult = FramePointer::MakeFramePointer((LPVOID)GetRegdisplayStackMark(&(pData->regDisplay)));
    }

#endif // !FEATURE_EH_FUNCLETS

    LOG((LF_CORDB, LL_INFO100000, "GFPFD: Frame pointer is 0x%p\n", fpResult.GetSPValue()));

    return fpResult;
}


#ifdef FEATURE_EH_FUNCLETS
//---------------------------------------------------------------------------------------
//
// This function is called to determine if we should start skipping funclets.  If we should, then we return the
// frame pointer for the parent method frame.  Otherwise we return LEAF_MOST_FRAME.  If we are already skipping
// frames, then we return the current frame pointer for the parent method frame.
//
// The return value of this function corresponds to the return value of ExceptionTracker::FindParentStackFrame().
// Refer to that function for more information.
//
// Arguments:
//    fpCurrentParentMarker     - This is the current frame pointer of the parent method frame.  It can be
//                                LEAF_MOST_FRAME if we are not currently skipping funclets.
//    pCF                       - the CrawlFrame for the current callback from the real stackwalker
//    fIsNonFilterFuncletFrame  - whether the current frame is a non-filter funclet frame
//
// Return Value:
//    LEAF_MOST_FRAME   - skipping not required
//    ROOT_MOST_FRAME   - skip one frame and try again
//    anything else     - skip all frames up to but not including the returned frame pointer
//

inline FramePointer CheckForParentFP(FramePointer fpCurrentParentMarker, CrawlFrame* pCF, bool fIsNonFilterFuncletFrame)
{
    WRAPPER_NO_CONTRACT;

    if (fpCurrentParentMarker == LEAF_MOST_FRAME)
    {
        // When we encounter a funclet, we simply stop processing frames until we hit the parent
        // of the funclet.  Funclets and their parents have the same MethodDesc pointers, and they
        // should really be treated as one frame.  However, we report both of them and let the callers
        // decide what they want to do with them.  For example, DebuggerThread::TraceAndSendStack()
        // should never report both frames, but ControllerStackInfo::GetStackInfo() may need both to
        // determine where to put a patch.  We use the fpParent as a flag to indicate if we are
        // searching for a parent of a funclet.
        //
        // Note that filter funclets are an exception.  We don't skip them.
        if (fIsNonFilterFuncletFrame)
        {
            // We really should be using the same structure, but FramePointer is used everywhere in the debugger......
            StackFrame sfParent = g_pEEInterface->FindParentStackFrame(pCF);
            return FramePointer::MakeFramePointer((LPVOID)sfParent.SP);
        }
        else
        {
            return LEAF_MOST_FRAME;
        }
    }
    else
    {
        // Just return the current marker if we are already skipping frames.
        return fpCurrentParentMarker;
    }
}
#endif // FEATURE_EH_FUNCLETS


//-----------------------------------------------------------------------------
// StackWalkAction DebuggerWalkStackProc():  This is the callback called
// by the EE stackwalker.
// Note that since we don't know what the frame pointer for frame
// X is until we've looked at the caller of frame X, we actually end up
// stashing the info and pData pointers in the DebuggerFrameDat struct, and
// then invoking pCallback when we've moved up one level, into the caller's
// frame.  We use the needParentInfo field to indicate that the previous frame
// needed this (parental) info, and so when it's true we should invoke
// pCallback.
// What happens is this: if the previous frame set needParentInfo, then we
// do pCallback (and set needParentInfo to false).
// Then we look at the current frame - if it's frameless (ie,
// managed), then we set needParentInfo to callback in the next frame.
// Otherwise we must be at a chain boundary, and so we set the chain reason
// appropriately.  We then figure out what type of frame it is, setting
// flags depending on the type.  If the user should see this frame, then
// we'll set needParentInfo to record it's existence.  Lastly, if we're in
// a funky frame, we'll explicitly update the register set, since the
// CrawlFrame doesn't do it automatically.
//-----------------------------------------------------------------------------
StackWalkAction DebuggerWalkStackProc(CrawlFrame *pCF, void *data)
{
    DebuggerFrameData *d = (DebuggerFrameData *)data;

    if (pCF->IsNativeMarker())
    {
#ifdef FEATURE_EH_FUNCLETS
        // The tricky part here is that we want to skip all frames between a funclet method frame
        // and the parent method frame UNLESS the funclet is a filter.  Moreover, we should never
        // let a native marker execute the rest of this method, so we just short-circuit it here.
        if ((d->fpParent != LEAF_MOST_FRAME) || d->info.IsNonFilterFuncletFrame())
        {
            return SWA_CONTINUE;
        }
#endif // FEATURE_EH_FUNCLETS

        // This REGDISPLAY is for the native method immediately following the managed method for which
        // we have received the previous callback, i.e. the native caller of the last managed method
        // we have encountered.
        REGDISPLAY* pRDSrc = pCF->GetRegisterSet();
        d->BeginTrackingUMChain(GetSP(pRDSrc), pRDSrc);

        return SWA_CONTINUE;
    }

    // Note that a CrawlFrame may have both a methoddesc & an EE Frame.
    Frame *frame = g_pEEInterface->GetFrame(pCF);
    MethodDesc *md = pCF->GetFunction();

    LOG((LF_CORDB, LL_EVERYTHING, "Calling DebuggerWalkStackProc. Frame=0x%p, md=0x%p(%s), native_marker=%d\n",
        frame, md, (md == NULL || md == (MethodDesc*)POISONC) ? "null" : md->m_pszDebugMethodName, pCF->IsNativeMarker() ));

    // The fp for a frame must be obtained from the _next_ frame. Fill it in now for the previous frame, if appropriate.
    if (d->needParentInfo)
    {
        LOG((LF_CORDB, LL_INFO100000, "DWSP: NeedParentInfo.\n"));

        d->info.fp = GetFramePointerForDebugger(d, pCF);

#if defined(_DEBUG) && !defined(TARGET_ARM) && !defined(TARGET_ARM64)
        // Make sure the stackwalk is making progress.
		// On ARM this is invalid as the stack pointer does necessarily have to move when unwinding a frame.
        _ASSERTE(IsCloserToLeaf(d->previousFP, d->info.fp));

        d->previousFP = d->info.fp;
#endif // _DEBUG && !TARGET_ARM

        d->needParentInfo = false;

        {
            // Don't invoke Stubs if we're not asking for internal frames.
            bool fDoInvoke = true;
            if (!d->ShouldProvideInternalFrames())
            {
                if (d->info.HasStubFrame())
                {
                    fDoInvoke = false;
                }
            }

            LOG((LF_CORDB, LL_INFO1000000, "DWSP: handling our target\n"));

            if (fDoInvoke)
            {
                if (d->InvokeCallback(&d->info) == SWA_ABORT)
                {
                    return SWA_ABORT;
                }
            }

            // @todo - eventually we should be initing our frame-infos properly
            // and thus should be able to remove this.
            d->info.eStubFrameType = STUBFRAME_NONE;
        }
    } // if (d->needParentInfo)


#ifdef FEATURE_EH_FUNCLETS
    // The tricky part here is that we want to skip all frames between a funclet method frame
    // and the parent method frame UNLESS the funclet is a filter.  We only have to check for fpParent
    // here (instead of checking d->info.fIsFunclet and d->info.fIsFilter as well, as in the beginning of
    // this method) is because at this point, fpParent is already set by the code above.
    if (d->fpParent == LEAF_MOST_FRAME)
#endif // FEATURE_EH_FUNCLETS
    {
        // Track the UM chain after we flush any managed goo from the last iteration.
        if (TrackUMChain(pCF, d) == SWA_ABORT)
        {
            return SWA_ABORT;
        }
    }


    // Track if we want to send a callback for this Frame / Method
    bool use=false;

    //
    // Examine the frame.
    //

    // We assume that the stack walker is just updating the
    // register display we passed in - assert it to be sure
    _ASSERTE(pCF->GetRegisterSet() == &d->regDisplay);

#ifdef FEATURE_EH_FUNCLETS
    Frame* pPrevFrame = d->info.frame;

    // Here we need to determine if we are in a non-leaf frame, in which case we want to adjust the relative offset.
    // Also, we need to check if this frame has faulted (throws a native exception), since if it has, then it should be
    // considered the leaf frame (and thus we don't need to update the relative offset).
    if (pCF->IsActiveFrame() || pCF->HasFaulted())
    {
        d->info.fIsLeaf = true;
    }
    else if ( (pPrevFrame != NULL) &&
              (pPrevFrame->GetFrameType() == Frame::TYPE_EXIT) &&
              !HasExitRuntime(pPrevFrame, d, NULL) )
    {
        // This is for the inlined NDirectMethodFrameGeneric case.  We have not exit the runtime yet, so the current
        // frame should still be regarded as the leaf frame.
        d->info.fIsLeaf = true;
    }
    else
    {
        d->info.fIsLeaf = false;
    }

    d->info.fIsFunclet = pCF->IsFunclet();
    d->info.fIsFilter  = false;
    if (d->info.fIsFunclet)
    {
        d->info.fIsFilter = pCF->IsFilterFunclet();
    }

    if (pCF->IsFrameless())
    {
        // Check if we are skipping.
        if (d->fpParent != LEAF_MOST_FRAME)
        {
            // If fpParent is ROOT_MOST_FRAME, then we just need to skip one frame.  Otherwise, we should stop
            // skipping if the current frame pointer matches fpParent.  In either case, clear fpParent, and
            // then check again.
            if ((d->fpParent == ROOT_MOST_FRAME) ||
                ExceptionTracker::IsUnwoundToTargetParentFrame(pCF, ConvertFPToStackFrame(d->fpParent)))
            {
                LOG((LF_CORDB, LL_INFO100000, "DWSP: Stopping to skip funclet at 0x%p.\n", d->fpParent.GetSPValue()));

                d->fpParent = LEAF_MOST_FRAME;
                d->fpParent = CheckForParentFP(d->fpParent, pCF, d->info.IsNonFilterFuncletFrame());
            }
        }
    }

#endif // FEATURE_EH_FUNCLETS

    d->info.frame = frame;
    d->info.ambientSP = NULL;

    // Record the appdomain that the thread was in when it
    // was running code for this frame.
    d->info.currentAppDomain = AppDomain::GetCurrentDomain();

    //  Grab all the info from CrawlFrame that we need to
    //  check for "Am I in an exeption code blob?" now.

#ifdef FEATURE_EH_FUNCLETS
    // We are still searching for the parent of the last funclet we encounter.
    if (d->fpParent != LEAF_MOST_FRAME)
    {
        // We do nothing here.
        LOG((LF_CORDB, LL_INFO100000, "DWSP: Skipping to parent method frame at 0x%p.\n", d->fpParent.GetSPValue()));
    }
    else
#endif // FEATURE_EH_FUNCLETS
    // We ignore most IL stubs with no frames in our stackwalking. As exceptions
    // we will always report multicast stubs and the tailcall call target stubs
    // since we treat them specially in the debugger.
    if ((md != NULL) && md->IsILStub() && pCF->IsFrameless())
    {
        _ASSERTE(md->IsDynamicMethod());
        DynamicMethodDesc* dMD = md->AsDynamicMethodDesc();
#ifdef FEATURE_MULTICASTSTUB_AS_IL
        use |= dMD->IsMulticastStub();
#endif
        use |= dMD->GetILStubType() == DynamicMethodDesc::StubTailCallCallTarget;

        if (use)
        {
            d->info.managed = true;
            d->info.internal = false;
        }
        else
        {
            LOG((LF_CORDB, LL_INFO100000, "DWSP: Skip frameless IL stub.\n"));
        }
    }
    else
    // For frames w/o method data, send them as an internal stub frame.
    if ((md != NULL) && md->IsDynamicMethod())
    {
        // Only Send the frame if "InternalFrames" are requested.
        // Else completely ignore it.
        if (d->ShouldProvideInternalFrames())
        {
            d->info.InitForDynamicMethod(pCF);

            // We'll loop around to get the FramePointer. Only modification to FrameInfo
            // after this is filling in framepointer and resetting MD.
            use = true;
        }
    }
    else if (pCF->IsFrameless())
    {
        // Regular managed-method.
        LOG((LF_CORDB, LL_INFO100000, "DWSP: Is frameless.\n"));
        use = true;
        d->info.managed = true;
        d->info.internal = false;
        d->info.chainReason = CHAIN_NONE;
        d->needParentInfo = true; // Possibly need chain reason
        d->info.relOffset =  AdjustRelOffset(pCF, &(d->info));
        d->info.pIJM = pCF->GetJitManager();
        d->info.MethodToken = pCF->GetMethodToken();

#ifdef TARGET_X86
        // This is collecting the ambientSP a lot more than we actually need it. Only time we need it is
        // inspecting local vars that are based off the ambient esp.
        d->info.ambientSP = pCF->GetAmbientSPFromCrawlFrame();
#endif
    }
    else
    {
        d->info.pIJM = NULL;
        d->info.MethodToken = METHODTOKEN(NULL, 0);

        //
        // Retrieve any interception info
        //

        // Each interception type in the switch statement below is associated with a chain reason.
        // The other chain reasons are:
        // CHAIN_INTERCEPTION      - not used
        // CHAIN_PROCESS_START     - not used
        // CHAIN_THREAD_START      - thread start
        // CHAIN_ENTER_MANAGED     - managed chain
        // CHAIN_ENTER_UNMANAGED   - unmanaged chain
        // CHAIN_DEBUGGER_EVAL     - not used
        // CHAIN_CONTEXT_SWITCH    - not used
        // CHAIN_FUNC_EVAL         - funceval

        switch (frame->GetInterception())
        {
        case Frame::INTERCEPTION_CLASS_INIT:
            //
            // Fall through
            //

        // V2 assumes that the only thing the prestub intercepts is the class constructor
        case Frame::INTERCEPTION_PRESTUB:
            d->info.chainReason = CHAIN_CLASS_INIT;
            break;

        case Frame::INTERCEPTION_EXCEPTION:
            d->info.chainReason = CHAIN_EXCEPTION_FILTER;
            break;

        case Frame::INTERCEPTION_CONTEXT:
            d->info.chainReason = CHAIN_CONTEXT_POLICY;
            break;

        case Frame::INTERCEPTION_SECURITY:
            d->info.chainReason = CHAIN_SECURITY;
            break;

        default:
            d->info.chainReason = CHAIN_NONE;
        }

        //
        // Look at the frame type to figure out how to treat it.
        //

        LOG((LF_CORDB, LL_INFO100000, "DWSP: Chain reason is 0x%X.\n", d->info.chainReason));

        switch (frame->GetFrameType())
        {
        case Frame::TYPE_ENTRY: // We now ignore entry + exit frames.
        case Frame::TYPE_EXIT:
        case Frame::TYPE_HELPER_METHOD_FRAME:
        case Frame::TYPE_INTERNAL:

            /* If we have a specific interception type, use it. However, if this
               is the top-most frame (with a specific type), we can ignore it
               and it wont appear in the stack-trace */
#define INTERNAL_FRAME_ACTION(d, use)   \
    (d)->info.managed = true;           \
    (d)->info.internal = false;         \
    use = true

            LOG((LF_CORDB, LL_INFO100000, "DWSP: Frame type is TYPE_INTERNAL.\n"));
            if (d->info.chainReason == CHAIN_NONE || pCF->IsActiveFrame())
            {
                use = false;
            }
            else
            {
                INTERNAL_FRAME_ACTION(d, use);
            }
            break;

        case Frame::TYPE_INTERCEPTION:
        case Frame::TYPE_SECURITY: // Security is a sub-type of interception
            LOG((LF_CORDB, LL_INFO100000, "DWSP: Frame type is TYPE_INTERCEPTION/TYPE_SECURITY.\n"));
            d->info.managed = true;
            d->info.internal = true;
            use = true;
            break;

        case Frame::TYPE_CALL:
            LOG((LF_CORDB, LL_INFO100000, "DWSP: Frame type is TYPE_CALL.\n"));
            // In V4, StubDispatchFrame is only used on 64-bit (and PPC?) but not on x86.  x86 uses a
            // different code path which sets up a HelperMethodFrame instead.  In V4.5, x86 and ARM
            // both use the 64-bit code path and they set up a StubDispatchFrame as well.  This causes
            // a problem in the debugger stackwalker (see Dev11 Issue 13229) since the two frame types
            // are treated differently.  More specifically, a StubDispatchFrame causes the debugger
            // stackwalk to make an invalid callback, i.e. a callback which is not for a managed method,
            // an explicit frame, or a chain.
            //
            // Ideally we would just change the StubDispatchFrame to behave like a HMF, but it's
            // too big of a change for an in-place release.  For now I'm just making surgical fixes in
            // the debugger stackwalker.  This may introduce behavioural changes in on X64, but the
            // chance of that is really small.  StubDispatchFrame is only used in the virtual stub
            // disptch code path.  It stays on the stack in a small time window and it's not likely to
            // be on the stack while some managed methods closer to the leaf are on the stack.  There is
            // only one scenario I know of, and that's the repro for Dev11 13229, but that's for x86 only.
            // The jitted code on X64 behaves differently.
            //
            // Note that there is a corresponding change in DacDbiInterfaceImpl::GetInternalFrameType().
            if (frame->GetVTablePtr() == StubDispatchFrame::GetMethodFrameVPtr())
            {
                use = false;
            }
            else
            {
                d->info.managed = true;
                d->info.internal = false;
                use = true;
            }
            break;

        case Frame::TYPE_FUNC_EVAL:
            LOG((LF_CORDB, LL_INFO100000, "DWSP: Frame type is TYPE_FUNC_EVAL.\n"));
            d->info.managed = true;
            d->info.internal = true;
            // This is actually a nop.  We reset the chain reason in InitForFuncEval() below.
            // So is a FuncEvalFrame a chain or an internal frame?
            d->info.chainReason = CHAIN_FUNC_EVAL;

            {
                // We only show a FuncEvalFrame if the funceval is not trying to abort the thread.
                FuncEvalFrame *pFuncEvalFrame = static_cast<FuncEvalFrame *>(frame);
                use = pFuncEvalFrame->ShowFrame() ? true : false;
            }

            // Send Internal frame. This is "inside" (leafmost) the chain, so we send it first
            // since sending starts from the leaf.
            if (use && d->ShouldProvideInternalFrames())
            {
                FrameInfo f;
                f.InitForFuncEval(pCF);
                if (d->InvokeCallback(&f) == SWA_ABORT)
                {
                    return SWA_ABORT;
                }
            }

            break;

        // Put frames we want to ignore here:
        case Frame::TYPE_MULTICAST:
            LOG((LF_CORDB, LL_INFO100000, "DWSP: Frame type is TYPE_MULTICAST.\n"));
            if (d->ShouldIgnoreNonmethodFrames())
            {
                // Multicast frames exist only to gc protect the arguments
                // between invocations of a delegate.  They don't have code that
                // we can (currently) show the user (we could change this with
                // work, but why bother?  It's an internal stub, and even if the
                // user could see it, they can't modify it).
                LOG((LF_CORDB, LL_INFO100000, "DWSP: Skipping frame 0x%x b/c it's "
                    "a multicast frame!\n", frame));
                use = false;
            }
            else
            {
                LOG((LF_CORDB, LL_INFO100000, "DWSP: NOT Skipping frame 0x%x even thought it's "
                    "a multicast frame!\n", frame));
                INTERNAL_FRAME_ACTION(d, use);
            }
            break;

        default:
            _ASSERTE(!"Invalid frame type!");
            break;
        }
    }


    // Check for ICorDebugInternalFrame stuff.
    // These callbacks are dispatched out of band.
    if (d->ShouldProvideInternalFrames() && (frame != NULL) && (frame != FRAME_TOP))
    {
        Frame::ETransitionType t = frame->GetTransitionType();
        FrameInfo f;
        bool fUse = false;

        if (t == Frame::TT_U2M)
        {
            // We can invoke the Internal U2M frame now.
            f.InitForU2MInternalFrame(pCF);
            fUse = true;
        }
        else if (t == Frame::TT_AppDomain)
        {
            // Internal frame for an Appdomain transition.
            // We used to ignore frames for ADs which we hadn't sent a Create event for yet.  In V3 we send AppDomain
            // create events immediately (before any assemblies are loaded), so this should no longer be an issue.
            f.InitForADTransition(pCF);
            fUse = true;
        }

        // Frame's setup. Now invoke the callback.
        if (fUse)
        {
            if (d->InvokeCallback(&f) == SWA_ABORT)
            {
                return SWA_ABORT;
            }
        }
    } // should we give frames?



    if (use)
    {
        //
        // If we are returning a complete stack walk from the helper thread, then we
        // need to gather information to instantiate generics.  However, a stepper doing
        // a stackwalk does not need this information, so skip in that case.
        //
        if (d->ShouldIgnoreNonmethodFrames())
        {
            // Finding sizes of value types on the argument stack while
            // looking for the arg runs the class loader in non-load mode.
            ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();
            d->info.exactGenericArgsToken = pCF->GetExactGenericArgsToken();
        }
        else
        {
            d->info.exactGenericArgsToken = NULL;
        }

        d->info.md = md;
        CopyREGDISPLAY(&(d->info.registers), &(d->regDisplay));

#if defined(TARGET_AMD64)
        LOG((LF_CORDB, LL_INFO100000, "DWSP: Saving REGDISPLAY with sp = 0x%p, pc = 0x%p.\n",
            GetRegdisplaySP(&(d->info.registers)),
            GetControlPC(&(d->info.registers))));
#endif // TARGET_AMD64

        d->needParentInfo = true;
        LOG((LF_CORDB, LL_INFO100000, "DWSP: Setting needParentInfo\n"));
    }

#if defined(FEATURE_EH_FUNCLETS)
    d->fpParent = CheckForParentFP(d->fpParent, pCF, d->info.IsNonFilterFuncletFrame());
#endif // FEATURE_EH_FUNCLETS

    //
    // The stackwalker doesn't update the register set for the
    // case where a non-frameless frame is returning to another
    // non-frameless frame.  Cover this case.
    //
    // !!! This assumes that updating the register set multiple times
    // for a given frame times is not a bad thing...
    //
    if (!pCF->IsFrameless())
    {
        LOG((LF_CORDB, LL_INFO100000, "DWSP: updating regdisplay.\n"));
        pCF->GetFrame()->UpdateRegDisplay(&d->regDisplay);
    }

    return SWA_CONTINUE;
}

#if defined(TARGET_X86) && defined(FEATURE_INTEROP_DEBUGGING)
// Helper to get the Wait-Sleep-Join bit from the thread
bool IsInWaitSleepJoin(Thread * pThread)
{
    // Partial User state is sufficient because that has the bit we're checking against.
    CorDebugUserState cts = g_pEEInterface->GetPartialUserState(pThread);
    return ((cts & USER_WAIT_SLEEP_JOIN) != 0);
}

//-----------------------------------------------------------------------------
// Decide if we should send an UM leaf chain.
// This goes through a bunch of heuristics.
// The driving guidelines here are:
// - we try not to send an UM chain if it's just internal mscorwks stuff
//   and we know it can't have native user code.
//   (ex, anything beyond a filter context, various hijacks, etc).
// - If it may have native user code, we send it anyway.
//-----------------------------------------------------------------------------
bool ShouldSendUMLeafChain(Thread * pThread)
{
    // If we're in shutodown, don't bother trying to sniff for an UM leaf chain.
    // @todo - we'd like to never even be trying to stack trace on shutdown, this
    // comes up when we do helper thread duty on shutdown.
    if (g_fProcessDetach)
    {
        return false;
    }

    if (pThread->IsUnstarted() || pThread->IsDead())
    {
        return false;
    }

    // If a thread is suspended for sync purposes, it was suspended from managed
    // code and the only native code is a mscorwks hijack.
    // There are a few caveats here:
    // - This means a thread will lose it's UM chain. But what if a user inactive thread
    // enters the CLR from native code and hits a GC toggle? We'll lose that entire
    // UM chain.
    // - at a managed-only stop, preemptive threads are still live. Thus a thread
    // may not have this state set, run a little, try to enter the GC, and then get
    // this state set. Thus we'll lose the UM chain right out from under our noses.
    Thread::ThreadState ts = pThread->GetSnapshotState();
    if ((ts & Thread::TS_SyncSuspended) != 0)
    {
        // If we've been stopped inside the runtime (eg, at a gc-toggle) but
        // not actually at a stopping context, then the thread must have some
        // leafframes in mscorwks.
        // We can detect this case by checking if GetManagedStoppedCtx(pThread) == NULL.
        // This is very significant for notifcations (like LogMessage) that are
        // dispatches from within mscorwks w/o a filter context.
        // We don't send a UM chain for these cases because that would
        // cause managed debug events to be dispatched w/ UM chains on the callstack.
        // And that just seems wrong ...

        return false;
    }

#ifdef FEATURE_HIJACK
    if ((ts & Thread::TS_Hijacked) != 0)
    {
        return false;
    }
#endif

    // This is pretty subjective. If we have a thread stopped in a managed sleep,
    // managed wait, or managed join, then don't bother showing the native end of the
    // stack. This check can be removed w/o impacting correctness.
    // @todo - may be a problem if Sleep/Wait/Join go through a hosting interface
    // which lands us in native user code.
    // Partial User state is sufficient because that has the bit we're checking against.
    if (IsInWaitSleepJoin(pThread))
    {
        return false;
    }

    // If we're tracing ourselves, we must be in managed code.
    // Native user code can't initiate a managed stackwalk.
    if (pThread == GetThreadNULLOk())
    {
        return false;
    }

    return true;
}

//-----------------------------------------------------------------------------
// Prepare a Leaf UM chain. This assumes we should send an UM leaf chain.
// Returns true if we actually prep for an UM leaf,
// false if we don't.
//-----------------------------------------------------------------------------
bool PrepareLeafUMChain(DebuggerFrameData * pData, CONTEXT * pCtxTemp)
{
    // Get the current user context (depends on if we're the active thread or not).
    Thread * thread = pData->GetThread();
    REGDISPLAY * pRDSrc = NULL;
    REGDISPLAY rdTemp;


#ifdef _DEBUG
    // Anybody stopped at an native debug event (and hijacked) should have a filter ctx.
    if (thread->GetInteropDebuggingHijacked() && (thread->GetFrame() != NULL) && (thread->GetFrame() != FRAME_TOP))
    {
        _ASSERTE(g_pEEInterface->GetThreadFilterContext(thread) != NULL);
    }
#endif

    // If we're hijacked, then we assume we're in native code. This covers the active thread case.
    if (g_pEEInterface->GetThreadFilterContext(thread) != NULL)
    {
        LOG((LF_CORDB, LL_EVERYTHING, "DWS - sending special case UM Chain.\n"));

        // This will get it from the filter ctx.
        pRDSrc = &(pData->regDisplay);
    }
    else
    {
        // For inactive thread, we may not be hijacked. So just get the current ctx.
        // This will use a filter ctx if we have one.
        // We may suspend a thread in native code w/o hijacking it, so it's still at it's live context.
        // This can happen when we get a debug event on 1 thread; and then switch to look at another thread.
        // This is very common when debugging apps w/ cross-thread causality (including COM STA objects)
        pRDSrc = &rdTemp;

        bool fOk;


        // We need to get thread's context (InitRegDisplay will do that under the covers).
        // If this is our thread, we're in bad shape. Fortunately that should never happen.
        _ASSERTE(thread != GetThreadNULLOk());

        Thread::SuspendThreadResult str = thread->SuspendThread();
        if (str != Thread::STR_Success)
        {
            return false;
        }

        // @todo - this context is less important because the RS will overwrite it with the live context.
        // We don't need to even bother getting it. We can just intialize the regdisplay w/ a sentinal.
        fOk = g_pEEInterface->InitRegDisplay(thread, pRDSrc, pCtxTemp, false);
        thread->ResumeThread();

        if (!fOk)
        {
            return false;
        }
    }

    // By now we have a Regdisplay from somewhere (filter ctx, current ctx, etc).
    _ASSERTE(pRDSrc != NULL);

    // If we're stopped in mscorwks (b/c of a handler for a managed BP), then the filter ctx will
    // still be set out in jitted code.
    // If our regdisplay is out in UM code , then send a UM chain.
    BYTE* ip = (BYTE*) GetControlPC(pRDSrc);
    if (g_pEEInterface->IsManagedNativeCode(ip))
    {
        return false;
    }

    LOG((LF_CORDB, LL_EVERYTHING, "DWS - sending leaf UM Chain.\n"));

    // Get the ending fp. We may not have any managed goo on the stack (eg, native thread called
    // into a managed method and then returned from it).
    FramePointer fpRoot;
    Frame * pFrame = thread->GetFrame();
    if ((pFrame != NULL) && (pFrame != FRAME_TOP))
    {
        fpRoot = FramePointer::MakeFramePointer((void*) pFrame);
    }
    else
    {
        fpRoot= ROOT_MOST_FRAME;
    }


    // Start tracking an UM chain. We won't actually send the UM chain until
    // we hit managed code. Since this is the leaf, we don't need to send an
    // Enter-Managed chain either.
    pData->BeginTrackingUMChain(fpRoot, pRDSrc);

    return true;
}
#endif //  defined(TARGET_X86) && defined(FEATURE_INTEROP_DEBUGGING)

//-----------------------------------------------------------------------------
// Entry function for the debugger's stackwalking layer.
// This will invoke pCallback(FrameInfo * pInfo, pData) for each 'frame'
//-----------------------------------------------------------------------------
StackWalkAction DebuggerWalkStack(Thread *thread,
                                  FramePointer targetFP,
                                  CONTEXT *context,
                                  BOOL contextValid,
                                  DebuggerStackCallback pCallback,
                                  void *pData,
                                  BOOL fIgnoreNonmethodFrames)
{
    _ASSERTE(context != NULL);

    DebuggerFrameData data;

    StackWalkAction result = SWA_CONTINUE;
    bool fRegInit = false;

    LOG((LF_CORDB, LL_EVERYTHING, "DebuggerWalkStack called\n"));

    if(contextValid || g_pEEInterface->GetThreadFilterContext(thread) != NULL)
    {
        fRegInit = g_pEEInterface->InitRegDisplay(thread, &data.regDisplay, context, contextValid != 0);
        _ASSERTE(fRegInit);
    }

    if (!fRegInit)
    {
#if defined(CONTEXT_EXTENDED_REGISTERS)

            // Note: the size of a CONTEXT record contains the extended registers, but the context pointer we're given
            // here may not have room for them. Therefore, we only set the non-extended part of the context to 0.
            memset((void *)context, 0, offsetof(CONTEXT, ExtendedRegisters));
#else
            memset((void *)context, 0, sizeof(CONTEXT));
#endif
            memset((void *)&data, 0, sizeof(data));

#if !defined(FEATURE_EH_FUNCLETS)
            // @todo - this seems pointless. context->Eip will be 0; and when we copy it over to the DebuggerRD,
            // the context will be completely null.
            data.regDisplay.ControlPC = context->Eip;
            data.regDisplay.PCTAddr = (TADDR)&(context->Eip);

#else
            //
            // @TODO: this should be the code for all platforms now that it uses FillRegDisplay,
            // which encapsulates the platform variances.  This could all be avoided if we used
            // StackWalkFrames instead of StackWalkFramesEx.
            //
            ::SetIP(context, 0);
            ::SetSP(context, 0);
            FillRegDisplay(&data.regDisplay, context);

            ::SetSP(data.regDisplay.pCallerContext, 0);
#endif
    }

    data.Init(thread, targetFP, fIgnoreNonmethodFrames, pCallback, pData);


#if defined(TARGET_X86) && defined(FEATURE_INTEROP_DEBUGGING)
    CONTEXT ctxTemp; // Temp context for Leaf UM chain. Need it here so that it stays alive for whole stackwalk.

    // Important case for Interop Debugging -
    // We may be stopped in Native Code (perhaps at a BP) w/ no Transition frame on the stack!
    // We still need to send an UM Chain for this case.
    if (ShouldSendUMLeafChain(thread))
    {
        // It's possible this may fail (eg, GetContext fails on win9x), so we're not guaranteed
        // to be sending an UM chain even though we want to.
        PrepareLeafUMChain(&data, &ctxTemp);

    }
#endif // defined(TARGET_X86) && defined(FEATURE_INTEROP_DEBUGGING)

    if ((result != SWA_FAILED) && !thread->IsUnstarted() && !thread->IsDead())
    {
        int flags = 0;

        result = g_pEEInterface->StackWalkFramesEx(thread, &data.regDisplay,
                                                   DebuggerWalkStackProc,
                                                   &data,
                                                   flags | HANDLESKIPPEDFRAMES | NOTIFY_ON_U2M_TRANSITIONS |
                                                   ALLOW_ASYNC_STACK_WALK | SKIP_GSCOOKIE_CHECK);
    }
    else
    {
        result = SWA_DONE;
    }

    if (result == SWA_DONE || result == SWA_FAILED) // SWA_FAILED if no frames
    {
        // Since Debugger StackWalk callbacks are delayed 1 frame from EE stackwalk callbacks, we
        // have to touch up the 1 leftover here.
        //
        // This is safe only because we use the REGDISPLAY of the native marker callback for any subsequent
        // explicit frames which do not update the REGDISPLAY.  It's kind of fragile.  If we can change
        // the x86 real stackwalker to unwind one frame ahead of time, we can get rid of this code.
        if (data.needParentInfo)
        {
            data.info.fp = GetFramePointerForDebugger(&data, NULL);

            if (data.InvokeCallback(&data.info) == SWA_ABORT)
            {
                return SWA_ABORT;
            }
        }

        //
        // Top off the stack trace as necessary w/ a thread-start chain.
        //
        REGDISPLAY * pRegDisplay = &(data.regDisplay);
        if (data.IsTrackingUMChain())
        {
            // This is the common case b/c managed code gets called from native code.
            pRegDisplay = data.GetUMChainStartRD();
        }


        // All Thread starts in unmanaged code (at something like kernel32!BaseThreadStart),
        // so all ThreadStart chains must be unmanaged.
        // InvokeCallback will fabricate the EnterManaged chain if we haven't already sent one.
        data.info.InitForThreadStart(thread, pRegDisplay);
        result = data.InvokeCallback(&data.info);

    }
    return result;
}
