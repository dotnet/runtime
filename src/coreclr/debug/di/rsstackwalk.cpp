// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// RsStackWalk.cpp
//

//
// This file contains the implementation of the V3 managed stackwalking API.
//
// ======================================================================================

#include "stdafx.h"
#include "primitives.h"


//---------------------------------------------------------------------------------------
//
// Constructor for CordbStackWalk.
//
// Arguments:
//    pCordbThread - the thread on which this stackwalker is created
//

CordbStackWalk::CordbStackWalk(CordbThread * pCordbThread)
  : CordbBase(pCordbThread->GetProcess(), 0, enumCordbStackWalk),
    m_pCordbThread(pCordbThread),
    m_pSFIHandle(NULL),
    m_cachedSetContextFlag(SET_CONTEXT_FLAG_ACTIVE_FRAME),
    m_cachedHR(S_OK),
    m_fIsOneFrameAhead(false)
{
    m_pCachedFrame.Clear();
}

void CordbStackWalk::Init()
{
    CordbProcess * pProcess = GetProcess();
    m_lastSyncFlushCounter = pProcess->m_flushCounter;

    IDacDbiInterface * pDAC = pProcess->GetDAC();
    pDAC->CreateStackWalk(m_pCordbThread->m_vmThreadToken,
                          &m_context,
                          &m_pSFIHandle);

    // see the function header of code:CordbStackWalk::CheckForLegacyHijackCase
    CheckForLegacyHijackCase();

    // Add itself to the neuter list.
    m_pCordbThread->GetRefreshStackNeuterList()->Add(GetProcess(), this);
}

// ----------------------------------------------------------------------------
// CordbStackWalk::CheckForLegacyHijackCase
//
// Description:
// @dbgtodo  legacy interop debugging - In the case of an unhandled hardware exception, the
// thread will be hijacked to code:Debugger::GenericHijackFunc, which the stackwalker doesn't know how to
// unwind. We can teach the stackwalker to recognize that hijack stub, but since it's going to be deprecated
// anyway, it's not worth the effort. So we check for the hijack CONTEXT here and use it as the CONTEXT. This
// check should be removed when we are completely
// out-of-process.
//

void CordbStackWalk::CheckForLegacyHijackCase()
{
#if defined(FEATURE_INTEROP_DEBUGGING)
    CordbProcess * pProcess = GetProcess();

    // Only do this if we have a shim and we are interop-debugging.
    if ((pProcess->GetShim() != NULL) &&
        pProcess->IsInteropDebugging())
    {
        // And only if we have a CordbUnmanagedThread and we are hijacked to code:Debugger::GenericHijackFunc
        CordbUnmanagedThread * pUT = pProcess->GetUnmanagedThread(m_pCordbThread->GetVolatileOSThreadID());
        if (pUT != NULL)
        {
            if (pUT->IsFirstChanceHijacked() || pUT->IsGenericHijacked())
            {
                // The GetThreadContext function hides the effects of hijacking and returns the unhijacked context
                m_context.ContextFlags = DT_CONTEXT_FULL;
                pUT->GetThreadContext(&m_context);
                IDacDbiInterface * pDAC = GetProcess()->GetDAC();
                pDAC->SetStackWalkCurrentContext(m_pCordbThread->m_vmThreadToken,
                                                 m_pSFIHandle,
                                                 SET_CONTEXT_FLAG_ACTIVE_FRAME,
                                                 &m_context);
            }
        }
    }
#endif // FEATURE_INTEROP_DEBUGGING
}

//---------------------------------------------------------------------------------------
//
// Destructor for CordbStackWalk.
//
// Notes:
//    We don't really need to do anything here since the CordbStackWalk should have been neutered already.
//

CordbStackWalk::~CordbStackWalk()
{
    _ASSERTE(IsNeutered());
}

//---------------------------------------------------------------------------------------
//
// This function resets all the state on a CordbStackWalk and releases all the memory.
// It is used for neutering and refreshing.
//

void CordbStackWalk::DeleteAll()
{
    _ASSERTE(GetProcess()->GetProcessLock()->HasLock());

    // delete allocated memory
    if (m_pSFIHandle)
    {
        HRESULT hr = S_OK;
        EX_TRY
        {
#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
            // For Mac debugging, it's not safe to call into the DAC once
            // code:INativeEventPipeline::TerminateProcess is called.  This is because the transport will not
            // work anymore.  The sole purpose of calling DeleteStackWalk() is to release the resources and
            // memory allocated for the stackwalk.  In the remote debugging case, the memory is allocated in
            // the debuggee process.  If the process is already terminated, then it's ok to skip the call.
            if (!GetProcess()->m_exiting)
#endif // FEATURE_DBGIPC_TRANSPORT_DI
            {
                // This Delete call shouldn't actually throw. Worst case, the DDImpl leaked memory.
                GetProcess()->GetDAC()->DeleteStackWalk(m_pSFIHandle);
            }
        }
        EX_CATCH_HRESULT(hr);
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
        m_pSFIHandle = NULL;
    }

    // clear out the cached frame
    m_pCachedFrame.Clear();
    m_cachedHR = S_OK;
    m_fIsOneFrameAhead = false;
}

//---------------------------------------------------------------------------------------
//
// Release all memory used by the stackwalker.
//
//
// Notes:
//    CordbStackWalk is neutered by CordbThread or CleanupStack().
//

void CordbStackWalk::Neuter()
{
    if (IsNeutered())
    {
        return;
    }

    DeleteAll();
    CordbBase::Neuter();
}

// standard QI function
HRESULT CordbStackWalk::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugStackWalk)
    {
        *pInterface = static_cast<ICorDebugStackWalk*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugStackWalk*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Refreshes all the state stored on the CordbStackWalk.  This is necessary because sending IPC events to
// the LS flushes the DAC cache, and m_pSFIHandle is allocated entirely in DAC memory.  So, we keep track
// of whether we have sent an IPC event and refresh the CordbStackWalk if necessary.
//
// Notes:
//    Throws on error.
//

void CordbStackWalk::RefreshIfNeeded()
{
    CordbProcess * pProcess = GetProcess();
    _ASSERTE(pProcess->GetProcessLock()->HasLock());

    // check if we need to refresh
    if (m_lastSyncFlushCounter != pProcess->m_flushCounter)
    {
        // Make a local copy of the CONTEXT here.  DeleteAll() will delete the CONTEXT on the cached frame,
        // and CreateStackWalk() actually uses the CONTEXT buffer we pass to it.
        DT_CONTEXT ctx;
        if (m_fIsOneFrameAhead)
        {
            ctx = *(m_pCachedFrame->GetContext());
        }
        else
        {
            ctx = m_context;
        }

        // clear all the state
        DeleteAll();

        // create a new stackwalk handle
        pProcess->GetDAC()->CreateStackWalk(m_pCordbThread->m_vmThreadToken,
                                            &m_context,
                                            &m_pSFIHandle);

        // advance the stackwalker to where we originally were
        SetContextWorker(m_cachedSetContextFlag, sizeof(DT_CONTEXT), reinterpret_cast<BYTE *>(&ctx));

        // update the sync counter
        m_lastSyncFlushCounter = pProcess->m_flushCounter;
    }
} // CordbStackWalk::RefreshIfNeeded()

//---------------------------------------------------------------------------------------
//
// Retrieves the CONTEXT of the current frame.
//
// Arguments:
//    contextFlags   - context flags used to determine the required size for the buffer
//    contextBufSize - size of the CONTEXT buffer
//    pContextSize   - out parameter; returns the size required for the CONTEXT buffer
//    pbContextBuf   - the CONTEXT buffer
//
// Return Value:
//    Return S_OK on success.
//    Return CORDBG_E_PAST_END_OF_STACK if we are already at the end of the stack.
//    Return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) if the buffer is too small.
//    Return E_FAIL on other failures.
//

HRESULT CordbStackWalk::GetContext(ULONG32   contextFlags,
                                   ULONG32   contextBufSize,
                                   ULONG32 * pContextSize,
                                   BYTE      pbContextBuf[])
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        RefreshIfNeeded();

        // set the required size for the CONTEXT buffer
        if (pContextSize != NULL)
        {
            *pContextSize = ContextSizeForFlags(contextFlags);
        }

        // If all the user wants to know is the CONTEXT size, then we are done.
        if ((contextBufSize != 0) && (pbContextBuf != NULL))
        {
            if (contextBufSize < 4)
            {
                ThrowWin32(ERROR_INSUFFICIENT_BUFFER);
            }

            DT_CONTEXT * pContext = reinterpret_cast<DT_CONTEXT *>(pbContextBuf);

            // Some helper functions that examine the context expect the flags to be initialized.
            pContext->ContextFlags = contextFlags;

            // check the size of the incoming buffer
            if (!CheckContextSizeForBuffer(contextBufSize, pbContextBuf))
            {
                ThrowWin32(ERROR_INSUFFICIENT_BUFFER);
            }

            // Check if we are one frame ahead.  If so, returned the CONTEXT on the cached frame.
            if (m_fIsOneFrameAhead)
            {
                if (m_pCachedFrame != NULL)
                {
                    const DT_CONTEXT * pSrcContext = m_pCachedFrame->GetContext();
                    _ASSERTE(pSrcContext);
                    CORDbgCopyThreadContext(pContext, pSrcContext);
                }
                else
                {
                    // We encountered a problem when we were trying to initialize the CordbNativeFrame.
                    // However, the problem occurred after we have unwound the current frame.
                    // What do we do here?  We don't have the CONTEXT anymore.
                    _ASSERTE(FAILED(m_cachedHR));
                    ThrowHR(m_cachedHR);
                }
            }
            else
            {
                // No easy way out in this case.  We have to call the DDI.
                IDacDbiInterface * pDAC = GetProcess()->GetDAC();

                IDacDbiInterface::FrameType ft = pDAC->GetStackWalkCurrentFrameInfo(m_pSFIHandle, NULL);
                if (ft == IDacDbiInterface::kInvalid)
                {
                    ThrowHR(E_FAIL);
                }
                else if (ft == IDacDbiInterface::kAtEndOfStack)
                {
                    ThrowHR(CORDBG_E_PAST_END_OF_STACK);
                }
                else if (ft == IDacDbiInterface::kExplicitFrame)
                {
                    ThrowHR(CORDBG_E_NO_CONTEXT_FOR_INTERNAL_FRAME);
                }
                else
                {
                    // We always store the current CONTEXT, so just copy it into the buffer.
                    CORDbgCopyThreadContext(pContext, &m_context);
                }
            }
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Set the stackwalker to the specified CONTEXT.
//
// Arguments:
//    flag        - context flags used to determine the size of the CONTEXT
//    contextSize - the size of the CONTEXT
//    context     - the CONTEXT as a byte array
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if context is NULL
//    Return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) if the CONTEXT is too small.
//    Return E_FAIL on other failures.
//

HRESULT CordbStackWalk::SetContext(CorDebugSetContextFlag flag, ULONG32 contextSize, BYTE context[])
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        RefreshIfNeeded();
        SetContextWorker(flag, contextSize, context);
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Refer to the comment for code:CordbStackWalk::SetContext
//

void CordbStackWalk::SetContextWorker(CorDebugSetContextFlag flag, ULONG32 contextSize, BYTE context[])
{
    if (context == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }

    if (!CheckContextSizeForBuffer(contextSize, context))
    {
        ThrowWin32(ERROR_INSUFFICIENT_BUFFER);
    }

    // invalidate the cache
    m_pCachedFrame.Clear();
    m_cachedHR = S_OK;
    m_fIsOneFrameAhead = false;

    DT_CONTEXT * pSrcContext = reinterpret_cast<DT_CONTEXT *>(context);

    // Check the incoming CONTEXT using a temporary CONTEXT buffer before updating our real CONTEXT buffer.
    // The incoming CONTEXT is not required to have all the bits set in its CONTEXT flags, so only update
    // the registers specified by the CONTEXT flags.  Note that CORDbgCopyThreadContext() honours the CONTEXT
    // flags on both the source and the destination CONTEXTs when it copies them.
    DT_CONTEXT tmpCtx = m_context;
    tmpCtx.ContextFlags |= pSrcContext->ContextFlags;
    CORDbgCopyThreadContext(&tmpCtx, pSrcContext);

    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    IfFailThrow(pDAC->CheckContext(m_pCordbThread->m_vmThreadToken, &tmpCtx));

    // At this point we have done all of our checks to verify that the incoming CONTEXT is sane, so we can
    // update our internal CONTEXT buffer.
    m_context = tmpCtx;
    m_cachedSetContextFlag = flag;

    pDAC->SetStackWalkCurrentContext(m_pCordbThread->m_vmThreadToken,
                                     m_pSFIHandle,
                                     flag,
                                     &m_context);
}

//---------------------------------------------------------------------------------------
//
// Helper to perform all the necessary operations when we unwind, including:
//     1) Unwind
//     2) Save the new unwound CONTEXT
//
// Return Value:
//    Return TRUE if we successfully unwind to the next frame.
//    Return FALSE if there is no more frame to walk.
//    Throw on error.
//

BOOL CordbStackWalk::UnwindStackFrame()
{
    CordbProcess * pProcess = GetProcess();
    _ASSERTE(pProcess->GetProcessLock()->HasLock());

    IDacDbiInterface * pDAC = pProcess->GetDAC();
    BOOL retVal = pDAC->UnwindStackWalkFrame(m_pSFIHandle);

    // Now that we have unwound, make sure we update the CONTEXT buffer to reflect the current stack frame.
    // This call is safe regardless of whether the unwind is successful or not.
    pDAC->GetStackWalkCurrentContext(m_pSFIHandle, &m_context);

    return retVal;
} // CordbStackWalk::UnwindStackWalkFrame

//---------------------------------------------------------------------------------------
//
// Unwind the stackwalker to the next frame.
//
// Return Value:
//    Return S_OK on success.
//    Return CORDBG_E_FAIL_TO_UNWIND_FRAME if the unwind fails.
//    Return CORDBG_S_AT_END_OF_STACK if we have reached the end of the stack as a result of this unwind.
//    Return CORDBG_E_PAST_END_OF_STACK if we are already at the end of the stack to begin with.
//

HRESULT CordbStackWalk::Next()
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        RefreshIfNeeded();
        if (m_fIsOneFrameAhead)
        {
            // We have already unwound to the next frame when we materialize the CordbNativeFrame
            // for the current frame.  So we just need to clear the cache because we are already at
            // the next frame.
            if (m_pCachedFrame != NULL)
            {
                m_pCachedFrame.Clear();
            }
            m_cachedHR = S_OK;
            m_fIsOneFrameAhead = false;
        }
        else
        {
            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            IDacDbiInterface::FrameType ft = IDacDbiInterface::kInvalid;

            ft = pDAC->GetStackWalkCurrentFrameInfo(this->m_pSFIHandle, NULL);
            if (ft == IDacDbiInterface::kAtEndOfStack)
            {
                ThrowHR(CORDBG_E_PAST_END_OF_STACK);
            }

            // update the cahced flag to indicate that we have reached an unwind CONTEXT
            m_cachedSetContextFlag = SET_CONTEXT_FLAG_UNWIND_FRAME;

            if (UnwindStackFrame())
            {
                hr = S_OK;
            }
            else
            {
                hr = CORDBG_S_AT_END_OF_STACK;
            }
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Retrieves an ICDFrame corresponding to the current frame:
// Stopped At           Out Parameter       Return Value
// ----------           -------------       ------------
// explicit frame       CordbInternalFrame  S_OK
// managed stack frame  CordbNativeFrame    S_OK
// native stack frame   NULL                S_FALSE
//
// Arguments:
//    ppFrame - out parameter; return the ICDFrame
//
// Return Value:
//    On success return the HRs above.
//    Return CORDBG_E_PAST_END_OF_STACK if we are already at the end of the stack.
//    Return E_INVALIDARG if ppFrame is NULL
//    Return E_FAIL on other errors.
//
// Notes:
//    This is just a wrapper with an EX_TRY/EX_CATCH_HRESULT for GetFrameWorker().
//

HRESULT CordbStackWalk::GetFrame(ICorDebugFrame ** ppFrame)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_NO_LOCK_BEGIN(this)
    {
        ATT_REQUIRE_STOPPED_MAY_FAIL_OR_THROW(GetProcess(), ThrowHR);
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());

        RefreshIfNeeded();
        hr = GetFrameWorker(ppFrame);
    }
    PUBLIC_REENTRANT_API_END(hr);

    if (FAILED(hr))
    {
        if (m_fIsOneFrameAhead && (m_pCachedFrame == NULL))
        {
            // We encountered a problem when we try to materialize a CordbNativeFrame.
            // Cache the failure HR so that we can return it later if the caller
            // calls GetFrame() again or GetContext().
            m_cachedHR = hr;
        }
    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Refer to the comment for code:CordbStackWalk::GetFrame
//

HRESULT CordbStackWalk::GetFrameWorker(ICorDebugFrame ** ppFrame)
{
    _ASSERTE(GetProcess()->GetProcessLock()->HasLock());

    if (ppFrame == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }
    *ppFrame = NULL;

    RSInitHolder<CordbFrame> pResultFrame(NULL);

    if (m_fIsOneFrameAhead)
    {
        if (m_pCachedFrame != NULL)
        {
            pResultFrame.Assign(m_pCachedFrame);
            pResultFrame.TransferOwnershipExternal(ppFrame);
            return S_OK;
        }
        else
        {
            // We encountered a problem when we were trying to initialize the CordbNativeFrame.
            // However, the problem occurred after we have unwound the current frame.
            // Whatever error code we return, it should be the same one GetContext() returns.
            _ASSERTE(FAILED(m_cachedHR));
            ThrowHR(m_cachedHR);
        }
    }

    IDacDbiInterface * pDAC = NULL;
    DebuggerIPCE_STRData frameData;
    ZeroMemory(&frameData, sizeof(frameData));
    IDacDbiInterface::FrameType ft = IDacDbiInterface::kInvalid;

    pDAC = GetProcess()->GetDAC();
    ft = pDAC->GetStackWalkCurrentFrameInfo(m_pSFIHandle, &frameData);

    if (ft == IDacDbiInterface::kInvalid)
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CSW::GFW - invalid stackwalker (%p)", this);
        ThrowHR(E_FAIL);
    }
    else if (ft == IDacDbiInterface::kAtEndOfStack)
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CSW::GFW - past end of stack (%p)", this);
        ThrowHR(CORDBG_E_PAST_END_OF_STACK);
    }
    else if (ft == IDacDbiInterface::kNativeStackFrame)
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CSW::GFW - native stack frame (%p)", this);
        return S_FALSE;
    }
    else if (ft == IDacDbiInterface::kManagedExceptionHandlingCodeFrame)
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CSW::GFW - managed exception handling code frame (%p)", this);
        return S_FALSE;
    }
    else if (ft == IDacDbiInterface::kExplicitFrame)
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CSW::GFW - explicit frame (%p)", this);

        // We no longer expect to get internal frames by unwinding.
        GetProcess()->TargetConsistencyCheck(false);
    }
    else if (ft == IDacDbiInterface::kManagedStackFrame)
    {
        _ASSERTE(frameData.eType == DebuggerIPCE_STRData::cMethodFrame);

        HRESULT hr = S_OK;

        // In order to find the FramePointer on x86, we need to unwind to the next frame.
        // Technically, only x86 needs to do this, because the x86 runtime stackwalker doesn't uwnind
        // one frame ahead of time.  However, we are doing this on all platforms to keep things simple.
        BOOL fSuccess = UnwindStackFrame();
        (void)fSuccess; //prevent "unused variable" error from GCC
        _ASSERTE(fSuccess);

        m_fIsOneFrameAhead = true;
#if defined(TARGET_X86)
        frameData.fp = pDAC->GetFramePointer(m_pSFIHandle);
#endif // TARGET_X86

        // currentFuncData contains general information about the method.
        // It has no information about any particular jitted instance of the method.
        DebuggerIPCE_FuncData * pFuncData = &(frameData.v.funcData);

        // currentJITFuncData contains information about the current jitted instance of the method
        // on the stack.
        DebuggerIPCE_JITFuncData * pJITFuncData = &(frameData.v.jitFuncData);

        // Lookup the appdomain that the thread was in when it was executing code for this frame. We pass this
        // to the frame when we create it so we can properly resolve locals in that frame later.
        CordbAppDomain * pCurrentAppDomain = GetProcess()->LookupOrCreateAppDomain(frameData.vmCurrentAppDomainToken);
        _ASSERTE(pCurrentAppDomain != NULL);

        // Lookup the module
        CordbModule* pModule = pCurrentAppDomain->LookupOrCreateModule(pFuncData->vmDomainAssembly);
        PREFIX_ASSUME(pModule != NULL);

        // Create or look up a CordbNativeCode.  There is one for each jitted instance of a method,
        // and we may have multiple instances because of generics.
        CordbNativeCode * pNativeCode = pModule->LookupOrCreateNativeCode(pFuncData->funcMetadataToken,
                                                                          pJITFuncData->vmNativeCodeMethodDescToken,
                                                                          pJITFuncData->nativeStartAddressPtr);
        IfFailThrow(hr);

        // The native code object will create the function object if needed
        CordbFunction * pFunction = pNativeCode->GetFunction();

        // A CordbFunction is theoretically the uninstantiated method, yet for back-compat we allow
        // debuggers to assume that it corresponds to exactly 1 native code blob. In order for
        // an open generic function to know what native code to give back, we attach an arbitrary
        // native code that we located through code inspection.
        // Note that not all CordbFunction objects get created via stack traces because you can also
        // create them by name. In that case you still won't get code for Open generic functions
        // because we will never have attached one and the lookup by token is insufficient. This
        // behavior mimics our 2.0 debugging behavior though so its not a regression.
        pFunction->NotifyCodeCreated(pNativeCode);

        IfFailThrow(hr);

        _ASSERTE((pFunction != NULL) && (pNativeCode != NULL));

        // initialize the auxiliary info required for funclets
        CordbMiscFrame miscFrame(pJITFuncData);

        // Create the native frame.
        CordbNativeFrame* pNativeFrame = new CordbNativeFrame(m_pCordbThread,
                                                              frameData.fp,
                                                              pNativeCode,
                                                              pJITFuncData->nativeOffset,
                                                              &(frameData.rd),
                                                              frameData.v.taAmbientESP,
                                                              !!frameData.quicklyUnwound,
                                                              pCurrentAppDomain,
                                                              &miscFrame,
                                                              &(frameData.ctx));

        pResultFrame.Assign(static_cast<CordbFrame *>(pNativeFrame));
        m_pCachedFrame.Assign(static_cast<CordbFrame *>(pNativeFrame));

        // @dbgtodo  dynamic language debugging
        // If we are dealing with a dynamic method (e.g. an IL stub, a LCG method, etc.),
        // then we don't have the metadata or the debug info (sequence points, etc.).
        // This means that we can't do anything meaningful with a CordbJITILFrame anyway,
        // so let's not create the CordbJITILFrame at all.  Note that methods created with
        // RefEmit are okay, i.e. they have metadata.

        //     The check for IsNativeImpl() != CordbFunction::kNativeOnly catches an odd profiler
        // case. A profiler can rewrite assemblies at load time so that a P/invoke becomes a
        // regular managed method. mscordbi isn't yet designed to handle runtime metadata
        // changes, so it still thinks the method is a p/invoke. If we only relied on
        // frameData.v.fNoMetadata which is populated by the DAC, that will report
        // FALSE (the method does have metadata/IL now). However pNativeCode->LoadNativeInfo
        // is going to check DBI's metadata and calculate this is a p/invoke, which will
        // throw an exception that the method isn't IL.
        //     Ideally we probably want to expose the profiler's change to the method,
        // however that will take significant work. Part of that is correctly detecting and
        // updating metadata in DBI, part is determinging if/how the debugger is notified,
        // and part is auditing mscordbi to ensure that anything we cached based on the
        // old metadata is correctly invalidated.
        //     Since this is a late fix going into a controlled servicing release I have
        // opted for a much narrower fix. Doing the check for IsNativeImpl() != CordbFunction::kNativeOnly
        // will continue to treat our new method as though it was a p/invoke, and the
        // debugger will not provide IL for it. The debugger can't inspect within the profiler
        // modified method, but at least the error won't leak out to interfere with inspection
        // of the callstack as a whole.
        if (!frameData.v.fNoMetadata &&
            pNativeCode->GetFunction()->IsNativeImpl() != CordbFunction::kNativeOnly)
        {
            pNativeCode->LoadNativeInfo();

            // By design, when a managed exception occurs we return the sequence point containing the faulting
            // instruction in the leaf frame. In the past we didn't always achieve this,
            // but we are being more deliberate about this behavior now.

            // If justAfterILThrow is true, it means nativeOffset points to the return address of IL_Throw
            // (or another JIT exception helper) after an exception has been thrown.
            // In such cases we want to adjust nativeOffset, so it will point an actual exception callsite.
            // By subtracting STACKWALK_CONTROLPC_ADJUST_OFFSET from nativeOffset you can get
            // an address somewhere inside CALL instruction.
            // This ensures more consistent placement of exception line highlighting in Visual Studio
            DWORD nativeOffsetToMap = pJITFuncData->justAfterILThrow ?
                               (DWORD)pJITFuncData->nativeOffset - STACKWALK_CONTROLPC_ADJUST_OFFSET :
                               (DWORD)pJITFuncData->nativeOffset;
            CorDebugMappingResult mappingType;
            ULONG uILOffset = pNativeCode->GetSequencePoints()->MapNativeOffsetToIL(
                    nativeOffsetToMap,
                    &mappingType);

            // Find or create the IL Code, and the pJITILFrame.
            RSExtSmartPtr<CordbILCode> pCode;

            // The code for populating CordbFunction ILCode looks really bizzare... it appears to only grab the
            // correct version of the IL if that is still the current EnC version yet it is populated deliberately
            // late bound at which point the latest version may be different. In fact even here the latest version
            // could already be different, but this is no worse than what the code used to do
            hr = pFunction->GetILCode(&pCode);
            IfFailThrow(hr);
            _ASSERTE(pCode != NULL);

            // We populate the code for ReJit eagerly to make sure we still have it if the profiler removes the
            // instrumentation later. Of course the only way it will still be accessible to our caller is if they
            // save a pointer to the ILCode.
            // I'm not sure if ignoring rejit for mini-dumps is the right call long term, but we aren't doing
            // anything special to collect the memory at dump time so we better be prepared to not fetch it here.
            // We'll attempt to treat it as not being instrumented, though I suspect the abstraction is leaky.
            RSSmartPtr<CordbReJitILCode> pReJitCode;
            EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
            {
                VMPTR_NativeCodeVersionNode vmNativeCodeVersionNode = VMPTR_NativeCodeVersionNode::NullPtr();
                IfFailThrow(GetProcess()->GetDAC()->GetNativeCodeVersionNode(pJITFuncData->vmNativeCodeMethodDescToken, pJITFuncData->nativeStartAddressPtr, &vmNativeCodeVersionNode));
                if (!vmNativeCodeVersionNode.IsNull())
                {
                    VMPTR_ILCodeVersionNode vmILCodeVersionNode = VMPTR_ILCodeVersionNode::NullPtr();
                    IfFailThrow(GetProcess()->GetDAC()->GetILCodeVersionNode(vmNativeCodeVersionNode, &vmILCodeVersionNode));
                    if (!vmILCodeVersionNode.IsNull())
                    {
                        IfFailThrow(pFunction->LookupOrCreateReJitILCode(vmILCodeVersionNode, &pReJitCode));
                    }
                }
            }
            EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY



            RSInitHolder<CordbJITILFrame> pJITILFrame(new CordbJITILFrame(pNativeFrame,
                                                                pCode,
                                                                uILOffset,
                                                                mappingType,
                                                                frameData.v.exactGenericArgsToken,
                                                                frameData.v.dwExactGenericArgsTokenIndex,
                                                                !!frameData.v.fVarArgs,
                                                                pReJitCode,
                                                                pJITFuncData->justAfterILThrow));

            // Initialize the frame.  This is a nop if the method is not a vararg method.
            hr = pJITILFrame->Init();
            IfFailThrow(hr);

            pNativeFrame->m_JITILFrame.Assign(pJITILFrame);
            pJITILFrame.ClearAndMarkDontNeuter();
        }

        STRESS_LOG3(LF_CORDB, LL_INFO1000, "CSW::GFW - managed stack frame (%p): CNF - 0x%p, CJILF - 0x%p",
                    this, pNativeFrame, pNativeFrame->m_JITILFrame.GetValue());
    } // kManagedStackFrame
    else if (ft == IDacDbiInterface::kNativeRuntimeUnwindableStackFrame)
    {
        _ASSERTE(frameData.eType == DebuggerIPCE_STRData::cRuntimeNativeFrame);

        // In order to find the FramePointer on x86, we need to unwind to the next frame.
        // Technically, only x86 needs to do this, because the x86 runtime stackwalker doesn't uwnind
        // one frame ahead of time.  However, we are doing this on all platforms to keep things simple.
        BOOL fSuccess = UnwindStackFrame();
        (void)fSuccess; //prevent "unused variable" error from GCC
        _ASSERTE(fSuccess);

        m_fIsOneFrameAhead = true;
#if defined(TARGET_X86)
        frameData.fp = pDAC->GetFramePointer(m_pSFIHandle);
#endif // TARGET_X86

        // Lookup the appdomain that the thread was in when it was executing code for this frame. We pass this
        // to the frame when we create it so we can properly resolve locals in that frame later.
        CordbAppDomain * pCurrentAppDomain =
            GetProcess()->LookupOrCreateAppDomain(frameData.vmCurrentAppDomainToken);
        _ASSERTE(pCurrentAppDomain != NULL);

        CordbRuntimeUnwindableFrame * pRuntimeFrame = new CordbRuntimeUnwindableFrame(m_pCordbThread,
                                                                                      frameData.fp,
                                                                                      pCurrentAppDomain,
                                                                                      &(frameData.ctx));

        pResultFrame.Assign(static_cast<CordbFrame *>(pRuntimeFrame));
        m_pCachedFrame.Assign(static_cast<CordbFrame *>(pRuntimeFrame));

        STRESS_LOG2(LF_CORDB, LL_INFO1000, "CSW::GFW - runtime unwindable stack frame (%p): 0x%p",
                    this, pRuntimeFrame);
    }

    pResultFrame.TransferOwnershipExternal(ppFrame);

    return S_OK;
}
