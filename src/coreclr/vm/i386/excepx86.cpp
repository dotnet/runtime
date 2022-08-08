// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

/*  EXCEP.CPP:
 *
 */
#include "common.h"

#include "frames.h"
#include "excep.h"
#include "object.h"
#include "field.h"
#include "dbginterface.h"
#include "cgensys.h"
#include "comutilnative.h"
#include "sigformat.h"
#include "siginfo.hpp"
#include "gcheaputilities.h"
#include "eedbginterfaceimpl.h" //so we can clearexception in COMPlusThrow
#include "eventtrace.h"
#include "eetoprofinterfacewrapper.inl"
#include "eedbginterfaceimpl.inl"
#include "dllimportcallback.h"
#include "threads.h"
#include "eeconfig.h"
#include "vars.hpp"
#include "generics.h"
#include "corinfo.h"

#include "asmconstants.h"
#include "virtualcallstub.h"

#ifndef FEATURE_EH_FUNCLETS
MethodDesc * GetUserMethodForILStub(Thread * pThread, UINT_PTR uStubSP, MethodDesc * pILStubMD, Frame ** ppFrameOut);

#if !defined(DACCESS_COMPILE)

#define FORMAT_MESSAGE_BUFFER_LENGTH 1024

BOOL ComPlusFrameSEH(EXCEPTION_REGISTRATION_RECORD*);
PEXCEPTION_REGISTRATION_RECORD GetPrevSEHRecord(EXCEPTION_REGISTRATION_RECORD*);

extern "C" {
// in asmhelpers.asm:
VOID STDCALL ResumeAtJitEHHelper(EHContext *pContext);
int STDCALL CallJitEHFilterHelper(size_t *pShadowSP, EHContext *pContext);
VOID STDCALL CallJitEHFinallyHelper(size_t *pShadowSP, EHContext *pContext);

typedef void (*RtlUnwindCallbackType)(void);

BOOL CallRtlUnwind(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
           RtlUnwindCallbackType callback,
           EXCEPTION_RECORD *pExceptionRecord,
           void *retval);

BOOL CallRtlUnwindSafe(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
           RtlUnwindCallbackType callback,
           EXCEPTION_RECORD *pExceptionRecord,
           void *retval);
}

static inline BOOL
CPFH_ShouldUnwindStack(const EXCEPTION_RECORD * pCER) {

    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pCER != NULL);

    // We can only unwind those exceptions whose context/record we don't need for a
    // rethrow.  This is complus, and stack overflow.  For all the others, we
    // need to keep the context around for a rethrow, which means they can't
    // be unwound.
    if (IsComPlusException(pCER) || pCER->ExceptionCode == STATUS_STACK_OVERFLOW)
        return TRUE;
    else
        return FALSE;
}

static inline BOOL IsComPlusNestedExceptionRecord(EXCEPTION_REGISTRATION_RECORD* pEHR)
{
    LIMITED_METHOD_CONTRACT;
    if (pEHR->Handler == (PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler)
        return TRUE;
    return FALSE;
}

EXCEPTION_REGISTRATION_RECORD *TryFindNestedEstablisherFrame(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame)
{
    LIMITED_METHOD_CONTRACT;
    while (pEstablisherFrame->Handler != (PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler) {
        pEstablisherFrame = pEstablisherFrame->Next;
        if (pEstablisherFrame == EXCEPTION_CHAIN_END) return 0;
    }
    return pEstablisherFrame;
}

#ifdef _DEBUG
// stores last handler we went to in case we didn't get an endcatch and stack is
// corrupted we can figure out who did it.
static MethodDesc *gLastResumedExceptionFunc = NULL;
static DWORD gLastResumedExceptionHandler = 0;
#endif

//---------------------------------------------------------------------
//  void RtlUnwindCallback()
// call back function after global unwind, rtlunwind calls this function
//---------------------------------------------------------------------
static void RtlUnwindCallback()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Should never get here");
}

BOOL FastNExportSEH(EXCEPTION_REGISTRATION_RECORD* pEHR)
{
    LIMITED_METHOD_CONTRACT;

    if ((LPVOID)pEHR->Handler == (LPVOID)FastNExportExceptHandler)
        return TRUE;
    return FALSE;
}

BOOL ReverseCOMSEH(EXCEPTION_REGISTRATION_RECORD* pEHR)
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_COMINTEROP
    if ((LPVOID)pEHR->Handler == (LPVOID)COMPlusFrameHandlerRevCom)
        return TRUE;
#endif // FEATURE_COMINTEROP
    return FALSE;
}


//
// Returns true if the given SEH handler is one of our SEH handlers that is responsible for managing exceptions in
// regions of managed code.
//
BOOL IsUnmanagedToManagedSEHHandler(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame)
{
    WRAPPER_NO_CONTRACT;

    //
    // ComPlusFrameSEH() is for COMPlusFrameHandler & COMPlusNestedExceptionHandler.
    // FastNExportSEH() is for FastNExportExceptHandler.
    //
    return (ComPlusFrameSEH(pEstablisherFrame) || FastNExportSEH(pEstablisherFrame) || ReverseCOMSEH(pEstablisherFrame));
}

Frame *GetCurrFrame(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame)
{
    Frame *pFrame;
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsUnmanagedToManagedSEHHandler(pEstablisherFrame));
    pFrame = ((FrameHandlerExRecord *)pEstablisherFrame)->GetCurrFrame();

    // Assert that the exception frame is on the thread or that the exception frame is the top frame.
    _ASSERTE(GetThreadNULLOk() == NULL || GetThread()->GetFrame() == (Frame*)-1 || GetThread()->GetFrame() <= pFrame);

    return pFrame;
}

EXCEPTION_REGISTRATION_RECORD* GetNextCOMPlusSEHRecord(EXCEPTION_REGISTRATION_RECORD* pRec) {
    WRAPPER_NO_CONTRACT;
    if (pRec == EXCEPTION_CHAIN_END)
        return EXCEPTION_CHAIN_END;

    do {
        _ASSERTE(pRec != 0);
        pRec = pRec->Next;
    } while (pRec != EXCEPTION_CHAIN_END && !IsUnmanagedToManagedSEHHandler(pRec));

    _ASSERTE(pRec == EXCEPTION_CHAIN_END || IsUnmanagedToManagedSEHHandler(pRec));
    return pRec;
}


/*
 * GetClrSEHRecordServicingStackPointer
 *
 * This function searchs all the Frame SEH records, and finds the one that is
 * currently signed up to do all exception handling for the given stack pointer
 * on the given thread.
 *
 * Parameters:
 *   pThread - The thread to search on.
 *   pStackPointer - The stack location that we are finding the Frame SEH Record for.
 *
 * Returns
 *   A pointer to the SEH record, or EXCEPTION_CHAIN_END if none was found.
 *
 */

PEXCEPTION_REGISTRATION_RECORD
GetClrSEHRecordServicingStackPointer(Thread *pThread,
                                     void *pStackPointer)
{
    ThreadExceptionState* pExState = pThread->GetExceptionState();

    //
    // We can only do this if there is a context in the pExInfo. There are cases (most notably the
    // EEPolicy::HandleFatalError case) where we don't have that.  In these cases we will return
    // no enclosing handler since we cannot accurately determine the FS:0 entry which services
    // this stack address.
    //
    // The side effect of this is that for these cases, the debugger cannot intercept
    // the exception
    //
    CONTEXT* pContextRecord = pExState->GetContextRecord();
    if (pContextRecord == NULL)
    {
        return EXCEPTION_CHAIN_END;
    }

    void *exceptionSP = dac_cast<PTR_VOID>(GetSP(pContextRecord));


    //
    // Now set the establishing frame.  What this means in English is that we need to find
    // the fs:0 entry that handles exceptions for the place on the stack given in stackPointer.
    //
    PEXCEPTION_REGISTRATION_RECORD pSEHRecord = GetFirstCOMPlusSEHRecord(pThread);

    while (pSEHRecord != EXCEPTION_CHAIN_END)
    {

        //
        // Skip any SEHRecord which is not a CLR record or was pushed after the exception
        // on this thread occurred.
        //
        if (IsUnmanagedToManagedSEHHandler(pSEHRecord) && (exceptionSP <= (void *)pSEHRecord))
        {
            Frame *pFrame = GetCurrFrame(pSEHRecord);
            //
            // Arcane knowledge here.  All Frame records are stored on the stack by the runtime
            // in ever decreasing address space.  So, we merely have to search back until
            // we find the first frame record with a higher stack value to find the
            // establishing frame for the given stack address.
            //
            if (((void *)pFrame) >= pStackPointer)
            {
                break;
            }

        }

        pSEHRecord = GetNextCOMPlusSEHRecord(pSEHRecord);
    }

    return pSEHRecord;
}

#ifdef _DEBUG
// We've deteremined during a stack walk that managed code is transitioning to unamanaged (EE) code. Check that the
// state of the EH chain is correct.
//
// For x86, check that we do INSTALL_COMPLUS_EXCEPTION_HANDLER before calling managed code.  This check should be
// done for all managed code sites, not just transitions. But this will catch most problem cases.
void VerifyValidTransitionFromManagedCode(Thread *pThread, CrawlFrame *pCF)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(ExecutionManager::IsManagedCode(GetControlPC(pCF->GetRegisterSet())));

    // Cannot get to the TEB of other threads. So ignore them.
    if (pThread != GetThreadNULLOk())
    {
        return;
    }

    // Find the EH record guarding the current region of managed code, based on the CrawlFrame passed in.
    PEXCEPTION_REGISTRATION_RECORD pEHR = GetCurrentSEHRecord();

    while ((pEHR != EXCEPTION_CHAIN_END) && ((ULONG_PTR)pEHR < GetRegdisplaySP(pCF->GetRegisterSet())))
    {
        pEHR = pEHR->Next;
    }

    // VerifyValidTransitionFromManagedCode can be called before the CrawlFrame's MethodDesc is initialized.
    // Fix that if necessary for the consistency check.
    MethodDesc * pFunction = pCF->GetFunction();
    if ((!IsUnmanagedToManagedSEHHandler(pEHR)) && // Will the assert fire?  If not, don't waste our time.
        (pFunction == NULL))
    {
        _ASSERTE(pCF->GetRegisterSet());
        PCODE ip = GetControlPC(pCF->GetRegisterSet());
        pFunction = ExecutionManager::GetCodeMethodDesc(ip);
        _ASSERTE(pFunction);
    }

    // Great, we've got the EH record that's next up the stack from the current SP (which is in managed code). That
    // had better be a record for one of our handlers responsible for handling exceptions in managed code. If its
    // not, then someone made it into managed code without setting up one of our EH handlers, and that's really
    // bad.
    CONSISTENCY_CHECK_MSGF(IsUnmanagedToManagedSEHHandler(pEHR),
                           ("Invalid transition into managed code!\n\n"
                            "We're walking this thread's stack and we've reached a managed frame at Esp=0x%p. "
                            "(The method is %s::%s) "
                            "The very next FS:0 record (0x%p) up from this point on the stack should be one of "
                            "our 'unmanaged to managed SEH handlers', but its not... its something else, and "
                            "that's very bad. It indicates that someone managed to call into managed code without "
                            "setting up the proper exception handling.\n\n"
                            "Get a good unmanaged stack trace for this thread. All FS:0 records are on the stack, "
                            "so you can see who installed the last handler. Somewhere between that function and "
                            "where the thread is now is where the bad transition occurred.\n\n"
                            "A little extra info: FS:0 = 0x%p, pEHR->Handler = 0x%p\n",
                            GetRegdisplaySP(pCF->GetRegisterSet()),
                            pFunction ->m_pszDebugClassName,
                            pFunction ->m_pszDebugMethodName,
                            pEHR,
                            GetCurrentSEHRecord(),
                            pEHR->Handler));
}

#endif

//================================================================================

// There are some things that should never be true when handling an
// exception.  This function checks for them.  Will assert or trap
// if it finds an error.
static inline void
CPFH_VerifyThreadIsInValidState(Thread* pThread, DWORD exceptionCode, EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame) {
    WRAPPER_NO_CONTRACT;

    if (   exceptionCode == STATUS_BREAKPOINT
        || exceptionCode == STATUS_SINGLE_STEP) {
        return;
    }

#ifdef _DEBUG
    // check for overwriting of stack
    CheckStackBarrier(pEstablisherFrame);
    // trigger check for bad fs:0 chain
    GetCurrentSEHRecord();
#endif

    if (!g_fEEShutDown) {
        // An exception on the GC thread, or while holding the thread store lock, will likely lock out the entire process.
        if (::IsGCThread() || ThreadStore::HoldingThreadStore())
        {
            _ASSERTE(!"Exception during garbage collection or while holding thread store");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
    }
}


#ifdef FEATURE_HIJACK
void
CPFH_AdjustContextForThreadSuspensionRace(CONTEXT *pContext, Thread *pThread)
{
    WRAPPER_NO_CONTRACT;

    PCODE f_IP = GetIP(pContext);
    if (Thread::IsAddrOfRedirectFunc((PVOID)f_IP)) {

        // This is a very rare case where we tried to redirect a thread that was
        // just about to dispatch an exception, and our update of EIP took, but
        // the thread continued dispatching the exception.
        //
        // If this should happen (very rare) then we fix it up here.
        //
        _ASSERTE(pThread->GetSavedRedirectContext());
        SetIP(pContext, GetIP(pThread->GetSavedRedirectContext()));
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 1 setting IP = %x\n", pContext->Eip);
    }

    if (f_IP == GetEEFuncEntryPoint(THROW_CONTROL_FOR_THREAD_FUNCTION)) {

        // This is a very rare case where we tried to redirect a thread that was
        // just about to dispatch an exception, and our update of EIP took, but
        // the thread continued dispatching the exception.
        //
        // If this should happen (very rare) then we fix it up here.
        //
        SetIP(pContext, GetIP(pThread->m_OSContext));
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 2 setting IP = %x\n", pContext->Eip);
    }

// We have another even rarer race condition:
// - A) On thread A, Debugger puts an int 3 in the code stream at address X
// - A) We hit it and the begin an exception. The eip will be X + 1 (int3 is special)
// - B) Meanwhile, thread B redirects A's eip to Y. (Although A is really somewhere
// in the kernel, it looks like it's still in user code, so it can fall under the
// HandledJitCase and can be redirected)
// - A) The OS, trying to be nice, expects we have a breakpoint exception at X+1,
// but does -1 on the address since it knows int3 will leave the eip +1.
// So the context structure it will pass to the Handler is ideally (X+1)-1 = X
//
// ** Here's the race: Since thread B redirected A, the eip is actually Y (not X+1),
// but the kernel still touches it up to Y-1. So there's a window between when we hit a
// bp and when the handler gets called that this can happen.
// This causes an unhandled BP (since the debugger doesn't recognize the bp at Y-1)
//
// So what to do: If we land at Y-1 (ie, if f_IP+1 is the addr of a Redirected Func),
// then restore the EIP back to X. This will skip the redirection.
// Fortunately, this only occurs in cases where it's ok
// to skip. The debugger will recognize the patch and handle it.

    if (Thread::IsAddrOfRedirectFunc((PVOID)(f_IP + 1))) {
        _ASSERTE(pThread->GetSavedRedirectContext());
        SetIP(pContext, GetIP(pThread->GetSavedRedirectContext()) - 1);
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 3 setting IP = %x\n", pContext->Eip);
    }

    if (f_IP + 1 == GetEEFuncEntryPoint(THROW_CONTROL_FOR_THREAD_FUNCTION)) {
        SetIP(pContext, GetIP(pThread->m_OSContext) - 1);
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 4 setting IP = %x\n", pContext->Eip);
    }
}
#endif // FEATURE_HIJACK

uint32_t            g_exceptionCount;

//******************************************************************************
EXCEPTION_DISPOSITION COMPlusAfterUnwind(
        EXCEPTION_RECORD *pExceptionRecord,
        EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
        ThrowCallbackType& tct)
{
    WRAPPER_NO_CONTRACT;

    // Note: we've completed the unwind pass up to the establisher frame, and we're headed off to finish our
    // cleanup and end up back in jitted code. Any more FS0 handlers pushed from this point on out will _not_ be
    // unwound. We go ahead and assert right here that indeed there are no handlers below the establisher frame
    // before we go any further.
    _ASSERTE(pEstablisherFrame == GetCurrentSEHRecord());

    Thread* pThread = GetThread();

    _ASSERTE(tct.pCurrentExceptionRecord == pEstablisherFrame);

    NestedHandlerExRecord nestedHandlerExRecord;
    nestedHandlerExRecord.Init((PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler, GetCurrFrame(pEstablisherFrame));

    // ... and now, put the nested record back on.
    INSTALL_EXCEPTION_HANDLING_RECORD(&(nestedHandlerExRecord.m_ExReg));

    // We entered COMPlusAfterUnwind in PREEMP, but we need to be in COOP from here on out
    GCX_COOP_NO_DTOR();

    tct.bIsUnwind = TRUE;
    tct.pProfilerNotify = NULL;

    LOG((LF_EH, LL_INFO100, "COMPlusFrameHandler: unwinding\n"));

    tct.bUnwindStack = CPFH_ShouldUnwindStack(pExceptionRecord);

    LOG((LF_EH, LL_INFO1000, "COMPlusAfterUnwind: going to: pFunc:%#X, pStack:%#X\n",
        tct.pFunc, tct.pStack));

    UnwindFrames(pThread, &tct);

#ifdef DEBUGGING_SUPPORTED
    ExInfo* pExInfo = pThread->GetExceptionState()->GetCurrentExceptionTracker();
    if (pExInfo->m_ValidInterceptionContext)
    {
        // By now we should have all unknown FS:[0] handlers unwinded along with the managed Frames until
        // the interception point. We can now pop nested exception handlers and resume at interception context.
        EHContext context = pExInfo->m_InterceptionContext;
        pExInfo->m_InterceptionContext.Init();
        pExInfo->m_ValidInterceptionContext = FALSE;

        UnwindExceptionTrackerAndResumeInInterceptionFrame(pExInfo, &context);
    }
#endif // DEBUGGING_SUPPORTED

    _ASSERTE(!"Should not get here");
    return ExceptionContinueSearch;
} // EXCEPTION_DISPOSITION COMPlusAfterUnwind()

#ifdef DEBUGGING_SUPPORTED

//---------------------------------------------------------------------------------------
//
// This function is called to intercept an exception and start an unwind.
//
// Arguments:
//    pCurrentEstablisherFrame  - the exception registration record covering the stack range
//                                containing the interception point
//    pExceptionRecord          - EXCEPTION_RECORD of the exception being intercepted
//
// Return Value:
//    ExceptionContinueSearch if the exception cannot be intercepted
//
// Notes:
//    If the exception is intercepted, this function never returns.
//

EXCEPTION_DISPOSITION ClrDebuggerDoUnwindAndIntercept(EXCEPTION_REGISTRATION_RECORD *pCurrentEstablisherFrame,
                                                      EXCEPTION_RECORD *pExceptionRecord)
{
    WRAPPER_NO_CONTRACT;

    if (!CheckThreadExceptionStateForInterception())
    {
        return ExceptionContinueSearch;
    }

    Thread*               pThread  = GetThread();
    ThreadExceptionState* pExState = pThread->GetExceptionState();

    EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame;
    ThrowCallbackType tct;
    tct.Init();

    pExState->GetDebuggerState()->GetDebuggerInterceptInfo(&pEstablisherFrame,
                                      &(tct.pFunc),
                                      &(tct.dHandler),
                                      &(tct.pStack),
                                      NULL,
                                      &(tct.pBottomFrame)
                                     );

    //
    // If the handler that we've selected as the handler for the target frame of the unwind is in fact above the
    // handler that we're currently executing in, then use the current handler instead. Why? Our handlers for
    // nested exceptions actually process managed frames that live above them, up to the COMPlusFrameHanlder that
    // pushed the nested handler. If the user selectes a frame above the nested handler, then we will have selected
    // the COMPlusFrameHandler above the current nested handler. But we don't want to ask RtlUnwind to unwind past
    // the nested handler that we're currently executing in.
    //
    if (pEstablisherFrame > pCurrentEstablisherFrame)
    {
        // This should only happen if we're in a COMPlusNestedExceptionHandler.
        _ASSERTE(IsComPlusNestedExceptionRecord(pCurrentEstablisherFrame));

        pEstablisherFrame = pCurrentEstablisherFrame;
    }

#ifdef _DEBUG
    tct.pCurrentExceptionRecord = pEstablisherFrame;
#endif

    LOG((LF_EH|LF_CORDB, LL_INFO100, "ClrDebuggerDoUnwindAndIntercept: Intercepting at %s\n", tct.pFunc->m_pszDebugMethodName));
    LOG((LF_EH|LF_CORDB, LL_INFO100, "\t\t: pFunc is 0x%X\n", tct.pFunc));
    LOG((LF_EH|LF_CORDB, LL_INFO100, "\t\t: pStack is 0x%X\n", tct.pStack));

    CallRtlUnwindSafe(pEstablisherFrame, RtlUnwindCallback, pExceptionRecord, 0);

    ExInfo* pExInfo = pThread->GetExceptionState()->GetCurrentExceptionTracker();
    if (pExInfo->m_ValidInterceptionContext)
    {
        // By now we should have all unknown FS:[0] handlers unwinded along with the managed Frames until
        // the interception point. We can now pop nested exception handlers and resume at interception context.
        GCX_COOP();
        EHContext context = pExInfo->m_InterceptionContext;
        pExInfo->m_InterceptionContext.Init();
        pExInfo->m_ValidInterceptionContext = FALSE;

        UnwindExceptionTrackerAndResumeInInterceptionFrame(pExInfo, &context);
    }

    // on x86 at least, RtlUnwind always returns

    // Note: we've completed the unwind pass up to the establisher frame, and we're headed off to finish our
    // cleanup and end up back in jitted code. Any more FS0 handlers pushed from this point on out will _not_ be
    // unwound.
    return COMPlusAfterUnwind(pExState->GetExceptionRecord(), pEstablisherFrame, tct);
} // EXCEPTION_DISPOSITION ClrDebuggerDoUnwindAndIntercept()

#endif // DEBUGGING_SUPPORTED

// This is a wrapper around the assembly routine that invokes RtlUnwind in the OS.
// When we invoke RtlUnwind, the OS will modify the ExceptionFlags field in the
// exception record to reflect unwind. Since we call RtlUnwind in the first pass
// with a valid exception record when we find an exception handler AND because RtlUnwind
// returns on x86, the OS would have flagged the exception record for unwind.
//
// Incase the exception is rethrown from the catch/filter-handler AND it's a non-COMPLUS
// exception, the runtime will use the reference to the saved exception record to reraise
// the exception, as part of rethrow fixup. Since the OS would have modified the exception record
// to reflect unwind, this wrapper will "reset" the ExceptionFlags field when RtlUnwind returns.
// Otherwise, the rethrow will result in second pass, as opposed to first, since the ExceptionFlags
// would indicate an unwind.
//
// This rethrow issue does not affect COMPLUS exceptions since we always create a brand new exception
// record for them in RaiseTheExceptionInternalOnly.
BOOL CallRtlUnwindSafe(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
           RtlUnwindCallbackType callback,
           EXCEPTION_RECORD *pExceptionRecord,
           void *retval)
{
    LIMITED_METHOD_CONTRACT;

    // Save the ExceptionFlags value before invoking RtlUnwind.
    DWORD dwExceptionFlags = pExceptionRecord->ExceptionFlags;

    BOOL fRetVal = CallRtlUnwind(pEstablisherFrame, callback, pExceptionRecord, retval);

    // Reset ExceptionFlags field, if applicable
    if (pExceptionRecord->ExceptionFlags != dwExceptionFlags)
    {
        // We would expect the 32bit OS to have set the unwind flag at this point.
        _ASSERTE(pExceptionRecord->ExceptionFlags & EXCEPTION_UNWINDING);
        LOG((LF_EH, LL_INFO100, "CallRtlUnwindSafe: Resetting ExceptionFlags from %lu to %lu\n", pExceptionRecord->ExceptionFlags, dwExceptionFlags));
        pExceptionRecord->ExceptionFlags = dwExceptionFlags;
    }

    return fRetVal;
}

//******************************************************************************
// The essence of the first pass handler (after we've decided to actually do
//  the first pass handling).
//******************************************************************************
inline EXCEPTION_DISPOSITION __cdecl
CPFH_RealFirstPassHandler(                  // ExceptionContinueSearch, etc.
    EXCEPTION_RECORD *pExceptionRecord,     // The exception record, with exception type.
    EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,   // Exception frame on whose behalf this is called.
    CONTEXT     *pContext,                  // Context from the exception.
    void        *pDispatcherContext,        // @todo
    BOOL        bAsynchronousThreadStop,    // @todo
    BOOL        fPGCDisabledOnEntry)        // @todo
{
    // We don't want to use a runtime contract here since this codepath is used during
    // the processing of a hard SO. Contracts use a significant amount of stack
    // which we can't afford for those cases.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

#ifdef _DEBUG
    static int breakOnFirstPass = -1;

    if (breakOnFirstPass == -1)
        breakOnFirstPass = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnFirstPass);

    if (breakOnFirstPass != 0)
    {
        _ASSERTE(!"First pass exception handler");
    }
#endif

    EXCEPTION_DISPOSITION retval;
    DWORD exceptionCode = pExceptionRecord->ExceptionCode;
    Thread *pThread = GetThread();

    // We always want to be in co-operative mode when we run this function and whenever we return
    // from it, want to go to pre-emptive mode because are returning to OS.
    _ASSERTE(pThread->PreemptiveGCDisabled());

    BOOL bPopNestedHandlerExRecord = FALSE;
    LFH found = LFH_NOT_FOUND;          // Result of calling LookForHandler.
    BOOL bRethrownException = FALSE;
    BOOL bNestedException = FALSE;

#if defined(USE_FEF)
    BOOL bPopFaultingExceptionFrame = FALSE;
    FrameWithCookie<FaultingExceptionFrame> faultingExceptionFrame;
#endif // USE_FEF
    ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);

    ThrowCallbackType tct;
    tct.Init();

    tct.pTopFrame = GetCurrFrame(pEstablisherFrame); // highest frame to search to

#ifdef _DEBUG
    tct.pCurrentExceptionRecord = pEstablisherFrame;
    tct.pPrevExceptionRecord    = GetPrevSEHRecord(pEstablisherFrame);
#endif // _DEBUG

    BOOL fIsManagedCode = pContext ? ExecutionManager::IsManagedCode(GetIP(pContext)) : FALSE;


    // this establishes a marker so can determine if are processing a nested exception
    // don't want to use the current frame to limit search as it could have been unwound by
    // the time get to nested handler (ie if find an exception, unwind to the call point and
    // then resume in the catch and then get another exception) so make the nested handler
    // have the same boundary as this one. If nested handler can't find a handler, we won't
    // end up searching this frame list twice because the nested handler will set the search
    // boundary in the thread and so if get back to this handler it will have a range that starts
    // and ends at the same place.

    NestedHandlerExRecord nestedHandlerExRecord;
    nestedHandlerExRecord.Init((PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler, GetCurrFrame(pEstablisherFrame));

    INSTALL_EXCEPTION_HANDLING_RECORD(&(nestedHandlerExRecord.m_ExReg));
    bPopNestedHandlerExRecord = TRUE;

#if defined(USE_FEF)
    // Note: don't attempt to push a FEF for an exception in managed code if we weren't in cooperative mode when
    // the exception was received. If preemptive GC was enabled when we received the exception, then it means the
    // exception was rethrown from unmangaed code (including EE impl), and we shouldn't push a FEF.
    if (fIsManagedCode &&
        fPGCDisabledOnEntry &&
        (pThread->m_pFrame == FRAME_TOP ||
         pThread->m_pFrame->GetVTablePtr() != FaultingExceptionFrame::GetMethodFrameVPtr() ||
         (size_t)pThread->m_pFrame > (size_t)pEstablisherFrame))
    {
        // setup interrupted frame so that GC during calls to init won't collect the frames
        // only need it for non COM+ exceptions in managed code when haven't already
        // got one on the stack (will have one already if we have called rtlunwind because
        // the instantiation that called unwind would have installed one)
        faultingExceptionFrame.InitAndLink(pContext);
        bPopFaultingExceptionFrame = TRUE;
    }
#endif // USE_FEF

    OBJECTREF e;
    e = pThread->LastThrownObject();

    STRESS_LOG7(LF_EH, LL_INFO10, "CPFH_RealFirstPassHandler: code:%X, LastThrownObject:%p, MT:%pT"
        ", IP:%p, SP:%p, pContext:%p, pEstablisherFrame:%p\n",
        exceptionCode, OBJECTREFToObject(e), (e!=0)?e->GetMethodTable():0,
        pContext ? GetIP(pContext) : 0, pContext ? GetSP(pContext) : 0,
        pContext, pEstablisherFrame);

#ifdef LOGGING
    // If it is a complus exception, and there is a thrown object, get its name, for better logging.
    if (IsComPlusException(pExceptionRecord))
    {
        const char * eClsName = "!EXCEPTION_COMPLUS";
        if (e != 0)
        {
            eClsName = e->GetMethodTable()->GetDebugClassName();
    }
        LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: exception: 0x%08X, class: '%s', IP: 0x%p\n",
             exceptionCode, eClsName, pContext ? GetIP(pContext) : NULL));
    }
#endif

    EXCEPTION_POINTERS exceptionPointers = {pExceptionRecord, pContext};

    STRESS_LOG4(LF_EH, LL_INFO10000, "CPFH_RealFirstPassHandler: setting boundaries: Exinfo: 0x%p, BottomMostHandler:0x%p, SearchBoundary:0x%p, TopFrame:0x%p\n",
         pExInfo, pExInfo->m_pBottomMostHandler, pExInfo->m_pSearchBoundary, tct.pTopFrame);

    // Here we are trying to decide if we are coming in as:
    // 1) first handler in a brand new exception
    // 2) a subsequent handler in an exception
    // 3) a nested exception
    // m_pBottomMostHandler is the registration structure (establisher frame) for the most recent (ie lowest in
    // memory) non-nested handler that was installed  and pEstablisher frame is what the current handler
    // was registered with.
    // The OS calls each registered handler in the chain, passing its establisher frame to it.
    if (pExInfo->m_pBottomMostHandler != NULL && pEstablisherFrame > pExInfo->m_pBottomMostHandler)
    {
        STRESS_LOG3(LF_EH, LL_INFO10000, "CPFH_RealFirstPassHandler: detected subsequent handler.  ExInfo:0x%p, BottomMost:0x%p SearchBoundary:0x%p\n",
                    pExInfo, pExInfo->m_pBottomMostHandler, pExInfo->m_pSearchBoundary);

        // If the establisher frame of this handler is greater than the bottommost then it must have been
        // installed earlier and therefore we are case 2
        if (pThread->GetThrowable() == NULL)
        {
            // Bottommost didn't setup a throwable, so not exception not for us
            retval = ExceptionContinueSearch;
            goto exit;
        }

        // setup search start point
        tct.pBottomFrame = pExInfo->m_pSearchBoundary;

        if (tct.pTopFrame == tct.pBottomFrame)
        {
            // this will happen if our nested handler already searched for us so we don't want
            // to search again
            retval = ExceptionContinueSearch;
            goto exit;
        }
    }
    else
    {   // we are either case 1 or case 3
#if defined(_DEBUG_IMPL)
        //@todo: merge frames, context, handlers
        if (pThread->GetFrame() != FRAME_TOP)
            pThread->GetFrame()->LogFrameChain(LF_EH, LL_INFO1000);
#endif // _DEBUG_IMPL

        // If the exception was rethrown, we'll create a new ExInfo, which will represent the rethrown exception.
        //  The original exception is not the rethrown one.
        if (pExInfo->m_ExceptionFlags.IsRethrown() && pThread->LastThrownObject() != NULL)
        {
            pExInfo->m_ExceptionFlags.ResetIsRethrown();
            bRethrownException = TRUE;

#if defined(USE_FEF)
            if (bPopFaultingExceptionFrame)
            {
                // if we added a FEF, it will refer to the frame at the point of the original exception which is
                // already unwound so don't want it.
                // If we rethrew the exception we have already added a helper frame for the rethrow, so don't
                // need this one. If we didn't rethrow it, (ie rethrow from native) then there the topmost frame will
                // be a transition to native frame in which case we don't need it either
                faultingExceptionFrame.Pop();
                bPopFaultingExceptionFrame = FALSE;
            }
#endif
        }

        // If the establisher frame is less than the bottommost handler, then this is nested because the
        // establisher frame was installed after the bottommost.
        if (pEstablisherFrame < pExInfo->m_pBottomMostHandler
            /* || IsComPlusNestedExceptionRecord(pEstablisherFrame) */ )
        {
            bNestedException = TRUE;

            // case 3: this is a nested exception. Need to save and restore the thread info
            STRESS_LOG3(LF_EH, LL_INFO10000, "CPFH_RealFirstPassHandler: ExInfo:0x%p detected nested exception 0x%p < 0x%p\n",
                        pExInfo, pEstablisherFrame, pExInfo->m_pBottomMostHandler);

            EXCEPTION_REGISTRATION_RECORD* pNestedER = TryFindNestedEstablisherFrame(pEstablisherFrame);
            ExInfo *pNestedExInfo;

            if (!pNestedER || pNestedER >= pExInfo->m_pBottomMostHandler )
            {
                // RARE CASE.  We've re-entered the EE from an unmanaged filter.
                //
                // OR
                //
                // We can be here if we dont find a nested exception handler. This is exemplified using
                // call chain of scenario 2 explained further below.
                //
                // Assuming __try of NativeB throws an exception E1 and it gets caught in ManagedA2, then
                // bottom-most handler (BMH) is going to be CPFH_A. The catch will trigger an unwind
                // and invoke __finally in NativeB. Let the __finally throw a new exception E2.
                //
                // Assuming ManagedB2 has a catch block to catch E2, when we enter CPFH_B looking for a
                // handler for E2, our establisher frame will be that of CPFH_B, which will be lower
                // in stack than current BMH (which is CPFH_A). Thus, we will come here, determining
                // E2 to be nested exception correctly but not find a nested exception handler.
                void *limit = (void *) GetPrevSEHRecord(pExInfo->m_pBottomMostHandler);

                pNestedExInfo = new (nothrow) ExInfo();     // Very rare failure here; need robust allocator.
                if (pNestedExInfo == NULL)
                {   // if we can't allocate memory, we can't correctly continue.
                    #if defined(_DEBUG)
                    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NestedEhOom))
                        _ASSERTE(!"OOM in callback from unmanaged filter.");
                    #endif // _DEBUG

                    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_OUTOFMEMORY);
                }


                pNestedExInfo->m_StackAddress = limit;      // Note: this is also the flag that tells us this
                                                            // ExInfo was stack allocated.
            }
            else
            {
                pNestedExInfo = &((NestedHandlerExRecord*)pNestedER)->m_handlerInfo;
            }

            LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: PushExInfo() current: 0x%p previous: 0x%p\n",
                 pExInfo->m_StackAddress, pNestedExInfo->m_StackAddress));

            _ASSERTE(pNestedExInfo);
            pNestedExInfo->m_hThrowable = NULL; // pNestedExInfo may be stack allocated, and as such full of
                                                // garbage. m_hThrowable must be sane, so set it to NULL. (We could
                                                // zero the entire record, but this is cheaper.)

            pNestedExInfo->CopyAndClearSource(pExInfo);

            pExInfo->m_pPrevNestedInfo = pNestedExInfo;     // Save at head of nested info chain

#if 0
/* the following code was introduced in Whidbey as part of the Faulting Exception Frame removal (12/03).
   However it isn't correct.  If any nested exceptions occur while processing a rethrow, we would
   incorrectly consider the nested exception to be a rethrow.  See VSWhidbey 349379 for an example.

   Therefore I am disabling this code until we see a failure that explains why it was added in the first
   place.  cwb 9/04.
*/
            // If we're here as a result of a rethrown exception, set the rethrown flag on the new ExInfo.
            if (bRethrownException)
            {
                pExInfo->m_ExceptionFlags.SetIsRethrown();
            }
#endif
        }
        else
        {
            // At this point, either:
            //
            // 1) the bottom-most handler is NULL, implying this is a new exception for which we are getting ready, OR
            // 2) the bottom-most handler is not-NULL, implying that a there is already an existing exception in progress.
            //
            // Scenario 1 is that of a new throw and is easy to understand. Scenario 2 is the interesting one.
            //
            // ManagedA1 -> ManagedA2 -> ManagedA3 -> NativeCodeA -> ManagedB1 -> ManagedB2 -> ManagedB3 -> NativeCodeB
            //
            // On x86, each block of managed code is protected by one COMPlusFrameHandler [CPFH] (CLR's exception handler
            // for managed code), unlike 64bit where each frame has a personality routine attached to it. Thus,
            // for the example above, assume CPFH_A protects ManagedA* blocks and is setup just before the call to
            // ManagedA1. Likewise, CPFH_B protects ManagedB* blocks and is setup just before the call to ManagedB1.
            //
            // When ManagedB3 throws an exception, CPFH_B is invoked to look for a handler in all of the ManagedB* blocks.
            // At this point, it is setup as the "bottom-most-handler" (BMH). If no handler is found and exception reaches
            // ManagedA* blocks, CPFH_A is invoked to look for a handler and thus, becomes BMH.
            //
            // Thus, in the first pass on x86 for a given exception, a particular CPFH will be invoked only once when looking
            // for a handler and thus, registered as BMH only once. Either the exception goes unhandled and the process will
            // terminate or a handler will be found and second pass will commence.
            //
            // However, assume NativeCodeB had a __try/__finally and raised an exception [E1] within the __try. Let's assume
            // it gets caught in ManagedB1 and thus, unwind is triggered. At this point, the active exception tracker
            // has context about the exception thrown out of __try and CPFH_B is registered as BMH.
            //
            // If the __finally throws a new exception [E2], CPFH_B will be invoked again for first pass while looking for
            // a handler for the thrown exception. Since BMH is already non-NULL, we will come here since EstablisherFrame will be
            // the same as BMH (because EstablisherFrame will be that of CPFH_B). We will proceed to overwrite the "required" parts
            // of the existing exception tracker with the details of E2 (see setting of exception record and context below), erasing
            // any artifact of E1.
            //
            // This is unlike Scenario 1 when exception tracker is completely initialized to default values. This is also
            // unlike 64bit which will detect that E1 and E2 are different exceptions and hence, will setup a new tracker
            // to track E2, effectively behaving like Scenario 1 above. X86 cannot do this since there is no nested exception
            // tracker setup that gets to see the new exception.
            //
            // Thus, if E1 was a CSE and E2 isn't, we will come here and treat E2 as a CSE as well since corruption severity
            // is initialized as part of exception tracker initialization. Thus, E2 will start to be treated as CSE, which is
            // incorrect. Similar argument applies to delivery of First chance exception notification delivery.
            //
            // <QUIP> Another example why we should unify EH systems :) </QUIP>
            //
            // To address this issue, we will need to reset exception tracker here, just like the overwriting of "required"
            // parts of exception tracker.

            // If the current establisher frame is the same as the bottom-most-handler and we are here
            // in the first pass, assert that current exception and the one tracked by active exception tracker
            // are indeed different exceptions. In such a case, we must reset the exception tracker so that it can be
            // setup correctly further down when CEHelper::SetupCorruptionSeverityForActiveException is invoked.

            if ((pExInfo->m_pBottomMostHandler != NULL) &&
                (pEstablisherFrame == pExInfo->m_pBottomMostHandler))
            {
                // Current exception should be different from the one exception tracker is already tracking.
                _ASSERTE(pExceptionRecord != pExInfo->m_pExceptionRecord);

                // This cannot be nested exceptions - they are handled earlier (see above).
                _ASSERTE(!bNestedException);

                LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: Bottom-most handler (0x%p) is the same as EstablisherFrame.\n",
                 pExInfo->m_pBottomMostHandler));
                LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: Exception record in exception tracker is 0x%p, while that of new exception is 0x%p.\n",
                 pExInfo->m_pExceptionRecord, pExceptionRecord));
                LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: Resetting exception tracker (0x%p).\n", pExInfo));

                // This will reset the exception tracker state, including the corruption severity.
                pExInfo->Init();
            }
        }

        // If we are handling a fault from managed code, we need to set the Thread->ExInfo->pContext to
        //  the current fault context, which is used in the stack walk to get back into the managed
        //  stack with the correct registers.  (Previously, this was done by linking in a FaultingExceptionFrame
        //  record.)
        // We are about to create the managed exception object, which may trigger a GC, so set this up now.

        pExInfo->m_pExceptionRecord = pExceptionRecord;
        pExInfo->m_pContext = pContext;
        if (pContext && ShouldHandleManagedFault(pExceptionRecord, pContext, pEstablisherFrame, pThread))
        {   // If this was a fault in managed code, rather than create a Frame for stackwalking,
            //  we can use this exinfo (after all, it has all the register info.)
            pExInfo->m_ExceptionFlags.SetUseExInfoForStackwalk();
        }

        // It should now be safe for a GC to happen.

        // case 1 & 3: this is the first time through of a new, nested, or rethrown exception, so see if we can
        // find a handler.  Only setup throwable if are bottommost handler
        if (IsComPlusException(pExceptionRecord) && (!bAsynchronousThreadStop))
        {

            // Update the throwable from the last thrown object. Note: this may cause OOM, in which case we replace
            // both throwables with the preallocated OOM exception.
            pThread->SafeSetThrowables(pThread->LastThrownObject());

            // now we've got a COM+ exception, fall through to so see if we handle it

            STRESS_LOG3(LF_EH, LL_INFO10000, "CPFH_RealFirstPassHandler: fall through ExInfo:0x%p setting m_pBottomMostHandler to 0x%p from 0x%p\n",
                        pExInfo, pEstablisherFrame, pExInfo->m_pBottomMostHandler);
            pExInfo->m_pBottomMostHandler = pEstablisherFrame;
        }
        else if (bRethrownException)
        {
            // If it was rethrown and not COM+, will still be the last one thrown. Either we threw it last and
            // stashed it here or someone else caught it and rethrew it, in which case it will still have been
            // originally stashed here.

            // Update the throwable from the last thrown object. Note: this may cause OOM, in which case we replace
            // both throwables with the preallocated OOM exception.
            pThread->SafeSetThrowables(pThread->LastThrownObject());
            STRESS_LOG3(LF_EH, LL_INFO10000, "CPFH_RealFirstPassHandler: rethrow non-COM+ ExInfo:0x%p setting m_pBottomMostHandler to 0x%p from 0x%p\n",
                        pExInfo, pEstablisherFrame, pExInfo->m_pBottomMostHandler);
            pExInfo->m_pBottomMostHandler = pEstablisherFrame;
        }
        else
        {
            if (!fIsManagedCode)
            {
                tct.bDontCatch = false;
            }

            if (exceptionCode == STATUS_BREAKPOINT)
            {
                // don't catch int 3
                retval = ExceptionContinueSearch;
                goto exit;
            }

            // We need to set m_pBottomMostHandler here, Thread::IsExceptionInProgress returns 1.
            // This is a necessary part of suppressing thread abort exceptions in the constructor
            // of any exception object we might create.
            STRESS_LOG3(LF_EH, LL_INFO10000, "CPFH_RealFirstPassHandler: setting ExInfo:0x%p m_pBottomMostHandler for IsExceptionInProgress to 0x%p from 0x%p\n",
                        pExInfo, pEstablisherFrame, pExInfo->m_pBottomMostHandler);
            pExInfo->m_pBottomMostHandler = pEstablisherFrame;

            // Create the managed exception object.
            OBJECTREF throwable = CreateCOMPlusExceptionObject(pThread, pExceptionRecord, bAsynchronousThreadStop);

            // Set the throwables on the thread to the newly created object. If this fails, it will return a
            // preallocated exception object instead. This also updates the last thrown exception, for rethrows.
            throwable = pThread->SafeSetThrowables(throwable);

            // Set the exception code and pointers. We set these after setting the throwables on the thread,
            // because if the proper exception is replaced by an OOM exception, we still want the exception code
            // and pointers set in the OOM exception.
            EXCEPTIONREF exceptionRef = (EXCEPTIONREF)throwable;
            exceptionRef->SetXCode(pExceptionRecord->ExceptionCode);
            exceptionRef->SetXPtrs(&exceptionPointers);
        }

        tct.pBottomFrame = NULL;

        EEToProfilerExceptionInterfaceWrapper::ExceptionThrown(pThread);

        g_exceptionCount++;

    } // End of case-1-or-3

    {
        // Allocate storage for the stack trace.
        OBJECTREF throwable = NULL;
        GCPROTECT_BEGIN(throwable);
        throwable = pThread->GetThrowable();

        if (IsProcessCorruptedStateException(exceptionCode, throwable))
        {
            // Failfast if exception indicates corrupted process state
            EEPOLICY_HANDLE_FATAL_ERROR(exceptionCode);
        }

        // If we're out of memory, then we figure there's probably not memory to maintain a stack trace, so we skip it.
        // If we've got a stack overflow, then we figure the stack will be so huge as to make tracking the stack trace
        // impracticle, so we skip it.
        if ((throwable == CLRException::GetPreallocatedOutOfMemoryException()) ||
            (throwable == CLRException::GetPreallocatedStackOverflowException()))
        {
            tct.bAllowAllocMem = FALSE;
        }
        else
        {
            pExInfo->m_StackTraceInfo.AllocateStackTrace();
        }

        GCPROTECT_END();
    }

    // Set up information for GetExceptionPointers()/GetExceptionCode() callback.
    pExInfo->SetExceptionCode(pExceptionRecord);

    pExInfo->m_pExceptionPointers = &exceptionPointers;

    if (bRethrownException || bNestedException)
    {
        _ASSERTE(pExInfo->m_pPrevNestedInfo != NULL);
        SetStateForWatsonBucketing(bRethrownException, pExInfo->GetPreviousExceptionTracker()->GetThrowableAsHandle());
    }

#ifdef DEBUGGING_SUPPORTED
    //
    // At this point the exception is still fresh to us, so assert that
    // there should be nothing from the debugger on it.
    //
    _ASSERTE(!pExInfo->m_ExceptionFlags.DebuggerInterceptInfo());
#endif

    if (pThread->IsRudeAbort())
    {
        OBJECTREF throwable = pThread->GetThrowable();
        if (throwable == NULL || !IsExceptionOfType(kThreadAbortException, &throwable))
        {
            // Neither of these sets will throw because the throwable that we're setting is a preallocated
            // exception. This also updates the last thrown exception, for rethrows.
            pThread->SafeSetThrowables(CLRException::GetBestThreadAbortException());
        }

        if (!pThread->IsRudeAbortInitiated())
        {
            pThread->PreWorkForThreadAbort();
        }
    }

    LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: looking for handler bottom %x, top %x\n",
         tct.pBottomFrame, tct.pTopFrame));
    tct.bReplaceStack = pExInfo->m_pBottomMostHandler == pEstablisherFrame && !bRethrownException;
    tct.bSkipLastElement = bRethrownException && bNestedException;
    found = LookForHandler(&exceptionPointers,
                                pThread,
                                &tct);

    // We have searched this far.
    pExInfo->m_pSearchBoundary = tct.pTopFrame;
    LOG((LF_EH, LL_INFO1000, "CPFH_RealFirstPassHandler: set pSearchBoundary to 0x%p\n", pExInfo->m_pSearchBoundary));

    if ((found == LFH_NOT_FOUND)
#ifdef DEBUGGING_SUPPORTED
        && !pExInfo->m_ExceptionFlags.DebuggerInterceptInfo()
#endif
        )
    {
        LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: NOT_FOUND\n"));

        if (tct.pTopFrame == FRAME_TOP)
        {
            LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: NOT_FOUND at FRAME_TOP\n"));
        }

        retval = ExceptionContinueSearch;
        goto exit;
    }
    else
    {
    // so we are going to handle the exception

    // Remove the nested exception record -- before calling RtlUnwind.
    // The second-pass callback for a NestedExceptionRecord assumes that if it's
    // being unwound, it should pop one exception from the pExInfo chain.  This is
    // true for any older NestedRecords that might be unwound -- but not for the
    // new one we're about to add.  To avoid this, we remove the new record
    // before calling Unwind.
    //
    // <TODO>@NICE: This can probably be a little cleaner -- the nested record currently
    // is also used to guard the running of the filter code.  When we clean up the
    // behaviour of exceptions within filters, we should be able to get rid of this
    // PUSH/POP/PUSH behaviour.</TODO>
    _ASSERTE(bPopNestedHandlerExRecord);

    UNINSTALL_EXCEPTION_HANDLING_RECORD(&(nestedHandlerExRecord.m_ExReg));

    // Since we are going to handle the exception we switch into preemptive mode
    GCX_PREEMP_NO_DTOR();

#ifdef DEBUGGING_SUPPORTED
    //
    // Check if the debugger wants to intercept this frame at a different point than where we are.
    //
    if (pExInfo->m_ExceptionFlags.DebuggerInterceptInfo())
    {
        ClrDebuggerDoUnwindAndIntercept(pEstablisherFrame, pExceptionRecord);

        //
        // If this returns, then the debugger couldn't do it's stuff and we default to the found handler.
        //
        if (found == LFH_NOT_FOUND)
        {
            retval = ExceptionContinueSearch;
                // we need to be sure to switch back into Cooperative mode since we are going to
                // jump to the exit: label and follow the normal return path (it is expected that
                // CPFH_RealFirstPassHandler returns in COOP.
                GCX_PREEMP_NO_DTOR_END();
            goto exit;
        }
    }
#endif

    LOG((LF_EH, LL_INFO100, "CPFH_RealFirstPassHandler: handler found: %s\n", tct.pFunc->m_pszDebugMethodName));

    CallRtlUnwindSafe(pEstablisherFrame, RtlUnwindCallback, pExceptionRecord, 0);
    // on x86 at least, RtlUnwind always returns

    // The CallRtlUnwindSafe could have popped the explicit frame that the tct.pBottomFrame points to (UMThunkPrestubHandler
    // does that). In such case, the tct.pBottomFrame needs to be updated to point to the first valid explicit frame.
    Frame* frame = pThread->GetFrame();
    if ((tct.pBottomFrame != NULL) && (frame > tct.pBottomFrame))
    {
        tct.pBottomFrame = frame;
    }
    // Note: we've completed the unwind pass up to the establisher frame, and we're headed off to finish our
    // cleanup and end up back in jitted code. Any more FS0 handlers pushed from this point on out will _not_ be
    // unwound.
        // Note: we are still in Preemptive mode here and that is correct, COMPlusAfterUnwind will switch us back
        // into Cooperative mode.
    return COMPlusAfterUnwind(pExceptionRecord, pEstablisherFrame, tct);
    }

exit:
    {
        // We need to be in COOP if we get here
        GCX_ASSERT_COOP();
    }

    // If we got as far as saving pExInfo, save the context pointer so it's available for the unwind.
    if (pExInfo)
    {
        pExInfo->m_pContext = pContext;
        // pExInfo->m_pExceptionPointers points to a local structure, which is now going out of scope.
        pExInfo->m_pExceptionPointers = NULL;
    }

#if defined(USE_FEF)
    if (bPopFaultingExceptionFrame)
    {
        faultingExceptionFrame.Pop();
    }
#endif // USE_FEF

    if (bPopNestedHandlerExRecord)
    {
        UNINSTALL_EXCEPTION_HANDLING_RECORD(&(nestedHandlerExRecord.m_ExReg));
    }
    return retval;
} // CPFH_RealFirstPassHandler()


//******************************************************************************
//
void InitializeExceptionHandling()
{
    WRAPPER_NO_CONTRACT;

    CLRAddVectoredHandlers();

    // Initialize the lock used for synchronizing access to the stacktrace in the exception object
    g_StackTraceArrayLock.Init(LOCK_TYPE_DEFAULT, TRUE);
}

//******************************************************************************
static inline EXCEPTION_DISPOSITION __cdecl
CPFH_FirstPassHandler(EXCEPTION_RECORD *pExceptionRecord,
                      EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
                      CONTEXT *pContext,
                      DISPATCHER_CONTEXT *pDispatcherContext)
{
    WRAPPER_NO_CONTRACT;
    EXCEPTION_DISPOSITION retval;

    _ASSERTE (!(pExceptionRecord->ExceptionFlags & (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND)));

    DWORD exceptionCode = pExceptionRecord->ExceptionCode;

    Thread *pThread = GetThread();

    STRESS_LOG4(LF_EH, LL_INFO100,
                "CPFH_FirstPassHandler: pEstablisherFrame = %x EH code = %x  EIP = %x with ESP = %x\n",
				pEstablisherFrame, exceptionCode, pContext ? GetIP(pContext) : 0, pContext ? GetSP(pContext) : 0);

    EXCEPTION_POINTERS ptrs = { pExceptionRecord, pContext };

    // Call to the vectored handler to give other parts of the Runtime a chance to jump in and take over an
    // exception before we do too much with it. The most important point in the vectored handler is not to toggle
    // the GC mode.
    DWORD filter = CLRVectoredExceptionHandler(&ptrs);

    if (filter == (DWORD) EXCEPTION_CONTINUE_EXECUTION)
    {
        return ExceptionContinueExecution;
    }
    else if (filter == EXCEPTION_CONTINUE_SEARCH)
    {
        return ExceptionContinueSearch;
    }

#if defined(STRESS_HEAP)
    //
    // Check to see if this exception is due to GCStress. Since the GCStress mechanism only injects these faults
    // into managed code, we only need to check for them in CPFH_FirstPassHandler.
    //
    if (IsGcMarker(pContext, pExceptionRecord))
    {
        return ExceptionContinueExecution;
    }
#endif // STRESS_HEAP

    // We always want to be in co-operative mode when we run this function and whenever we return
    // from it, want to go to pre-emptive mode because are returning to OS.
    BOOL disabled = pThread->PreemptiveGCDisabled();
    GCX_COOP_NO_DTOR();

    BOOL bAsynchronousThreadStop = IsThreadHijackedForThreadStop(pThread, pExceptionRecord);

    if (bAsynchronousThreadStop)
    {
        // If we ever get here in preemptive mode, we're in trouble.  We've
        // changed the thread's IP to point at a little function that throws ... if
        // the thread were to be in preemptive mode and a GC occurred, the stack
        // crawl would have been all messed up (becuase we have no frame that points
        // us back to the right place in managed code).
        _ASSERTE(disabled);

        AdjustContextForThreadStop(pThread, pContext);
        LOG((LF_EH, LL_INFO100, "CPFH_FirstPassHandler is Asynchronous Thread Stop or Abort\n"));
    }

    pThread->ResetThrowControlForThread();

    CPFH_VerifyThreadIsInValidState(pThread, exceptionCode, pEstablisherFrame);

    // If we were in cooperative mode when we came in here, then its okay to see if we should do HandleManagedFault
    // and push a FaultingExceptionFrame. If we weren't in coop mode coming in here, then it means that there's no
    // way the exception could really be from managed code. I might look like it was from managed code, but in
    // reality its a rethrow from unmanaged code, either unmanaged user code, or unmanaged EE implementation.
    if (disabled && ShouldHandleManagedFault(pExceptionRecord, pContext, pEstablisherFrame, pThread))
    {
#if defined(USE_FEF)
        HandleManagedFault(pExceptionRecord, pContext, pEstablisherFrame, pThread);
        retval = ExceptionContinueExecution;
        goto exit;
#else // USE_FEF
        // Save the context pointer in the Thread's EXInfo, so that a stack crawl can recover the
        //  register values from the fault.

        //@todo: I haven't yet found any case where we need to do anything here.  If there are none, eliminate
        //  this entire if () {} block.
#endif // USE_FEF
    }

    // OK. We're finally ready to start the real work. Nobody else grabbed the exception in front of us. Now we can
    // get started.
    retval = CPFH_RealFirstPassHandler(pExceptionRecord,
                                       pEstablisherFrame,
                                       pContext,
                                       pDispatcherContext,
                                       bAsynchronousThreadStop,
                                       disabled);

#if defined(USE_FEF) // This label is only used in the HandleManagedFault() case above.
exit:
#endif
    if (retval != ExceptionContinueExecution || !disabled)
    {
        GCX_PREEMP_NO_DTOR();
    }

    STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_FirstPassHandler: exiting with retval %d\n", retval);
    return retval;
} // CPFH_FirstPassHandler()

//******************************************************************************
inline void
CPFH_UnwindFrames1(Thread* pThread, EXCEPTION_REGISTRATION_RECORD* pEstablisherFrame, DWORD exceptionCode)
{
    WRAPPER_NO_CONTRACT;

    ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);

    // Ready to unwind the stack...
    ThrowCallbackType tct;
    tct.Init();
    tct.bIsUnwind = TRUE;
    tct.pTopFrame = GetCurrFrame(pEstablisherFrame); // highest frame to search to
    tct.pBottomFrame = NULL;

    #ifdef _DEBUG
    tct.pCurrentExceptionRecord = pEstablisherFrame;
    tct.pPrevExceptionRecord = GetPrevSEHRecord(pEstablisherFrame);
    #endif

    #ifdef DEBUGGING_SUPPORTED
        EXCEPTION_REGISTRATION_RECORD *pInterceptEstablisherFrame = NULL;

        // If the exception is intercepted, use information stored in the DebuggerExState to unwind the stack.
        if (pExInfo->m_ExceptionFlags.DebuggerInterceptInfo())
        {
            pExInfo->m_DebuggerExState.GetDebuggerInterceptInfo(&pInterceptEstablisherFrame,
                                              NULL,     // MethodDesc **ppFunc,
                                              NULL,     // int *pdHandler,
                                              NULL,     // BYTE **ppStack
                                              NULL,     // ULONG_PTR *pNativeOffset,
                                              NULL      // Frame **ppFrame)
                                             );
            LOG((LF_EH, LL_INFO1000, "CPFH_UnwindFrames1: frames are Est 0x%X, Intercept 0x%X\n",
                 pEstablisherFrame, pInterceptEstablisherFrame));

            //
            // When we set up for the interception we store off the CPFH or CPNEH that we
            // *know* will handle unwinding the destination of the intercept.
            //
            // However, a CPNEH with the same limiting Capital-F-rame could do the work
            // and unwind us, so...
            //
            // If this is the exact frame handler we are supposed to search for, or
            // if this frame handler services the same Capital-F-rame as the frame handler
            // we are looking for (i.e. this frame handler may do the work that we would
            // expect our frame handler to do),
            // then
            //   we need to pass the interception destination during this unwind.
            //
            _ASSERTE(IsUnmanagedToManagedSEHHandler(pEstablisherFrame));

            if ((pEstablisherFrame == pInterceptEstablisherFrame) ||
                (GetCurrFrame(pEstablisherFrame) == GetCurrFrame(pInterceptEstablisherFrame)))
            {
                pExInfo->m_DebuggerExState.GetDebuggerInterceptInfo(NULL,
                                              &(tct.pFunc),
                                              &(tct.dHandler),
                                              &(tct.pStack),
                                              NULL,
                                              &(tct.pBottomFrame)
                                             );

                LOG((LF_EH, LL_INFO1000, "CPFH_UnwindFrames1: going to: pFunc:%#X, pStack:%#X\n",
                    tct.pFunc, tct.pStack));

            }

        }
    #endif

    UnwindFrames(pThread, &tct);

    LOG((LF_EH, LL_INFO1000, "CPFH_UnwindFrames1: after unwind ec:%#x, tct.pTopFrame:0x%p, pSearchBndry:0x%p\n"
                             "                    pEstFrame:0x%p, IsC+NestExRec:%d, !Nest||Active:%d\n",
         exceptionCode, tct.pTopFrame, pExInfo->m_pSearchBoundary, pEstablisherFrame,
         IsComPlusNestedExceptionRecord(pEstablisherFrame),
         (!IsComPlusNestedExceptionRecord(pEstablisherFrame) || reinterpret_cast<NestedHandlerExRecord*>(pEstablisherFrame)->m_ActiveForUnwind)));

    if (tct.pTopFrame >= pExInfo->m_pSearchBoundary &&
         (!IsComPlusNestedExceptionRecord(pEstablisherFrame) ||
          reinterpret_cast<NestedHandlerExRecord*>(pEstablisherFrame)->m_ActiveForUnwind) )
    {
        // If this is the search boundary, and we're not a nested handler, then
        // this is the last time we'll see this exception.  Time to unwind our
        // exinfo.
        STRESS_LOG0(LF_EH, LL_INFO100, "CPFH_UnwindFrames1: Exception unwind -- unmanaged catcher detected\n");
        pExInfo->UnwindExInfo((VOID*)pEstablisherFrame);
    }
} // CPFH_UnwindFrames1()

//******************************************************************************
inline EXCEPTION_DISPOSITION __cdecl
CPFH_UnwindHandler(EXCEPTION_RECORD *pExceptionRecord,
                   EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
                   CONTEXT *pContext,
                   void *pDispatcherContext)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE (pExceptionRecord->ExceptionFlags & (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND));

    #ifdef _DEBUG
    // Note: you might be inclined to write "static int breakOnSecondPass = CLRConfig::GetConfigValue(...);", but
    // you can't do that here. That causes C++ EH to be generated under the covers for this function, and this
    // function isn't allowed to have any C++ EH in it because its never going to return.
    static int breakOnSecondPass; // = 0
    static BOOL breakOnSecondPassSetup; // = FALSE
    if (!breakOnSecondPassSetup)
    {
        breakOnSecondPass = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnSecondPass);
        breakOnSecondPassSetup = TRUE;
    }
    if (breakOnSecondPass != 0)
    {
        _ASSERTE(!"Unwind handler");
    }
    #endif

    DWORD exceptionCode = pExceptionRecord->ExceptionCode;
    Thread *pThread = GetThread();

    ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);

    STRESS_LOG4(LF_EH, LL_INFO100, "In CPFH_UnwindHandler EHCode = %x EIP = %x with ESP = %x, pEstablisherFrame = 0x%p\n", exceptionCode,
        pContext ? GetIP(pContext) : 0, pContext ? GetSP(pContext) : 0, pEstablisherFrame);

    // We always want to be in co-operative mode when we run this function.  Whenever we return
    // from it, want to go to pre-emptive mode because are returning to OS.

    {
        // needs to be in its own scope to avoid polluting the namespace, since
        // we don't do a _END then we don't revert the state
        GCX_COOP_NO_DTOR();
    }

    CPFH_VerifyThreadIsInValidState(pThread, exceptionCode, pEstablisherFrame);

    if (IsComPlusNestedExceptionRecord(pEstablisherFrame))
    {
        NestedHandlerExRecord *pHandler = reinterpret_cast<NestedHandlerExRecord*>(pEstablisherFrame);
        if (pHandler->m_pCurrentExInfo != NULL)
        {
            // See the comment at the end of COMPlusNestedExceptionHandler about nested exception.
            // OS is going to skip the EstablisherFrame before our NestedHandler.
            if (pHandler->m_pCurrentExInfo->m_pBottomMostHandler <= pHandler->m_pCurrentHandler)
            {
                // We're unwinding -- the bottom most handler is potentially off top-of-stack now.  If
                // it is, change it to the next COM+ frame.  (This one is not good, as it's about to
                // disappear.)
                EXCEPTION_REGISTRATION_RECORD *pNextBottomMost = GetNextCOMPlusSEHRecord(pHandler->m_pCurrentHandler);

                STRESS_LOG3(LF_EH, LL_INFO10000, "COMPlusNestedExceptionHandler: setting ExInfo:0x%p m_pBottomMostHandler from 0x%p to 0x%p\n",
                    pHandler->m_pCurrentExInfo, pHandler->m_pCurrentExInfo->m_pBottomMostHandler, pNextBottomMost);

                pHandler->m_pCurrentExInfo->m_pBottomMostHandler = pNextBottomMost;
            }
        }
    }

    // this establishes a marker so can determine if are processing a nested exception
    // don't want to use the current frame to limit search as it could have been unwound by
    // the time get to nested handler (ie if find an exception, unwind to the call point and
    // then resume in the catch and then get another exception) so make the nested handler
    // have the same boundary as this one. If nested handler can't find a handler, we won't
    // end up searching this frame list twice because the nested handler will set the search
    // boundary in the thread and so if get back to this handler it will have a range that starts
    // and ends at the same place.
    NestedHandlerExRecord nestedHandlerExRecord;
    nestedHandlerExRecord.Init((PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler, GetCurrFrame(pEstablisherFrame));

    nestedHandlerExRecord.m_ActiveForUnwind = TRUE;
        nestedHandlerExRecord.m_pCurrentExInfo = pExInfo;
        nestedHandlerExRecord.m_pCurrentHandler = pEstablisherFrame;

    INSTALL_EXCEPTION_HANDLING_RECORD(&(nestedHandlerExRecord.m_ExReg));

    // Unwind the stack.  The establisher frame sets the boundary.
    CPFH_UnwindFrames1(pThread, pEstablisherFrame, exceptionCode);

    // We're unwinding -- the bottom most handler is potentially off top-of-stack now.  If
    // it is, change it to the next COM+ frame.  (This one is not good, as it's about to
    // disappear.)
    if (pExInfo->m_pBottomMostHandler &&
        pExInfo->m_pBottomMostHandler <= pEstablisherFrame)
    {
        EXCEPTION_REGISTRATION_RECORD *pNextBottomMost = GetNextCOMPlusSEHRecord(pEstablisherFrame);

        // If there is no previous COM+ SEH handler, GetNextCOMPlusSEHRecord() will return -1.  Much later, we will dereference that and AV.
        _ASSERTE (pNextBottomMost != EXCEPTION_CHAIN_END);

        STRESS_LOG3(LF_EH, LL_INFO10000, "CPFH_UnwindHandler: setting ExInfo:0x%p m_pBottomMostHandler from 0x%p to 0x%p\n",
            pExInfo, pExInfo->m_pBottomMostHandler, pNextBottomMost);

        pExInfo->m_pBottomMostHandler = pNextBottomMost;
    }

    {
        // needs to be in its own scope to avoid polluting the namespace, since
        // we don't do a _END then we don't revert the state
        GCX_PREEMP_NO_DTOR();
    }
    UNINSTALL_EXCEPTION_HANDLING_RECORD(&(nestedHandlerExRecord.m_ExReg));

    // If we are here, then exception was not caught in managed code protected by this
    // ComplusFrameHandler. Hence, reset thread abort state if this is the last personality routine,
    // for managed code, on the stack.
    ResetThreadAbortState(pThread, pEstablisherFrame);

    STRESS_LOG0(LF_EH, LL_INFO100, "CPFH_UnwindHandler: Leaving with ExceptionContinueSearch\n");
    return ExceptionContinueSearch;
} // CPFH_UnwindHandler()

//******************************************************************************
// This is the first handler that is called in the context of managed code
// It is the first level of defense and tries to find a handler in the user
// code to handle the exception
//-------------------------------------------------------------------------
// EXCEPTION_DISPOSITION __cdecl COMPlusFrameHandler(
//     EXCEPTION_RECORD *pExceptionRecord,
//     _EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
//     CONTEXT *pContext,
//     DISPATCHER_CONTEXT *pDispatcherContext)
//
// See http://www.microsoft.com/msj/0197/exception/exception.aspx for a background piece on Windows
// unmanaged structured exception handling.
EXCEPTION_HANDLER_IMPL(COMPlusFrameHandler)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!DebugIsEECxxException(pExceptionRecord) && "EE C++ Exception leaked into managed code!");

    STRESS_LOG5(LF_EH, LL_INFO100, "In COMPlusFrameHandler EH code = %x  flag = %x EIP = %x with ESP = %x, pEstablisherFrame = 0x%p\n",
        pExceptionRecord->ExceptionCode, pExceptionRecord->ExceptionFlags,
        pContext ? GetIP(pContext) : 0, pContext ? GetSP(pContext) : 0, pEstablisherFrame);

    _ASSERTE((pContext == NULL) || ((pContext->ContextFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL));

    if (g_fNoExceptions)
        return ExceptionContinueSearch; // No EH during EE shutdown.

    // Check if the exception represents a GCStress Marker. If it does,
    // we shouldnt record its entry in the TLS as such exceptions are
    // continuable and can confuse the VM to treat them as CSE,
    // as they are implemented using illegal instruction exception.

    bool fIsGCMarker = false;

#ifdef HAVE_GCCOVER // This is a debug only macro
    if (GCStress<cfg_instr_jit>::IsEnabled())
    {
        // TlsGetValue trashes last error. When Complus_GCStress=4, GC is invoked
        // on every allowable JITed instruction by means of our exception handling machanism
        // it is very easy to trash the last error. For example, a p/invoke called a native method
        // which sets last error. Before we getting the last error in the IL stub, it is trashed here
        DWORD dwLastError = GetLastError();
        fIsGCMarker = IsGcMarker(pContext, pExceptionRecord);
        if (!fIsGCMarker)
        {
            SaveCurrentExceptionInfo(pExceptionRecord, pContext);
        }
        SetLastError(dwLastError);
    }
    else
#endif
    {
        // GCStress does not exist on retail builds (see IsGcMarker implementation for details).
        SaveCurrentExceptionInfo(pExceptionRecord, pContext);
    }

    if (fIsGCMarker)
    {
        // If this was a GCStress marker exception, then return
        // ExceptionContinueExecution to the OS.
        return ExceptionContinueExecution;
    }

    EXCEPTION_DISPOSITION retVal = ExceptionContinueSearch;

    Thread *pThread = GetThread();
    if ((pExceptionRecord->ExceptionFlags & (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND)) == 0)
    {
        if (pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
        {
            EEPolicy::HandleStackOverflow();

            // VC's unhandled exception filter plays with stack.  It VirtualAlloc's a new stack, and
            // then launch Watson from the new stack.  When Watson asks CLR to save required data, we
            // are not able to walk the stack.
            // Setting Context in ExInfo so that our Watson dump routine knows how to walk this stack.
            ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);
            pExInfo->m_pContext = pContext;

            // Save the reference to the topmost handler we see during first pass when an SO goes past us.
            // When an unwind gets triggered for the exception, we will reset the frame chain when we reach
            // the topmost handler we saw during the first pass.
            //
            // This unifies, behaviour-wise, 32bit with 64bit.
            if ((pExInfo->m_pTopMostHandlerDuringSO == NULL) ||
                (pEstablisherFrame > pExInfo->m_pTopMostHandlerDuringSO))
            {
                pExInfo->m_pTopMostHandlerDuringSO = pEstablisherFrame;
            }

            // Switch to preemp mode since we are returning back to the OS.
            // We will do the quick switch since we are short of stack
            InterlockedAnd((LONG*)&pThread->m_fPreemptiveGCDisabled, 0);

            return ExceptionContinueSearch;
        }
    }
    else
    {
        DWORD exceptionCode = pExceptionRecord->ExceptionCode;

        if (exceptionCode == STATUS_UNWIND)
        {
            // If exceptionCode is STATUS_UNWIND, RtlUnwind is called with a NULL ExceptionRecord,
            // therefore OS uses a faked ExceptionRecord with STATUS_UNWIND code.  Then we need to
            // look at our saved exception code.
            exceptionCode = GetCurrentExceptionCode();
        }

        if (exceptionCode == STATUS_STACK_OVERFLOW)
        {
            // We saved the context during the first pass in case the stack overflow exception is
            // unhandled and Watson dump code needs it.  Now we are in the second pass, therefore
            // either the exception is handled by user code, or we have finished unhandled exception
            // filter process, and the OS is unwinding the stack.  Either way, we don't need the
            // context any more.  It is very important to reset the context so that our code does not
            // accidentally walk the frame using the dangling context in ExInfoWalker::WalkToPosition.
            ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);
            pExInfo->m_pContext = NULL;

            // We should have the reference to the topmost handler seen during the first pass of SO
            _ASSERTE(pExInfo->m_pTopMostHandlerDuringSO != NULL);

            // Reset frame chain till we reach the topmost establisher frame we saw in the first pass.
            // This will ensure that if any intermediary frame calls back into managed (e.g. native frame
            // containing a __finally that reverse pinvokes into managed), then we have the correct
            // explicit frame on the stack. Resetting the frame chain only when we reach the topmost
            // personality routine seen in the first pass may not result in expected behaviour,
            // specially during stack walks when crawl frame needs to be initialized from
            // explicit frame.
            if (pEstablisherFrame <= pExInfo->m_pTopMostHandlerDuringSO)
            {
                GCX_COOP_NO_DTOR();

                if (pThread->GetFrame() < GetCurrFrame(pEstablisherFrame))
                {
                    // We are very short of stack.  We avoid calling UnwindFrame which may
                    // run unknown code here.
                    pThread->SetFrame(GetCurrFrame(pEstablisherFrame));
                }
            }

            // Switch to preemp mode since we are returning back to the OS.
            // We will do the quick switch since we are short of stack
            InterlockedAnd((LONG*)&pThread->m_fPreemptiveGCDisabled, 0);

            return ExceptionContinueSearch;
        }
    }

    if (pExceptionRecord->ExceptionFlags & (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND))
    {
        retVal =  CPFH_UnwindHandler(pExceptionRecord,
                                     pEstablisherFrame,
                                     pContext,
                                     pDispatcherContext);
    }
    else
    {

        /* Make no assumptions about the current machine state.
           <TODO>@PERF: Only needs to be called by the very first handler invoked by SEH </TODO>*/
        ResetCurrentContext();

        retVal = CPFH_FirstPassHandler(pExceptionRecord,
                                       pEstablisherFrame,
                                       pContext,
                                       pDispatcherContext);

    }

    return retVal;
} // COMPlusFrameHandler()


//-------------------------------------------------------------------------
// This is called by the EE to restore the stack pointer if necessary.
//-------------------------------------------------------------------------

// This can't be inlined into the caller to avoid introducing EH frame
NOINLINE LPVOID COMPlusEndCatchWorker(Thread * pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    LOG((LF_EH, LL_INFO1000, "COMPlusPEndCatch:called with "
        "pThread:0x%x\n",pThread));

    // indicate that we are out of the managed clause as early as possible
    ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);
    pExInfo->m_EHClauseInfo.SetManagedCodeEntered(FALSE);

    void* esp = NULL;

    // Notify the profiler that the catcher has finished running
    // IL stubs don't contain catch blocks so inability to perform this check does not matter.
    // if (!pFunc->IsILStub())
    EEToProfilerExceptionInterfaceWrapper::ExceptionCatcherLeave();

    // no need to set pExInfo->m_ClauseType = (DWORD)COR_PRF_CLAUSE_NONE now that the
    // notification is done because the ExInfo record is about to be popped off anyway

    LOG((LF_EH, LL_INFO1000, "COMPlusPEndCatch:pThread:0x%x\n",pThread));

#ifdef _DEBUG
    gLastResumedExceptionFunc = NULL;
    gLastResumedExceptionHandler = 0;
#endif
    // Set the thrown object to NULL as no longer needed. This also sets the last thrown object to NULL.
    pThread->SafeSetThrowables(NULL);

    // reset the stashed exception info
    pExInfo->m_pExceptionRecord = NULL;
    pExInfo->m_pContext = NULL;
    pExInfo->m_pExceptionPointers = NULL;

    if  (pExInfo->m_pShadowSP)
    {
        *pExInfo->m_pShadowSP = 0;  // Reset the shadow SP
    }

    // pExInfo->m_dEsp was set in ResumeAtJITEH(). It is the Esp of the
    // handler nesting level which catches the exception.
    esp = (void*)(size_t)pExInfo->m_dEsp;

    pExInfo->UnwindExInfo(esp);

    // Prepare to sync managed exception state
    //
    // In a case when we're nested inside another catch block, the domain in which we're executing may not be the
    // same as the one the domain of the throwable that was just made the current throwable above. Therefore, we
    // make a special effort to preserve the domain of the throwable as we update the last thrown object.
    //
    // This function (COMPlusEndCatch) can also be called by the in-proc debugger helper thread on x86 when
    // an attempt to SetIP takes place to set IP outside the catch clause. In such a case, managed thread object
    // will not be available. Thus, we should reset the severity only if its not such a thread.
    //
    // This behaviour (of debugger doing SetIP) is not allowed on 64bit since the catch clauses are implemented
    // as a separate funclet and it's just not allowed to set the IP across EH scopes, such as from inside a catch
    // clause to outside of the catch clause.
    bool fIsDebuggerHelperThread = (g_pDebugInterface == NULL) ? false : g_pDebugInterface->ThisIsHelperThread();

    // Sync managed exception state, for the managed thread, based upon any active exception tracker
    pThread->SyncManagedExceptionState(fIsDebuggerHelperThread);

    LOG((LF_EH, LL_INFO1000, "COMPlusPEndCatch: esp=%p\n", esp));

    return esp;
}

//
// This function works in conjunction with JIT_EndCatch.  On input, the parameters are set as follows:
//    ebp, ebx, edi, esi: the values of these registers at the end of the catch block
//    *pRetAddress: the next instruction after the call to JIT_EndCatch
//
// On output, *pRetAddress is the instruction at which to resume execution.  This may be user code,
// or it may be ThrowControlForThread (which will re-raise a pending ThreadAbortException).
//
// Returns the esp to set before resuming at *pRetAddress.
//
LPVOID STDCALL COMPlusEndCatch(LPVOID ebp, DWORD ebx, DWORD edi, DWORD esi, LPVOID* pRetAddress)
{
    //
    // PopNestedExceptionRecords directly manipulates fs:[0] chain. This method can't have any EH!
    //
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    ETW::ExceptionLog::ExceptionCatchEnd();
    ETW::ExceptionLog::ExceptionThrownEnd();

    void* esp = COMPlusEndCatchWorker(GetThread());

    // We are going to resume at a handler nesting level whose esp is dEsp. Pop off any SEH records below it. This
    // would be the COMPlusNestedExceptionHandler we had inserted.
    PopNestedExceptionRecords(esp);

    //
    // Set up m_OSContext for the call to COMPlusCheckForAbort
    //
    Thread* pThread = GetThread();

    SetIP(pThread->m_OSContext, (PCODE)*pRetAddress);
    SetSP(pThread->m_OSContext, (TADDR)esp);
    SetFP(pThread->m_OSContext, (TADDR)ebp);
    pThread->m_OSContext->Ebx = ebx;
    pThread->m_OSContext->Edi = edi;
    pThread->m_OSContext->Esi = esi;

    LPVOID throwControl = COMPlusCheckForAbort((UINT_PTR)*pRetAddress);
    if (throwControl)
        *pRetAddress = throwControl;

    return esp;
}

PEXCEPTION_REGISTRATION_RECORD GetCurrentSEHRecord()
{
    WRAPPER_NO_CONTRACT;

    LPVOID fs0 = (LPVOID)__readfsdword(0);

#if 0  // This walk is too expensive considering we hit it every time we a CONTRACT(NOTHROW)
#ifdef _DEBUG
    EXCEPTION_REGISTRATION_RECORD *pEHR = (EXCEPTION_REGISTRATION_RECORD *)fs0;
    LPVOID spVal;
    __asm {
        mov spVal, esp
    }

    // check that all the eh frames are all greater than the current stack value. If not, the
    // stack has been updated somehow w/o unwinding the SEH chain.

    // LOG((LF_EH, LL_INFO1000000, "ER Chain:\n"));
    while (pEHR != NULL && pEHR != EXCEPTION_CHAIN_END) {
        // LOG((LF_EH, LL_INFO1000000, "\tp: prev:p handler:%x\n", pEHR, pEHR->Next, pEHR->Handler));
        if (pEHR < spVal) {
            if (gLastResumedExceptionFunc != 0)
                _ASSERTE(!"Stack is greater than start of SEH chain - possible missing leave in handler. See gLastResumedExceptionHandler & gLastResumedExceptionFunc for info");
            else
                _ASSERTE(!"Stack is greater than start of SEH chain (FS:0)");
        }
        if (pEHR->Handler == (void *)-1)
            _ASSERTE(!"Handler value has been corrupted");

            _ASSERTE(pEHR < pEHR->Next);

        pEHR = pEHR->Next;
    }
#endif
#endif // 0

    return (EXCEPTION_REGISTRATION_RECORD*) fs0;
}

PEXCEPTION_REGISTRATION_RECORD GetFirstCOMPlusSEHRecord(Thread *pThread) {
    WRAPPER_NO_CONTRACT;
    EXCEPTION_REGISTRATION_RECORD *pEHR = *(pThread->GetExceptionListPtr());
    if (pEHR == EXCEPTION_CHAIN_END || IsUnmanagedToManagedSEHHandler(pEHR)) {
        return pEHR;
    } else {
        return GetNextCOMPlusSEHRecord(pEHR);
    }
}


PEXCEPTION_REGISTRATION_RECORD GetPrevSEHRecord(EXCEPTION_REGISTRATION_RECORD *next)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsUnmanagedToManagedSEHHandler(next));

    EXCEPTION_REGISTRATION_RECORD *pEHR = GetCurrentSEHRecord();
    _ASSERTE(pEHR != 0 && pEHR != EXCEPTION_CHAIN_END);

    EXCEPTION_REGISTRATION_RECORD *pBest = 0;
    while (pEHR != next) {
        if (IsUnmanagedToManagedSEHHandler(pEHR))
            pBest = pEHR;
        pEHR = pEHR->Next;
        _ASSERTE(pEHR != 0 && pEHR != EXCEPTION_CHAIN_END);
    }

    return pBest;
}

VOID SetCurrentSEHRecord(EXCEPTION_REGISTRATION_RECORD *pSEH)
{
    WRAPPER_NO_CONTRACT;
    *GetThread()->GetExceptionListPtr() = pSEH;
}

// Note that this logic is copied below, in PopSEHRecords
__declspec(naked)
VOID __cdecl PopSEHRecords(LPVOID pTargetSP)
{
    // No CONTRACT possible on naked functions
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    __asm{
        mov     ecx, [esp+4]        ;; ecx <- pTargetSP
        mov     eax, fs:[0]         ;; get current SEH record
  poploop:
        cmp     eax, ecx
        jge     done
        mov     eax, [eax]          ;; get next SEH record
        jmp     poploop
  done:
        mov     fs:[0], eax
        retn
    }
}

//
// Unwind pExinfo, pops FS:[0] handlers until the interception context SP, and
// resumes at interception context.
//
VOID UnwindExceptionTrackerAndResumeInInterceptionFrame(ExInfo* pExInfo, EHContext* context)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    _ASSERTE(pExInfo && context);

    pExInfo->UnwindExInfo((LPVOID)(size_t)context->Esp);
    PopNestedExceptionRecords((LPVOID)(size_t)context->Esp);

    STRESS_LOG3(LF_EH|LF_CORDB, LL_INFO100, "UnwindExceptionTrackerAndResumeInInterceptionFrame: completing intercept at EIP = %p  ESP = %p EBP = %p\n", context->Eip, context->Esp, context->Ebp);

    ResumeAtJitEHHelper(context);
    UNREACHABLE_MSG("Should never return from ResumeAtJitEHHelper!");
}

//
// Pop SEH records below the given target ESP. This is only used to pop nested exception records.
// If bCheckForUnknownHandlers is set, it only checks for unknown FS:[0] handlers.
//
BOOL PopNestedExceptionRecords(LPVOID pTargetSP, BOOL bCheckForUnknownHandlers)
{
    // No CONTRACT here, because we can't run the risk of it pushing any SEH into the current method.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    PEXCEPTION_REGISTRATION_RECORD pEHR = GetCurrentSEHRecord();

    while ((LPVOID)pEHR < pTargetSP)
    {
        //
        // The only handler types we're allowed to have below the limit on the FS:0 chain in these cases are a
        // nested exception record or a fast NExport record, so we verify that here.
        //
        // There is a special case, of course: for an unhandled exception, when the default handler does the exit
        // unwind, we may have an exception that escapes a finally clause, thus replacing the original unhandled
        // exception. If we find a catcher for that new exception, then we'll go ahead and do our own unwind, then
        // jump to the catch. When we are called here, just before jumpping to the catch, we'll pop off our nested
        // handlers, then we'll pop off one more handler: the handler that ntdll!ExecuteHandler2 pushed before
        // calling our nested handler. We go ahead and pop off that handler, too. Its okay, its only there to catch
        // exceptions from handlers and turn them into collided unwind status codes... there's no cleanup in the
        // handler that we're removing, and that's the important point. The handler that ExecuteHandler2 pushes
        // isn't a public export from ntdll, but its named "UnwindHandler" and is physically shortly after
        // ExecuteHandler2 in ntdll.
        // In this case, we don't want to pop off the NExportSEH handler since it's our outermost handler.
        //
        static HINSTANCE ExecuteHandler2Module = 0;
        static BOOL ExecuteHandler2ModuleInited = FALSE;

        // Cache the handle to the dll with the handler pushed by ExecuteHandler2.
        if (!ExecuteHandler2ModuleInited)
        {
            ExecuteHandler2Module = WszGetModuleHandle(W("ntdll.dll"));
            ExecuteHandler2ModuleInited = TRUE;
        }

        if (bCheckForUnknownHandlers)
        {
            if (!IsComPlusNestedExceptionRecord(pEHR) ||
                !((ExecuteHandler2Module != NULL) && IsIPInModule(ExecuteHandler2Module, (PCODE)pEHR->Handler)))
            {
                return TRUE;
            }
        }
#ifdef _DEBUG
        else
        {
            // Note: if we can't find the module containing ExecuteHandler2, we'll just be really strict and require
            // that we're only popping nested handlers or the FastNExportSEH handler.
            _ASSERTE(FastNExportSEH(pEHR) || IsComPlusNestedExceptionRecord(pEHR) ||
                     ((ExecuteHandler2Module != NULL) && IsIPInModule(ExecuteHandler2Module, (PCODE)pEHR->Handler)));
        }
#endif // _DEBUG

        pEHR = pEHR->Next;
    }

    if (!bCheckForUnknownHandlers)
    {
        SetCurrentSEHRecord(pEHR);
    }
    return FALSE;
}

//
// This is implemented differently from the PopNestedExceptionRecords above because it's called in the context of
// the DebuggerRCThread to operate on the stack of another thread.
//
VOID PopNestedExceptionRecords(LPVOID pTargetSP, CONTEXT *pCtx, void *pSEH)
{
    // No CONTRACT here, because we can't run the risk of it pushing any SEH into the current method.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef _DEBUG
    LOG((LF_CORDB,LL_INFO1000, "\nPrintSEHRecords:\n"));

    EXCEPTION_REGISTRATION_RECORD *pEHR = (EXCEPTION_REGISTRATION_RECORD *)(size_t)*(DWORD *)pSEH;

    // check that all the eh frames are all greater than the current stack value. If not, the
    // stack has been updated somehow w/o unwinding the SEH chain.
    while (pEHR != NULL && pEHR != EXCEPTION_CHAIN_END)
    {
        LOG((LF_EH, LL_INFO1000000, "\t%08x: next:%08x handler:%x\n", pEHR, pEHR->Next, pEHR->Handler));
        pEHR = pEHR->Next;
    }
#endif

    DWORD dwCur = *(DWORD*)pSEH; // 'EAX' in the original routine
    DWORD dwPrev = (DWORD)(size_t)pSEH;

    while (dwCur < (DWORD)(size_t)pTargetSP)
    {
        // Watch for the OS handler
        // for nested exceptions, or any C++ handlers for destructors in our call
        // stack, or anything else.
        if (dwCur < (DWORD)GetSP(pCtx))
            dwPrev = dwCur;

        dwCur = *(DWORD *)(size_t)dwCur;

        LOG((LF_CORDB,LL_INFO10000, "dwCur: 0x%x dwPrev:0x%x pTargetSP:0x%x\n",
            dwCur, dwPrev, pTargetSP));
    }

    *(DWORD *)(size_t)dwPrev = dwCur;

#ifdef _DEBUG
    pEHR = (EXCEPTION_REGISTRATION_RECORD *)(size_t)*(DWORD *)pSEH;
    // check that all the eh frames are all greater than the current stack value. If not, the
    // stack has been updated somehow w/o unwinding the SEH chain.

    LOG((LF_CORDB,LL_INFO1000, "\nPopSEHRecords:\n"));
    while (pEHR != NULL && pEHR != (void *)-1)
    {
        LOG((LF_EH, LL_INFO1000000, "\t%08x: next:%08x handler:%x\n", pEHR, pEHR->Next, pEHR->Handler));
        pEHR = pEHR->Next;
    }
#endif
}

//==========================================================================
// COMPlusThrowCallback
//
//==========================================================================

/*
 *
 * COMPlusThrowCallbackHelper
 *
 * This function is a simple helper function for COMPlusThrowCallback.  It is needed
 * because of the EX_TRY macro.  This macro does an alloca(), which allocates space
 * off the stack, not free'ing it.  Thus, doing a EX_TRY in a loop can easily result
 * in a stack overflow error.  By factoring out the EX_TRY into a separate function,
 * we recover that stack space.
 *
 * Parameters:
 *   pJitManager - The JIT manager that will filter the EH.
 *   pCf - The frame to crawl.
 *   EHClausePtr
 *   nestingLevel
 *   pThread - Used to determine if the thread is throwable or not.
 *
 * Return:
 *   Exception status.
 *
 */
int COMPlusThrowCallbackHelper(IJitManager *pJitManager,
                               CrawlFrame *pCf,
                               ThrowCallbackType* pData,
                               EE_ILEXCEPTION_CLAUSE  *EHClausePtr,
                               DWORD nestingLevel,
                               OBJECTREF throwable,
                               Thread *pThread
                              )
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    int iFilt = 0;

    EX_TRY
    {
        GCPROTECT_BEGIN (throwable);

        // We want to call filters even if the thread is aborting, so suppress abort
        // checks while the filter runs.
        ThreadPreventAsyncHolder preventAbort;

        BYTE* startAddress = (BYTE*)pCf->GetCodeInfo()->GetStartAddress();
        iFilt = ::CallJitEHFilter(pCf, startAddress, EHClausePtr, nestingLevel, throwable);

        GCPROTECT_END();
    }
    EX_CATCH
    {
        // We had an exception in filter invocation that remained unhandled.
        // Sync managed exception state, for the managed thread, based upon the active exception tracker.
        pThread->SyncManagedExceptionState(false);

        //
        // Swallow exception.  Treat as exception continue search.
        //
        iFilt = EXCEPTION_CONTINUE_SEARCH;

    }
    EX_END_CATCH(SwallowAllExceptions)

    return iFilt;
}

//******************************************************************************
// The stack walk callback for exception handling on x86.
// Returns one of:
//    SWA_CONTINUE    = 0,    // continue walking
//    SWA_ABORT       = 1,    // stop walking, early out in "failure case"
//    SWA_FAILED      = 2     // couldn't walk stack
StackWalkAction COMPlusThrowCallback(       // SWA value
    CrawlFrame  *pCf,                       // Data from StackWalkFramesEx
    ThrowCallbackType *pData)               // Context data passed through from CPFH
{
    // We don't want to use a runtime contract here since this codepath is used during
    // the processing of a hard SO. Contracts use a significant amount of stack
    // which we can't afford for those cases.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    Frame *pFrame = pCf->GetFrame();
    MethodDesc *pFunc = pCf->GetFunction();

    #if defined(_DEBUG)
    #define METHODNAME(pFunc) (pFunc?pFunc->m_pszDebugMethodName:"<n/a>")
    #else
    #define METHODNAME(pFunc) "<n/a>"
    #endif
    STRESS_LOG4(LF_EH, LL_INFO100, "COMPlusThrowCallback: STACKCRAWL method:%pM ('%s'), Frame:%p, FrameVtable = %pV\n",
        pFunc, METHODNAME(pFunc), pFrame, pCf->IsFrameless()?0:(*(void**)pFrame));
    #undef METHODNAME

    Thread *pThread = GetThread();

    if (pFrame && pData->pTopFrame == pFrame)
        /* Don't look past limiting frame if there is one */
        return SWA_ABORT;

    if (!pFunc)
        return SWA_CONTINUE;

    if (pThread->IsRudeAbortInitiated())
    {
        return SWA_CONTINUE;
    }

    ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);

    _ASSERTE(!pData->bIsUnwind);
#ifdef _DEBUG
    // It SHOULD be the case that any frames we consider live between this exception
    // record and the previous one.
    if (!pExInfo->m_pPrevNestedInfo) {
        if (pData->pCurrentExceptionRecord) {
            if (pFrame) _ASSERTE(pData->pCurrentExceptionRecord > pFrame);
            // The FastNExport SEH handler can be in the frame we just unwound and as a result just out of range.
            if (pCf->IsFrameless() && !FastNExportSEH((PEXCEPTION_REGISTRATION_RECORD)pData->pCurrentExceptionRecord))
            {
                _ASSERTE((ULONG_PTR)pData->pCurrentExceptionRecord >= GetRegdisplaySP(pCf->GetRegisterSet()));
            }
        }
        if (pData->pPrevExceptionRecord) {
            // FCALLS have an extra SEH record in debug because of the desctructor
            // associated with ForbidGC checking.  This is benign, so just ignore it.
            if (pFrame) _ASSERTE(pData->pPrevExceptionRecord < pFrame || pFrame->GetVTablePtr() == HelperMethodFrame::GetMethodFrameVPtr());
            if (pCf->IsFrameless()) _ASSERTE((ULONG_PTR)pData->pPrevExceptionRecord <= GetRegdisplaySP(pCf->GetRegisterSet()));
        }
    }
#endif

    UINT_PTR currentIP = 0;
    UINT_PTR currentSP = 0;

    if (pCf->IsFrameless())
    {
        currentIP = (UINT_PTR)GetControlPC(pCf->GetRegisterSet());
        currentSP = (UINT_PTR)GetRegdisplaySP(pCf->GetRegisterSet());
    }
    else if (InlinedCallFrame::FrameHasActiveCall(pFrame))
    {
        // don't have the IP, SP for native code
        currentIP = 0;
        currentSP = 0;
    }
    else
    {
        currentIP = (UINT_PTR)(pCf->GetFrame()->GetIP());
        currentSP = 0; //Don't have an SP to get.
    }

    if (!pFunc->IsILStub())
    {
        // Append the current frame to the stack trace and save the save trace to the managed Exception object.
        pExInfo->m_StackTraceInfo.AppendElement(pData->bAllowAllocMem, currentIP, currentSP, pFunc, pCf);

        pExInfo->m_StackTraceInfo.SaveStackTrace(pData->bAllowAllocMem,
                                                 pThread->GetThrowableAsHandle(),
                                                 pData->bReplaceStack,
                                                 pData->bSkipLastElement);
    }
    else
    {
        LOG((LF_EH, LL_INFO1000, "COMPlusThrowCallback: Skipping AppendElement/SaveStackTrace for IL stub MD %p\n", pFunc));
    }

    // Fire an exception thrown ETW event when an exception occurs
    ETW::ExceptionLog::ExceptionThrown(pCf, pData->bSkipLastElement, pData->bReplaceStack);

    // Reset the flags.  These flags are set only once before each stack walk done by LookForHandler(), and
    // they apply only to the first frame we append to the stack trace.  Subsequent frames are always appended.
    if (pData->bReplaceStack)
    {
        pData->bReplaceStack = FALSE;
    }
    if (pData->bSkipLastElement)
    {
        pData->bSkipLastElement = FALSE;
    }

    // now we've got the stack trace, if we aren't allowed to catch this and we're first pass, return
    if (pData->bDontCatch)
        return SWA_CONTINUE;

    if (!pCf->IsFrameless())
    {
        // @todo - remove this once SIS is fully enabled.
        extern bool g_EnableSIS;
        if (g_EnableSIS)
        {
            // For debugger, we may want to notify 1st chance exceptions if they're coming out of a stub.
            // We recognize stubs as Frames with a M2U transition type. The debugger's stackwalker also
            // recognizes these frames and publishes ICorDebugInternalFrames in the stackwalk. It's
            // important to use pFrame as the stack address so that the Exception callback matches up
            // w/ the ICorDebugInternlFrame stack range.
            if (CORDebuggerAttached())
            {
                Frame * pFrameStub = pCf->GetFrame();
                Frame::ETransitionType t = pFrameStub->GetTransitionType();
                if (t == Frame::TT_M2U)
                {
                    // Use address of the frame as the stack address.
                    currentSP = (SIZE_T) ((void*) pFrameStub);
                    currentIP = 0; // no IP.
                    EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedException(pThread, (SIZE_T)currentIP, (SIZE_T)currentSP);
                    // Deliver the FirstChanceNotification after the debugger, if not already delivered.
                    if (!pExInfo->DeliveredFirstChanceNotification())
                    {
                        ExceptionNotifications::DeliverFirstChanceNotification();
                    }
                }
            }
        }
        return SWA_CONTINUE;
    }

    bool fIsILStub = pFunc->IsILStub();
    bool fGiveDebuggerAndProfilerNotification = !fIsILStub;
    BOOL fMethodCanHandleException = TRUE;

    MethodDesc * pUserMDForILStub = NULL;
    Frame * pILStubFrame = NULL;
    if (fIsILStub)
        pUserMDForILStub = GetUserMethodForILStub(pThread, currentSP, pFunc, &pILStubFrame);

    // Let the profiler know that we are searching for a handler within this function instance
    if (fGiveDebuggerAndProfilerNotification)
        EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionEnter(pFunc);

    // The following debugger notification and AppDomain::FirstChanceNotification should be scoped together
    // since the AD notification *must* follow immediately after the debugger's notification.
    {
#ifdef DEBUGGING_SUPPORTED
        //
        // Go ahead and notify any debugger of this exception.
        //
        EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedException(pThread, (SIZE_T)currentIP, (SIZE_T)currentSP);

        if (CORDebuggerAttached() && pExInfo->m_ExceptionFlags.DebuggerInterceptInfo())
        {
            return SWA_ABORT;
        }
#endif // DEBUGGING_SUPPORTED

        // Attempt to deliver the first chance notification to the AD only *AFTER* the debugger
        // has done that, provided we have not already done that.
        if (!pExInfo->DeliveredFirstChanceNotification())
        {
            ExceptionNotifications::DeliverFirstChanceNotification();
        }
    }

    IJitManager* pJitManager = pCf->GetJitManager();
    _ASSERTE(pJitManager);

    EH_CLAUSE_ENUMERATOR pEnumState;
    unsigned EHCount = pJitManager->InitializeEHEnumeration(pCf->GetMethodToken(), &pEnumState);

    if (EHCount == 0)
    {
        // Inform the profiler that we're leaving, and what pass we're on
        if (fGiveDebuggerAndProfilerNotification)
            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pFunc);
        return SWA_CONTINUE;
    }

    TypeHandle thrownType = TypeHandle();
    // if we are being called on an unwind for an exception that we did not try to catch, eg.
    // an internal EE exception, then pThread->GetThrowable will be null
    {
        OBJECTREF  throwable = pThread->GetThrowable();
        if (throwable != NULL)
        {
            throwable = PossiblyUnwrapThrowable(throwable, pCf->GetAssembly());
            thrownType = TypeHandle(throwable->GetMethodTable());
        }
    }

    PREGDISPLAY regs = pCf->GetRegisterSet();
    BYTE *pStack = (BYTE *) GetRegdisplaySP(regs);
#ifdef DEBUGGING_SUPPORTED
    BYTE *pHandlerEBP   = (BYTE *) GetRegdisplayFP(regs);
#endif

    DWORD offs = (DWORD)pCf->GetRelOffset();  //= (BYTE*) (*regs->pPC) - (BYTE*) pCf->GetStartAddress();
    STRESS_LOG1(LF_EH, LL_INFO10000, "COMPlusThrowCallback: offset is %d\n", offs);

    EE_ILEXCEPTION_CLAUSE EHClause;
    unsigned start_adjust, end_adjust;

    start_adjust = !(pCf->HasFaulted() || pCf->IsIPadjusted());
    end_adjust = pCf->IsActiveFunc();

    for(ULONG i=0; i < EHCount; i++)
    {
        pJitManager->GetNextEHClause(&pEnumState, &EHClause);
        _ASSERTE(IsValidClause(&EHClause));

        STRESS_LOG4(LF_EH, LL_INFO100, "COMPlusThrowCallback: considering '%s' clause [%d,%d], ofs:%d\n",
            (IsFault(&EHClause) ? "fault" : (
            IsFinally(&EHClause) ? "finally" : (
            IsFilterHandler(&EHClause) ? "filter" : (
            IsTypedHandler(&EHClause) ? "typed" : "unknown")))),
            EHClause.TryStartPC,
            EHClause.TryEndPC,
            offs
            );

        // Checking the exception range is a bit tricky because
        // on CPU faults (null pointer access, div 0, ..., the IP points
        // to the faulting instruction, but on calls, the IP points
        // to the next instruction.
        // This means that we should not include the start point on calls
        // as this would be a call just preceding the try block.
        // Also, we should include the end point on calls, but not faults.

        // If we're in the FILTER part of a filter clause, then we
        // want to stop crawling.  It's going to be caught in a
        // EX_CATCH just above us.  If not, the exception
        if (   IsFilterHandler(&EHClause)
            && (   offs > EHClause.FilterOffset
                || (offs == EHClause.FilterOffset && !start_adjust) )
            && (   offs < EHClause.HandlerStartPC
                || (offs == EHClause.HandlerStartPC && !end_adjust) )) {

            STRESS_LOG4(LF_EH, LL_INFO100, "COMPlusThrowCallback: Fault inside filter [%d,%d] startAdj %d endAdj %d\n",
                        EHClause.FilterOffset, EHClause.HandlerStartPC, start_adjust, end_adjust);

            if (fGiveDebuggerAndProfilerNotification)
                EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pFunc);
            return SWA_ABORT;
        }

        if ( (offs < EHClause.TryStartPC) ||
             (offs > EHClause.TryEndPC) ||
             (offs == EHClause.TryStartPC && start_adjust) ||
             (offs == EHClause.TryEndPC && end_adjust))
            continue;

        BOOL typeMatch = FALSE;
        BOOL isTypedHandler = IsTypedHandler(&EHClause);

        if (isTypedHandler && !thrownType.IsNull())
        {
            if (EHClause.TypeHandle == (void*)(size_t)mdTypeRefNil)
            {
                // this is a catch(...)
                typeMatch = TRUE;
            }
            else
            {
                TypeHandle exnType = pJitManager->ResolveEHClause(&EHClause,pCf);

                // if doesn't have cached class then class wasn't loaded so couldn't have been thrown
                typeMatch = !exnType.IsNull() && ExceptionIsOfRightType(exnType, thrownType);
            }
        }

        // <TODO>@PERF: Is this too expensive? Consider storing the nesting level
        // instead of the HandlerEndPC.</TODO>

        // Determine the nesting level of EHClause. Just walk the table
        // again, and find out how many handlers enclose it
        DWORD nestingLevel = 0;

        if (IsFaultOrFinally(&EHClause))
            continue;
        if (isTypedHandler)
        {
            LOG((LF_EH, LL_INFO100, "COMPlusThrowCallback: %s match for typed handler.\n", typeMatch?"Found":"Did not find"));
            if (!typeMatch)
            {
                continue;
            }
        }
        else
        {
            // Must be an exception filter (__except() part of __try{}__except(){}).
            nestingLevel = ComputeEnclosingHandlerNestingLevel(pJitManager,
                                                               pCf->GetMethodToken(),
                                                               EHClause.HandlerStartPC);

            // We just need *any* address within the method. This will let the debugger
            // resolve the EnC version of the method.
            PCODE pMethodAddr = GetControlPC(regs);
            if (fGiveDebuggerAndProfilerNotification)
                EEToDebuggerExceptionInterfaceWrapper::ExceptionFilter(pFunc, pMethodAddr, EHClause.FilterOffset, pHandlerEBP);

            UINT_PTR uStartAddress = (UINT_PTR)pCf->GetCodeInfo()->GetStartAddress();

            // save clause information in the exinfo
            pExInfo->m_EHClauseInfo.SetInfo(COR_PRF_CLAUSE_FILTER,
                                            uStartAddress + EHClause.FilterOffset,
                                            StackFrame((UINT_PTR)pHandlerEBP));

            // Let the profiler know we are entering a filter
            if (fGiveDebuggerAndProfilerNotification)
                EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFilterEnter(pFunc);

            STRESS_LOG3(LF_EH, LL_INFO10, "COMPlusThrowCallback: calling filter code, EHClausePtr:%08x, Start:%08x, End:%08x\n",
                &EHClause, EHClause.HandlerStartPC, EHClause.HandlerEndPC);

            OBJECTREF throwable = PossiblyUnwrapThrowable(pThread->GetThrowable(), pCf->GetAssembly());

            pExInfo->m_EHClauseInfo.SetManagedCodeEntered(TRUE);

            int iFilt = COMPlusThrowCallbackHelper(pJitManager,
                                                   pCf,
                                                   pData,
                                                   &EHClause,
                                                   nestingLevel,
                                                   throwable,
                                                   pThread);

            pExInfo->m_EHClauseInfo.SetManagedCodeEntered(FALSE);

            // Let the profiler know we are leaving a filter
            if (fGiveDebuggerAndProfilerNotification)
                EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFilterLeave();

            pExInfo->m_EHClauseInfo.ResetInfo();

            if (pThread->IsRudeAbortInitiated())
            {
                if (fGiveDebuggerAndProfilerNotification)
                    EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pFunc);
                return SWA_CONTINUE;
            }

            // If this filter didn't want the exception, keep looking.
            if (EXCEPTION_EXECUTE_HANDLER != iFilt)
                continue;
        }

        // Record this location, to stop the unwind phase, later.
        pData->pFunc = pFunc;
        pData->dHandler = i;
        pData->pStack = pStack;

        // Notify the profiler that a catcher has been found
        if (fGiveDebuggerAndProfilerNotification)
        {
            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchCatcherFound(pFunc);
            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pFunc);
        }

#ifdef DEBUGGING_SUPPORTED
        //
        // Notify debugger that a catcher has been found.
        //
        if (fIsILStub)
        {
            EEToDebuggerExceptionInterfaceWrapper::NotifyOfCHFFilter(pExInfo->m_pExceptionPointers, pILStubFrame);
        }
        else
        if (fGiveDebuggerAndProfilerNotification &&
            CORDebuggerAttached() && !pExInfo->m_ExceptionFlags.DebuggerInterceptInfo())
        {
            _ASSERTE(pData);
            // We just need *any* address within the method. This will let the debugger
            // resolve the EnC version of the method.
            PCODE pMethodAddr = GetControlPC(regs);

            EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedExceptionCatcherFound(pThread,
                                                                                           pData->pFunc, pMethodAddr,
                                                                                           (SIZE_T)pData->pStack,
                                                                                           &EHClause);
        }
#endif // DEBUGGING_SUPPORTED

        return SWA_ABORT;
    }
    if (fGiveDebuggerAndProfilerNotification)
        EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pFunc);
    return SWA_CONTINUE;
} // StackWalkAction COMPlusThrowCallback()


//==========================================================================
// COMPlusUnwindCallback
//==========================================================================

#if defined(_MSC_VER)
#pragma warning(push)
#pragma warning (disable : 4740) // There is inline asm code in this function, which disables
                                 // global optimizations.
#pragma warning (disable : 4731)
#endif
StackWalkAction COMPlusUnwindCallback (CrawlFrame *pCf, ThrowCallbackType *pData)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    _ASSERTE(pData->bIsUnwind);

    Frame *pFrame = pCf->GetFrame();
    MethodDesc *pFunc = pCf->GetFunction();

    #if defined(_DEBUG)
    #define METHODNAME(pFunc) (pFunc?pFunc->m_pszDebugMethodName:"<n/a>")
    #else
    #define METHODNAME(pFunc) "<n/a>"
    #endif
    STRESS_LOG4(LF_EH, LL_INFO100, "COMPlusUnwindCallback: STACKCRAWL method:%pM ('%s'), Frame:%p, FrameVtable = %pV\n",
        pFunc, METHODNAME(pFunc), pFrame, pCf->IsFrameless()?0:(*(void**)pFrame));
    #undef METHODNAME

    if (pFrame && pData->pTopFrame == pFrame)
        /* Don't look past limiting frame if there is one */
        return SWA_ABORT;

    if (!pFunc)
        return SWA_CONTINUE;

    if (!pCf->IsFrameless())
        return SWA_CONTINUE;

    Thread *pThread = GetThread();

    // If the thread is being RudeAbort, we will not run any finally
    if (pThread->IsRudeAbortInitiated())
    {
        return SWA_CONTINUE;
    }

    IJitManager* pJitManager = pCf->GetJitManager();
    _ASSERTE(pJitManager);

    ExInfo *pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);

    PREGDISPLAY regs = pCf->GetRegisterSet();
    BYTE *pStack = (BYTE *) GetRegdisplaySP(regs);

    TypeHandle thrownType = TypeHandle();

#ifdef DEBUGGING_SUPPORTED
    LOG((LF_EH, LL_INFO1000, "COMPlusUnwindCallback: Intercept %d, pData->pFunc 0x%X, pFunc 0x%X, pData->pStack 0x%X, pStack 0x%X\n",
         pExInfo->m_ExceptionFlags.DebuggerInterceptInfo(),
         pData->pFunc,
         pFunc,
         pData->pStack,
         pStack));

    //
    // If the debugger wants to intercept this exception here, go do that.
    //
    if (pExInfo->m_ExceptionFlags.DebuggerInterceptInfo() && (pData->pFunc == pFunc) && (pData->pStack == pStack))
    {
        goto LDoDebuggerIntercept;
    }
#endif

    bool fGiveDebuggerAndProfilerNotification;
    fGiveDebuggerAndProfilerNotification = !pFunc->IsILStub();

    // Notify the profiler of the function we're dealing with in the unwind phase
    if (fGiveDebuggerAndProfilerNotification)
        EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionEnter(pFunc);

    EH_CLAUSE_ENUMERATOR pEnumState;
    unsigned EHCount = pJitManager->InitializeEHEnumeration(pCf->GetMethodToken(), &pEnumState);

    if (EHCount == 0)
    {
        // Inform the profiler that we're leaving, and what pass we're on
        if (fGiveDebuggerAndProfilerNotification)
            EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionLeave(pFunc);

        return SWA_CONTINUE;
    }

    // if we are being called on an unwind for an exception that we did not try to catch, eg.
    // an internal EE exception, then pThread->GetThrowable will be null
    {
        OBJECTREF  throwable = pThread->GetThrowable();
        if (throwable != NULL)
        {
            throwable = PossiblyUnwrapThrowable(throwable, pCf->GetAssembly());
            thrownType = TypeHandle(throwable->GetMethodTable());
        }
    }
#ifdef DEBUGGING_SUPPORTED
    BYTE *pHandlerEBP;
    pHandlerEBP = (BYTE *) GetRegdisplayFP(regs);
#endif

    DWORD offs;
    offs = (DWORD)pCf->GetRelOffset();  //= (BYTE*) (*regs->pPC) - (BYTE*) pCf->GetStartAddress();

    LOG((LF_EH, LL_INFO100, "COMPlusUnwindCallback: current EIP offset in method 0x%x, \n", offs));

    EE_ILEXCEPTION_CLAUSE EHClause;
    unsigned start_adjust, end_adjust;

    start_adjust = !(pCf->HasFaulted() || pCf->IsIPadjusted());
    end_adjust = pCf->IsActiveFunc();

    for(ULONG i=0; i < EHCount; i++)
    {
          pJitManager->GetNextEHClause(&pEnumState, &EHClause);
         _ASSERTE(IsValidClause(&EHClause));

        STRESS_LOG4(LF_EH, LL_INFO100, "COMPlusUnwindCallback: considering '%s' clause [%d,%d], offs:%d\n",
                (IsFault(&EHClause) ? "fault" : (
                 IsFinally(&EHClause) ? "finally" : (
                 IsFilterHandler(&EHClause) ? "filter" : (
                 IsTypedHandler(&EHClause) ? "typed" : "unknown")))),
                EHClause.TryStartPC,
                EHClause.TryEndPC,
                offs
                );

        // Checking the exception range is a bit tricky because
        // on CPU faults (null pointer access, div 0, ..., the IP points
        // to the faulting instruction, but on calls, the IP points
        // to the next instruction.
        // This means that we should not include the start point on calls
        // as this would be a call just preceding the try block.
        // Also, we should include the end point on calls, but not faults.

        if (   IsFilterHandler(&EHClause)
            && (   offs > EHClause.FilterOffset
                || (offs == EHClause.FilterOffset && !start_adjust) )
            && (   offs < EHClause.HandlerStartPC
                || (offs == EHClause.HandlerStartPC && !end_adjust) )
            ) {
            STRESS_LOG4(LF_EH, LL_INFO100, "COMPlusUnwindCallback: Fault inside filter [%d,%d] startAdj %d endAdj %d\n",
                        EHClause.FilterOffset, EHClause.HandlerStartPC, start_adjust, end_adjust);

            // Make the filter as done. See comment in CallJitEHFilter
            // on why we have to do it here.
            Frame* pFilterFrame = pThread->GetFrame();
            _ASSERTE(pFilterFrame->GetVTablePtr() == ExceptionFilterFrame::GetMethodFrameVPtr());
            ((ExceptionFilterFrame*)pFilterFrame)->SetFilterDone();

            // Inform the profiler that we're leaving, and what pass we're on
            if (fGiveDebuggerAndProfilerNotification)
                EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionLeave(pFunc);

            return SWA_ABORT;
        }

        if ( (offs <  EHClause.TryStartPC) ||
             (offs > EHClause.TryEndPC) ||
             (offs == EHClause.TryStartPC && start_adjust) ||
             (offs == EHClause.TryEndPC && end_adjust))
            continue;

        // <TODO>@PERF : Is this too expensive? Consider storing the nesting level
        // instead of the HandlerEndPC.</TODO>

        // Determine the nesting level of EHClause. Just walk the table
        // again, and find out how many handlers enclose it

        DWORD nestingLevel = ComputeEnclosingHandlerNestingLevel(pJitManager,
                                                                 pCf->GetMethodToken(),
                                                                 EHClause.HandlerStartPC);

        // We just need *any* address within the method. This will let the debugger
        // resolve the EnC version of the method.
        PCODE pMethodAddr = GetControlPC(regs);

        UINT_PTR uStartAddress = (UINT_PTR)pCf->GetCodeInfo()->GetStartAddress();

        if (IsFaultOrFinally(&EHClause))
        {
            if (fGiveDebuggerAndProfilerNotification)
                EEToDebuggerExceptionInterfaceWrapper::ExceptionHandle(pFunc, pMethodAddr, EHClause.HandlerStartPC, pHandlerEBP);

            pExInfo->m_EHClauseInfo.SetInfo(COR_PRF_CLAUSE_FINALLY,
                                            uStartAddress + EHClause.HandlerStartPC,
                                            StackFrame((UINT_PTR)pHandlerEBP));

            // Notify the profiler that we are about to execute the finally code
            if (fGiveDebuggerAndProfilerNotification)
                EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFinallyEnter(pFunc);

            LOG((LF_EH, LL_INFO100, "COMPlusUnwindCallback: finally clause [%d,%d] - call\n", EHClause.TryStartPC, EHClause.TryEndPC));

            pExInfo->m_EHClauseInfo.SetManagedCodeEntered(TRUE);

            ::CallJitEHFinally(pCf, (BYTE *)uStartAddress, &EHClause, nestingLevel);

            pExInfo->m_EHClauseInfo.SetManagedCodeEntered(FALSE);

            LOG((LF_EH, LL_INFO100, "COMPlusUnwindCallback: finally - returned\n"));

            // Notify the profiler that we are done with the finally code
            if (fGiveDebuggerAndProfilerNotification)
                EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFinallyLeave();

            pExInfo->m_EHClauseInfo.ResetInfo();

            continue;
        }

        // Current is not a finally, check if it's the catching handler (or filter).
        if (pData->pFunc != pFunc || (ULONG)(pData->dHandler) != i || pData->pStack != pStack)
        {
            continue;
        }

#ifdef _DEBUG
        gLastResumedExceptionFunc = pCf->GetFunction();
        gLastResumedExceptionHandler = i;
#endif

        // save clause information in the exinfo
        pExInfo->m_EHClauseInfo.SetInfo(COR_PRF_CLAUSE_CATCH,
                                        uStartAddress  + EHClause.HandlerStartPC,
                                        StackFrame((UINT_PTR)pHandlerEBP));

        // Notify the profiler that we are about to resume at the catcher.
        if (fGiveDebuggerAndProfilerNotification)
        {
            DACNotify::DoExceptionCatcherEnterNotification(pFunc, EHClause.HandlerStartPC);

            EEToProfilerExceptionInterfaceWrapper::ExceptionCatcherEnter(pThread, pFunc);

            EEToDebuggerExceptionInterfaceWrapper::ExceptionHandle(pFunc, pMethodAddr, EHClause.HandlerStartPC, pHandlerEBP);
        }

        STRESS_LOG4(LF_EH, LL_INFO100, "COMPlusUnwindCallback: offset 0x%x matches clause [0x%x, 0x%x) matches in method %pM\n",
                    offs, EHClause.TryStartPC, EHClause.TryEndPC, pFunc);

        // ResumeAtJitEH will set pExInfo->m_EHClauseInfo.m_fManagedCodeEntered = TRUE; at the appropriate time
        ::ResumeAtJitEH(pCf, (BYTE *)uStartAddress, &EHClause, nestingLevel, pThread, pData->bUnwindStack);
        //UNREACHABLE_MSG("ResumeAtJitEH shouldn't have returned!");

        // we do not set pExInfo->m_EHClauseInfo.m_fManagedCodeEntered = FALSE here,
        // that happens when the catch clause calls back to COMPlusEndCatch

    }

    STRESS_LOG1(LF_EH, LL_INFO100, "COMPlusUnwindCallback: no handler found in method %pM\n", pFunc);
    if (fGiveDebuggerAndProfilerNotification)
        EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionLeave(pFunc);

    return SWA_CONTINUE;


#ifdef DEBUGGING_SUPPORTED
LDoDebuggerIntercept:

    STRESS_LOG1(LF_EH|LF_CORDB, LL_INFO100, "COMPlusUnwindCallback: Intercepting in method %pM\n", pFunc);

    //
    // Setup up the easy parts of the context to restart at.
    //
    EHContext context;

    //
    // Note: EAX ECX EDX are scratch
    //
    context.Esp = (DWORD)(size_t)(GetRegdisplaySP(regs));
    context.Ebx = *regs->pEbx;
    context.Esi = *regs->pEsi;
    context.Edi = *regs->pEdi;
    context.Ebp = *regs->pEbp;

    //
    // Set scratch registers to 0 to avoid reporting incorrect values to GC in case of debugger changing the IP
    // in the middle of a scratch register lifetime (see Dev10 754922)
    //
    context.Eax = 0;
    context.Ecx = 0;
    context.Edx = 0;

    //
    // Ok, now set the target Eip to the address the debugger requested.
    //
    ULONG_PTR nativeOffset;
    pExInfo->m_DebuggerExState.GetDebuggerInterceptInfo(NULL, NULL, NULL, NULL, &nativeOffset, NULL);
    context.Eip = GetControlPC(regs) - (pCf->GetRelOffset() - nativeOffset);

    //
    // Finally we need to get the correct Esp for this nested level
    //

    context.Esp = pCf->GetCodeManager()->GetAmbientSP(regs,
                                                      pCf->GetCodeInfo(),
                                                      nativeOffset,
                                                      pData->dHandler,
                                                      pCf->GetCodeManState()
                                                     );
    //
    // In case we see unknown FS:[0] handlers we delay the interception point until we reach the handler that protects the interception point.
    // This way we have both FS:[0] handlers being poped up by RtlUnwind and managed capital F Frames being unwinded by managed stackwalker.
    //
    BOOL fCheckForUnknownHandler  = TRUE;
    if (PopNestedExceptionRecords((LPVOID)(size_t)context.Esp, fCheckForUnknownHandler))
    {
        // Let ClrDebuggerDoUnwindAndIntercept RtlUnwind continue to unwind frames until we reach the handler protected by COMPlusNestedExceptionHandler.
        pExInfo->m_InterceptionContext = context;
        pExInfo->m_ValidInterceptionContext = TRUE;
        STRESS_LOG0(LF_EH|LF_CORDB, LL_INFO100, "COMPlusUnwindCallback: Skip interception until unwinding reaches the actual handler protected by COMPlusNestedExceptionHandler\n");
    }
    else
    {
        //
        // Pop off all the Exception information up to this point in the stack
        //
        UnwindExceptionTrackerAndResumeInInterceptionFrame(pExInfo, &context);
    }
    return SWA_ABORT;
#endif // DEBUGGING_SUPPORTED
} // StackWalkAction COMPlusUnwindCallback ()
#if defined(_MSC_VER)
#pragma warning(pop)
#endif

#if defined(_MSC_VER)
#pragma warning(push)
#pragma warning (disable : 4740) // There is inline asm code in this function, which disables
                                 // global optimizations.
#pragma warning (disable : 4731)
#endif
void ResumeAtJitEH(CrawlFrame* pCf,
                   BYTE* startPC,
                   EE_ILEXCEPTION_CLAUSE *EHClausePtr,
                   DWORD nestingLevel,
                   Thread *pThread,
                   BOOL unwindStack)
{
    // No dynamic contract here because this function doesn't return and destructors wouldn't be executed
    WRAPPER_NO_CONTRACT;

    EHContext context;

    context.Setup(PCODE(startPC + EHClausePtr->HandlerStartPC), pCf->GetRegisterSet());

    size_t * pShadowSP = NULL; // Write Esp to *pShadowSP before jumping to handler
    size_t * pHandlerEnd = NULL;

    OBJECTREF throwable = PossiblyUnwrapThrowable(pThread->GetThrowable(), pCf->GetAssembly());

    pCf->GetCodeManager()->FixContext(ICodeManager::CATCH_CONTEXT,
                                      &context,
                                      pCf->GetCodeInfo(),
                                      EHClausePtr->HandlerStartPC,
                                      nestingLevel,
                                      throwable,
                                      pCf->GetCodeManState(),
                                      &pShadowSP,
                                      &pHandlerEnd);

    if (pHandlerEnd)
    {
        *pHandlerEnd = EHClausePtr->HandlerEndPC;
    }

    MethodDesc* pMethodDesc = pCf->GetCodeInfo()->GetMethodDesc();
    TADDR startAddress = pCf->GetCodeInfo()->GetStartAddress();
    if (InlinedCallFrame::FrameHasActiveCall(pThread->m_pFrame))
    {
        // When unwinding an exception in ReadyToRun, the JIT_PInvokeEnd helper which unlinks the ICF from
        // the thread will be skipped. This is because unlike jitted code, each pinvoke is wrapped by calls
        // to the JIT_PInvokeBegin and JIT_PInvokeEnd helpers, which push and pop the ICF on the thread. The
        // ICF is not linked at the method prolog and unlinked at the epilog when running R2R code. Since the
        // JIT_PInvokeEnd helper will be skipped, we need to unlink the ICF here. If the executing method
        // has another pinvoke, it will re-link the ICF again when the JIT_PInvokeBegin helper is called.

        // Check that the InlinedCallFrame is in the method with the exception handler. There can be other
        // InlinedCallFrame somewhere up the call chain that is not related to the current exception
        // handling.
        
        // See the usages for USE_PER_FRAME_PINVOKE_INIT for more information.

#ifdef DEBUG
        TADDR handlerFrameSP = pCf->GetRegisterSet()->SP;
#endif // DEBUG
        // Find the ESP of the caller of the method with the exception handler.
        bool unwindSuccess = pCf->GetCodeManager()->UnwindStackFrame(pCf->GetRegisterSet(),
                                                                     pCf->GetCodeInfo(),
                                                                     pCf->GetCodeManagerFlags(),
                                                                     pCf->GetCodeManState(),
                                                                     NULL /* StackwalkCacheUnwindInfo* */);
        _ASSERTE(unwindSuccess);

        if (((TADDR)pThread->m_pFrame < pCf->GetRegisterSet()->SP))
        {
            TADDR returnAddress = ((InlinedCallFrame*)pThread->m_pFrame)->m_pCallerReturnAddress;
#ifdef USE_PER_FRAME_PINVOKE_INIT
            // If we're setting up the frame for each P/Invoke for the given platform,
            // then we do this for all P/Invokes except ones in IL stubs.
            if (!ExecutionManager::GetCodeMethodDesc(returnAddress)->IsILStub())
#else
            // If we aren't setting up the frame for each P/Invoke (instead setting up once per method),
            // then ReadyToRun code is the only code using the per-P/Invoke logic.
            if (ExecutionManager::IsReadyToRunCode(returnAddress))
#endif
            {
                _ASSERTE((TADDR)pThread->m_pFrame >= handlerFrameSP);
                pThread->m_pFrame->Pop(pThread);
            }
        }
    }

    // save esp so that endcatch can restore it (it always restores, so want correct value)
    ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);
    pExInfo->m_dEsp = (LPVOID)context.GetSP();
    LOG((LF_EH, LL_INFO1000, "ResumeAtJitEH: current m_dEsp set to %p\n", context.GetSP()));

    PVOID dEsp = GetCurrentSP();

    if (!unwindStack)
    {
        // If we don't want to unwind the stack, then the guard page had better not be gone!
        _ASSERTE(pThread->DetermineIfGuardPagePresent());

        // so down below won't really update esp
        context.SetSP(dEsp);
        pExInfo->m_pShadowSP = pShadowSP; // so that endcatch can zero it back

        if  (pShadowSP)
        {
            *pShadowSP = (size_t)dEsp;
        }
    }
    else
    {
        // so shadow SP has the real SP as we are going to unwind the stack
        dEsp = (LPVOID)context.GetSP();

        // BEGIN: pExInfo->UnwindExInfo(dEsp);
        ExInfo *pPrevNestedInfo = pExInfo->m_pPrevNestedInfo;

        while (pPrevNestedInfo && pPrevNestedInfo->m_StackAddress < dEsp)
        {
            LOG((LF_EH, LL_INFO1000, "ResumeAtJitEH: popping nested ExInfo at 0x%p\n", pPrevNestedInfo->m_StackAddress));

            pPrevNestedInfo->DestroyExceptionHandle();
            pPrevNestedInfo->m_StackTraceInfo.FreeStackTrace();

#ifdef DEBUGGING_SUPPORTED
            if (g_pDebugInterface != NULL)
            {
                g_pDebugInterface->DeleteInterceptContext(pPrevNestedInfo->m_DebuggerExState.GetDebuggerInterceptContext());
            }
#endif // DEBUGGING_SUPPORTED

            pPrevNestedInfo = pPrevNestedInfo->m_pPrevNestedInfo;
        }

        pExInfo->m_pPrevNestedInfo = pPrevNestedInfo;

        _ASSERTE(pExInfo->m_pPrevNestedInfo == 0 || pExInfo->m_pPrevNestedInfo->m_StackAddress >= dEsp);

        // Before we unwind the SEH records, get the Frame from the top-most nested exception record.
        Frame* pNestedFrame = GetCurrFrame(FindNestedEstablisherFrame(GetCurrentSEHRecord()));

        PopNestedExceptionRecords((LPVOID)(size_t)dEsp);

        EXCEPTION_REGISTRATION_RECORD* pNewBottomMostHandler = GetCurrentSEHRecord();

        pExInfo->m_pShadowSP = pShadowSP;

        // The context and exception record are no longer any good.
        _ASSERTE(pExInfo->m_pContext < dEsp);   // It must be off the top of the stack.
        pExInfo->m_pContext = 0;                // Whack it.
        pExInfo->m_pExceptionRecord = 0;
        pExInfo->m_pExceptionPointers = 0;

        // We're going to put one nested record back on the stack before we resume.  This is
        // where it goes.
        NestedHandlerExRecord *pNestedHandlerExRecord = (NestedHandlerExRecord*)((BYTE*)dEsp - ALIGN_UP(sizeof(NestedHandlerExRecord), STACK_ALIGN_SIZE));

        // The point of no return.  The next statement starts scribbling on the stack.  It's
        // deep enough that we won't hit our own locals.  (That's important, 'cuz we're still
        // using them.)
        //
        _ASSERTE(dEsp > &pCf);
        pNestedHandlerExRecord->m_handlerInfo.m_hThrowable=NULL; // This is random memory.  Handle
                                                                 // must be initialized to null before
                                                                 // calling Init(), as Init() will try
                                                                 // to free any old handle.
        pNestedHandlerExRecord->Init((PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler, pNestedFrame);

        INSTALL_EXCEPTION_HANDLING_RECORD(&(pNestedHandlerExRecord->m_ExReg));

        context.SetSP(pNestedHandlerExRecord);

        // We might have moved the bottommost handler.  The nested record itself is never
        // the bottom most handler -- it's pushed after the fact.  So we have to make the
        // bottom-most handler the one BEFORE the nested record.
        if (pExInfo->m_pBottomMostHandler < pNewBottomMostHandler)
        {
            STRESS_LOG3(LF_EH, LL_INFO10000, "ResumeAtJitEH: setting ExInfo:0x%p m_pBottomMostHandler from 0x%p to 0x%p\n",
                pExInfo, pExInfo->m_pBottomMostHandler, pNewBottomMostHandler);
          pExInfo->m_pBottomMostHandler = pNewBottomMostHandler;
        }

        if  (pShadowSP)
        {
            *pShadowSP = context.GetSP();
        }
    }

    STRESS_LOG3(LF_EH, LL_INFO100, "ResumeAtJitEH: resuming at EIP = %p  ESP = %p EBP = %p\n",
                context.Eip, context.GetSP(), context.GetFP());

#ifdef STACK_GUARDS_DEBUG
    // We are transitioning back to managed code, so ensure that we are in
    // SO-tolerant mode before we do so.
    RestoreSOToleranceState();
#endif

    // we want this to happen as late as possible but certainly after the notification
    // that the handle for the current ExInfo has been freed has been delivered
    pExInfo->m_EHClauseInfo.SetManagedCodeEntered(TRUE);

    ETW::ExceptionLog::ExceptionCatchBegin(pMethodDesc, (PVOID)startAddress);

    ResumeAtJitEHHelper(&context);
    UNREACHABLE_MSG("Should never return from ResumeAtJitEHHelper!");

    // we do not set pExInfo->m_EHClauseInfo.m_fManagedCodeEntered = FALSE here,
    // that happens when the catch clause calls back to COMPlusEndCatch
    // we don't return to this point so it would be moot (see unreachable_msg above)

}
#if defined(_MSC_VER)
#pragma warning(pop)
#endif

// Must be in a separate function because INSTALL_COMPLUS_EXCEPTION_HANDLER has a filter
int CallJitEHFilterWorker(size_t *pShadowSP, EHContext *pContext)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    int retVal = EXCEPTION_CONTINUE_SEARCH;

    BEGIN_CALL_TO_MANAGED();

    retVal = CallJitEHFilterHelper(pShadowSP, pContext);

    END_CALL_TO_MANAGED();

    return retVal;
}

int CallJitEHFilter(CrawlFrame* pCf, BYTE* startPC, EE_ILEXCEPTION_CLAUSE *EHClausePtr, DWORD nestingLevel, OBJECTREF thrownObj)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    int retVal = EXCEPTION_CONTINUE_SEARCH;
    size_t * pShadowSP = NULL;
    EHContext context;

    context.Setup(PCODE(startPC + EHClausePtr->FilterOffset), pCf->GetRegisterSet());

    size_t * pEndFilter = NULL; // Write
    pCf->GetCodeManager()->FixContext(ICodeManager::FILTER_CONTEXT, &context, pCf->GetCodeInfo(),
                                      EHClausePtr->FilterOffset, nestingLevel, thrownObj, pCf->GetCodeManState(),
                                      &pShadowSP, &pEndFilter);

    // End of the filter is the same as start of handler
    if (pEndFilter)
    {
        *pEndFilter = EHClausePtr->HandlerStartPC;
    }

    // ExceptionFilterFrame serves two purposes:
    //
    // 1. It serves as a frame that stops the managed search for handler
    // if we fault in the filter. ThrowCallbackType.pTopFrame is going point
    // to this frame during search for exception handler inside filter.
    // The search for handler needs a frame to stop. If we had no frame here,
    // the exceptions in filters would not be swallowed correctly since we would
    // walk past the EX_TRY/EX_CATCH block in COMPlusThrowCallbackHelper.
    //
    // 2. It allows setting of SHADOW_SP_FILTER_DONE flag in UnwindFrames()
    // if we fault in the filter. We have to set this flag together with unwinding
    // of the filter frame. Using a regular C++ holder to clear this flag here would cause
    // GC holes. The stack would be in inconsistent state when we trigger gc just before
    // returning from UnwindFrames.

    FrameWithCookie<ExceptionFilterFrame> exceptionFilterFrame(pShadowSP);

    ETW::ExceptionLog::ExceptionFilterBegin(pCf->GetCodeInfo()->GetMethodDesc(), (PVOID)pCf->GetCodeInfo()->GetStartAddress());

    retVal = CallJitEHFilterWorker(pShadowSP, &context);

    ETW::ExceptionLog::ExceptionFilterEnd();

    exceptionFilterFrame.Pop();

    return retVal;
}

void CallJitEHFinally(CrawlFrame* pCf, BYTE* startPC, EE_ILEXCEPTION_CLAUSE *EHClausePtr, DWORD nestingLevel)
{
    WRAPPER_NO_CONTRACT;

    EHContext context;
    context.Setup(PCODE(startPC + EHClausePtr->HandlerStartPC), pCf->GetRegisterSet());

    size_t * pShadowSP = NULL; // Write Esp to *pShadowSP before jumping to handler

    size_t * pFinallyEnd = NULL;
    pCf->GetCodeManager()->FixContext(
        ICodeManager::FINALLY_CONTEXT, &context, pCf->GetCodeInfo(),
        EHClausePtr->HandlerStartPC, nestingLevel, ObjectToOBJECTREF((Object *) NULL), pCf->GetCodeManState(),
        &pShadowSP, &pFinallyEnd);

    if (pFinallyEnd)
    {
        *pFinallyEnd = EHClausePtr->HandlerEndPC;
    }

    ETW::ExceptionLog::ExceptionFinallyBegin(pCf->GetCodeInfo()->GetMethodDesc(), (PVOID)pCf->GetCodeInfo()->GetStartAddress());

    CallJitEHFinallyHelper(pShadowSP, &context);

    ETW::ExceptionLog::ExceptionFinallyEnd();

    //
    // Update the registers using new context
    //
    // This is necessary to reflect GC pointer changes during the middle of a unwind inside a
    // finally clause, because:
    // 1. GC won't see the part of stack inside try (which has thrown an exception) that is already
    // unwinded and thus GC won't update GC pointers for this portion of the stack, but rather the
    // call stack in finally.
    // 2. upon return of finally, the unwind process continues and unwinds stack based on the part
    // of stack inside try and won't see the updated values in finally.
    // As a result, we need to manually update the context using register values upon return of finally
    //
    // Note that we only update the registers for finally clause because
    // 1. For filter handlers, stack walker is able to see the whole stack (including the try part)
    // with the help of ExceptionFilterFrame as filter handlers are called in first pass
    // 2. For catch handlers, the current unwinding is already finished
    //
    context.UpdateFrame(pCf->GetRegisterSet());

    // This does not need to be guarded by a holder because the frame is dead if an exception gets thrown.  Filters are different
    //  since they are run in the first pass, so we must update the shadowSP reset in CallJitEHFilter.
    if (pShadowSP) {
        *pShadowSP = 0;  // reset the shadowSP to 0
    }
}
#if defined(_MSC_VER)
#pragma warning (default : 4731)
#endif

//=====================================================================
// *********************************************************************
BOOL ComPlusFrameSEH(EXCEPTION_REGISTRATION_RECORD* pEHR)
{
    LIMITED_METHOD_CONTRACT;

    return ((LPVOID)pEHR->Handler == (LPVOID)COMPlusFrameHandler || (LPVOID)pEHR->Handler == (LPVOID)COMPlusNestedExceptionHandler);
}


//
//-------------------------------------------------------------------------
// This is installed when we call COMPlusFrameHandler to provide a bound to
// determine when are within a nested exception
//-------------------------------------------------------------------------
EXCEPTION_HANDLER_IMPL(COMPlusNestedExceptionHandler)
{
    WRAPPER_NO_CONTRACT;

    if (pExceptionRecord->ExceptionFlags & (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND))
    {
        LOG((LF_EH, LL_INFO100, "    COMPlusNestedHandler(unwind) with %x at %x\n", pExceptionRecord->ExceptionCode,
            pContext ? GetIP(pContext) : 0));


        // We're unwinding past a nested exception record, which means that we've thrown
        // a new exception out of a region in which we're handling a previous one.  The
        // previous exception is overridden -- and needs to be unwound.

        // The preceding is ALMOST true.  There is one more case, where we use setjmp/longjmp
        // from within a nested handler.  We won't have a nested exception in that case -- just
        // the unwind.

        Thread* pThread = GetThread();
        ExInfo* pExInfo = &(pThread->GetExceptionState()->m_currentExInfo);
        ExInfo* pPrevNestedInfo = pExInfo->m_pPrevNestedInfo;

        if (pPrevNestedInfo == &((NestedHandlerExRecord*)pEstablisherFrame)->m_handlerInfo)
        {
            _ASSERTE(pPrevNestedInfo);

            LOG((LF_EH, LL_INFO100, "COMPlusNestedExceptionHandler: PopExInfo(): popping nested ExInfo at 0x%p\n", pPrevNestedInfo));

            pPrevNestedInfo->DestroyExceptionHandle();
            pPrevNestedInfo->m_StackTraceInfo.FreeStackTrace();

#ifdef DEBUGGING_SUPPORTED
            if (g_pDebugInterface != NULL)
            {
                g_pDebugInterface->DeleteInterceptContext(pPrevNestedInfo->m_DebuggerExState.GetDebuggerInterceptContext());
            }
#endif // DEBUGGING_SUPPORTED

            pExInfo->m_pPrevNestedInfo = pPrevNestedInfo->m_pPrevNestedInfo;

        } else {
            // The whacky setjmp/longjmp case.  Nothing to do.
        }

    } else {
        LOG((LF_EH, LL_INFO100, "    InCOMPlusNestedHandler with %x at %x\n", pExceptionRecord->ExceptionCode,
            pContext ? GetIP(pContext) : 0));
    }


    // There is a nasty "gotcha" in the way exception unwinding, finally's, and nested exceptions
    // interact.  Here's the scenario ... it involves two exceptions, one normal one, and one
    // raised in a finally.
    //
    // The first exception occurs, and is caught by some handler way up the stack.  That handler
    // calls RtlUnwind -- and handlers that didn't catch this first exception are called again, with
    // the UNWIND flag set.  If, one of the handlers throws an exception during
    // unwind (like, a throw from a finally) -- then that same handler is not called during
    // the unwind pass of the second exception.  [ASIDE: It is called on first-pass.]
    //
    // What that means is -- the COMPlusExceptionHandler, can't count on unwinding itself correctly
    // if an exception is thrown from a finally.  Instead, it relies on the NestedExceptionHandler
    // that it pushes for this.
    //

    EXCEPTION_DISPOSITION retval = EXCEPTION_HANDLER_FWD(COMPlusFrameHandler);
    LOG((LF_EH, LL_INFO100, "Leaving COMPlusNestedExceptionHandler with %d\n", retval));
    return retval;
}

EXCEPTION_REGISTRATION_RECORD *FindNestedEstablisherFrame(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame)
{
    LIMITED_METHOD_CONTRACT;

    while (pEstablisherFrame->Handler != (PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler) {
        pEstablisherFrame = pEstablisherFrame->Next;
        _ASSERTE(pEstablisherFrame != EXCEPTION_CHAIN_END);   // should always find one
    }
    return pEstablisherFrame;
}

EXCEPTION_HANDLER_IMPL(FastNExportExceptHandler)
{
    WRAPPER_NO_CONTRACT;

    // Most of our logic is in commin with COMPlusFrameHandler.
    EXCEPTION_DISPOSITION retval = EXCEPTION_HANDLER_FWD(COMPlusFrameHandler);

#ifdef _DEBUG
    // If the exception is escaping the last CLR personality routine on the stack,
    // then state a flag on the thread to indicate so.
    if (retval == ExceptionContinueSearch)
    {
        SetReversePInvokeEscapingUnhandledExceptionStatus(IS_UNWINDING(pExceptionRecord->ExceptionFlags), pEstablisherFrame);
    }
#endif // _DEBUG

    return retval;
}

#ifdef FEATURE_COMINTEROP
// The reverse COM interop path needs to be sure to pop the ComMethodFrame that is pushed, but we do not want
// to have an additional FS:0 handler between the COM callsite and the call into managed.  So we push this
// FS:0 handler, which will defer to the usual COMPlusFrameHandler and then perform the cleanup of the
// ComMethodFrame, if needed.
EXCEPTION_HANDLER_IMPL(COMPlusFrameHandlerRevCom)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    // Defer to COMPlusFrameHandler
    EXCEPTION_DISPOSITION result = EXCEPTION_HANDLER_FWD(COMPlusFrameHandler);

    if (pExceptionRecord->ExceptionFlags & (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND))
    {
        // Do cleanup as needed
        ComMethodFrame::DoSecondPassHandlerCleanup(GetCurrFrame(pEstablisherFrame));
    }

    return result;
}
#endif // FEATURE_COMINTEROP
#endif // !DACCESS_COMPILE
#endif // !FEATURE_EH_FUNCLETS

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(CONTEXT * pContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    UINT_PTR stackSlot = pContext->Ebp + REDIRECTSTUB_EBP_OFFSET_CONTEXT;
    PTR_PTR_CONTEXT ppContext = dac_cast<PTR_PTR_CONTEXT>((TADDR)stackSlot);
    return *ppContext;
}

#ifndef DACCESS_COMPILE
LONG CLRNoCatchHandler(EXCEPTION_POINTERS* pExceptionInfo, PVOID pv)
{
#ifndef FEATURE_EH_FUNCLETS
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_ENTRY_POINT;

    LONG result = EXCEPTION_CONTINUE_SEARCH;

    // This function can be called during the handling of a SO
    //BEGIN_ENTRYPOINT_VOIDRET;

    result = CLRVectoredExceptionHandler(pExceptionInfo);

    if (EXCEPTION_EXECUTE_HANDLER == result)
    {
        result = EXCEPTION_CONTINUE_SEARCH;
    }

    //END_ENTRYPOINT_VOIDRET;

    return result;
#else  // !FEATURE_EH_FUNCLETS
    return EXCEPTION_CONTINUE_SEARCH;
#endif // !FEATURE_EH_FUNCLETS
}

// Returns TRUE if caller should resume execution.
BOOL
AdjustContextForVirtualStub(
        EXCEPTION_RECORD *pExceptionRecord,
        CONTEXT *pContext)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = GetThreadNULLOk();

    // We may not have a managed thread object. Example is an AV on the helper thread.
    // (perhaps during StubManager::IsStub)
    if (pThread == NULL)
    {
        return FALSE;
    }

    PCODE f_IP = GetIP(pContext);

    VirtualCallStubManager::StubKind sk;
    VirtualCallStubManager *pMgr = VirtualCallStubManager::FindStubManager(f_IP, &sk);

    if (sk == VirtualCallStubManager::SK_DISPATCH)
    {
        if (*PTR_WORD(f_IP) != X86_INSTR_CMP_IND_ECX_IMM32)
        {
            _ASSERTE(!"AV in DispatchStub at unknown instruction");
            return FALSE;
        }
    }
    else
    if (sk == VirtualCallStubManager::SK_RESOLVE)
    {
        if (*PTR_WORD(f_IP) != X86_INSTR_MOV_EAX_ECX_IND)
        {
            _ASSERTE(!"AV in ResolveStub at unknown instruction");
            return FALSE;
        }

        SetSP(pContext, dac_cast<PCODE>(dac_cast<PTR_BYTE>(GetSP(pContext)) + sizeof(void*))); // rollback push eax
    }
    else
    {
        return FALSE;
    }

    PCODE callsite = *dac_cast<PTR_PCODE>(GetSP(pContext));
    if (pExceptionRecord != NULL)
    {
        pExceptionRecord->ExceptionAddress = (PVOID)callsite;
    }

    SetIP(pContext, callsite);

#if defined(GCCOVER_TOLERATE_SPURIOUS_AV)
    // Modify LastAVAddress saved in thread to distinguish between fake & real AV
    // See comments in IsGcMarker in file excep.cpp for more details
    pThread->SetLastAVAddress((LPVOID)GetIP(pContext));
#endif // defined(GCCOVER_TOLERATE_SPURIOUS_AV)

    // put ESP back to what it was before the call.
    TADDR sp = GetSP(pContext) + sizeof(void*);

#ifndef UNIX_X86_ABI
    // set the ESP to what it would be after the call (remove pushed arguments)

    size_t stackArgumentsSize;
    if (sk == VirtualCallStubManager::SK_DISPATCH)
    {
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

        DispatchHolder *holder = DispatchHolder::FromDispatchEntry(f_IP);
        MethodTable *pMT = (MethodTable*)holder->stub()->expectedMT();
        DispatchToken token(VirtualCallStubManager::GetTokenFromStubQuick(pMgr, f_IP, sk));
        MethodDesc* pMD = VirtualCallStubManager::GetRepresentativeMethodDescFromToken(token, pMT);
        stackArgumentsSize = pMD->SizeOfArgStack();
    }
    else
    {
        // Compute the stub entry address from the address of failure (location of dereferencing of "this" pointer)
        ResolveHolder *holder = ResolveHolder::FromResolveEntry(f_IP - ResolveStub::offsetOfThisDeref());
        stackArgumentsSize = holder->stub()->stackArgumentsSize();
    }

    sp += stackArgumentsSize;
#endif // UNIX_X86_ABI

    SetSP(pContext, dac_cast<PCODE>(dac_cast<PTR_BYTE>(sp)));

    return TRUE;
}

#endif // !DACCESS_COMPILE
