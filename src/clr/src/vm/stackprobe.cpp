// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
//-----------------------------------------------------------------------------
// StackProbe.cpp
//-----------------------------------------------------------------------------


#include "common.h"
#include "stackprobe.h"


#ifdef FEATURE_STACK_PROBE


// SOTolerantBoundaryFilter is called when an exception in SO-tolerant code arrives
// at the boundary back into SO-intolerant code.
//
// If we are running in an environment where we must be hardened to SO, then we must
// catch the exception if there is not enough space to run our backout code (the stuff in the
// EX_CATCH clauses).  We also cannot let a hard SO propogate into SO-intolerant code, because
// we rip the process if that happens (we have no way to tell that the SO is ok.)
int SOTolerantBoundaryFilter(EXCEPTION_POINTERS *pExceptionInfo, DWORD * pdwSOTolerantFlags)
{
    Thread *pThread = GetThread();
    _ASSERTE(pThread);
    _ASSERTE(pdwSOTolerantFlags != NULL);
    _ASSERTE(!((*pdwSOTolerantFlags) & BSTC_TRIGGERING_UNWIND_FOR_SO));

    SaveCurrentExceptionInfo(pExceptionInfo->ExceptionRecord, pExceptionInfo->ContextRecord);

    NTSTATUS exceptionCode = pExceptionInfo->ExceptionRecord->ExceptionCode;

    // We must always handle a hard SO
    if (IsSOExceptionCode(exceptionCode))
    {
        if (exceptionCode == EXCEPTION_SOFTSO)
        {
            *pdwSOTolerantFlags |= BSTC_IS_SOFT_SO;
        }
        *pdwSOTolerantFlags |= BSTC_IS_SO;

        if (!CLRHosted() || pThread == NULL || GetEEPolicy()->GetActionOnFailure(FAIL_StackOverflow) != eRudeUnloadAppDomain)
        {
            // For security reason, it is not safe to continue execution if stack overflow happens
            // unless a host tells us to do something different.
            EEPolicy::HandleFatalStackOverflow(pExceptionInfo);
        }

        /* If there is a SO_INTOLERANT region above this */
        /* we should have processed it already in SOIntolerantTransitionHandler */
        EEPolicy::HandleStackOverflow(SOD_SOTolerantTransitor, FRAME_TOP);

        *pdwSOTolerantFlags |= BSTC_TRIGGERING_UNWIND_FOR_SO;

        return EXCEPTION_EXECUTE_HANDLER;
    }

    // Make sure we have enough stack to run our backout code.  If not,
    // catch the exception.
    if (! pThread->IsStackSpaceAvailable(ADJUST_PROBE(BACKOUT_CODE_STACK_LIMIT)))
    {
        *pdwSOTolerantFlags |= BSTC_TRIGGERING_UNWIND_FOR_SO;
        return EXCEPTION_EXECUTE_HANDLER;
    }


    return EXCEPTION_CONTINUE_SEARCH;
}

void SOTolerantCode_RecoverStack(DWORD dwFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    Thread * pThread = GetThread();
    if (!(dwFlags & BSTC_IS_SOFT_SO))
    {
        pThread->RestoreGuardPage();
    }
    if (dwFlags & BSTC_IS_SO)
    {
        if (!pThread->PreemptiveGCDisabled())
        {
            pThread->DisablePreemptiveGC();
        }
    }
    COMPlusThrowSO();
}

void SOTolerantCode_ExceptBody(DWORD * pdwFlags, Frame * pSafeForSOFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // do nothing here.  Get our stack back post-catch and then throw a new exception
    *pdwFlags |= BSTC_RECOVER_STACK;
    if (*pdwFlags & BSTC_IS_SO)
    {
        // If this assertion fires, then it means that we have not unwound the frame chain
        Thread * pThread = GetThread();
        _ASSERTE(pSafeForSOFrame == pThread->GetFrame());
        pThread->ClearExceptionStateAfterSO(pSafeForSOFrame);
    }
}

//
// ReportStackOverflow is called when our probe infrastructure detects that there
// is insufficient stack to perform the operation.
//

void ReportStackOverflow()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsStackProbingEnabled());

    Thread *pThread = GetThread();

    if (pThread != NULL)
    {
        // We don't want an SO to happen while we are trying to throw this one.  So check if there
        // is enough space left to handle an exception (this translates to check that we have stack
        // space left equivalent to the soft guard region).  If not, then remove the guard page by
        // forcing a hard SO.  This effectively turns the SO into a boundary SO.

        // We should only ever get in this situation on a probe from managed code.  From within the EE,
        // we will never let our probe point get this close.  Either way, we'd rip the process if a hard
        // SO occurred.

        UINT_PTR stackGuarantee = pThread->GetStackGuarantee();

        // We expect the stackGuarantee to be a multiple of the page size for
        // the call to IsStackSpaceAvailable.
        _ASSERTE(stackGuarantee%GetOsPageSize() == 0);
        if (pThread->IsStackSpaceAvailable(static_cast<float>(stackGuarantee)/GetOsPageSize()))
        {
            COMPlusThrowSO();
        }

        // If there isn't much stack left to attempt to report a soft stack overflow, let's trigger a hard
        // SO, so we clear the guard page and give us at least another page of stack to work with.

        if (!pThread->IsStackSpaceAvailable(ADJUST_PROBE(1)))
        {
            DontCallDirectlyForceStackOverflow();            
        }
    }

    RaiseException(EXCEPTION_SOFTSO, 0, 0, NULL);
}

void CheckForSOInSOIntolerantCode()
{
    Thread *pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        return;
    }
    // We use the location of frames to decide SO mode.  But during exception,
    // we may not unwind some frames, for example: TPMethodFrame, therefore
    // it is not safe to apply this check.
    //_ASSERTE(!pThread->IsSOTolerant(FRAME_TOP));
    if (! pThread->IsSPBeyondLimit())
    {
        return;
    }
    EEPolicy::HandleStackOverflow(SOD_SOIntolerantTransitor, FRAME_TOP);
    _ASSERTE (!"Can not reach here");
}

//---------------------------------------------------------------------------------------
//
// SetSOIntolerantTransitionMarker: Use the current frame as our marker for intolerant transition.
//
// Arguments:
//    None.
//
// Return Value:
//    None.
// 
// Note:
//    SO mode is determined by what is on stack.  If we see our intolerant transtion first, we are in SO.
//    Because compiler lays object in a function at random stack location, the address of our intolerant
//    transition object SOIntolerantTransitionHandler may be before the HelperMethodFrame.  Therefore, we
//    can not use the address of the handlers.  Instead we use the current top frame.
//
void SetSOIntolerantTransitionMarker()
{
    LIMITED_METHOD_CONTRACT;

    Thread *pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        return;
    }
    Frame *pFrame = pThread->GetFrame();

    //
    // Check to see if the Frame chain is corrupt
    // This can happen when unmanaged code calls back to managed code
    //
    if (pFrame != FRAME_TOP)
    {
        // SafeGetGCCookiePtr examines the value of the vtable pointer 
        // and makes sure that it is a legal Frame subtype.
        // It returns NULL when we have an illegal (i.e. corrupt) vtable value.
        //
        if (!Frame::HasValidVTablePtr(pFrame))
            DoJITFailFast();
    }

    // We use pFrame - 1 as our marker so that IntolerantTransitionHandler is seen before
    // a transition frame.
    ClrFlsSetValue(TlsIdx_SOIntolerantTransitionHandler, (void*)(((size_t)pFrame)-1));

    _ASSERTE(!pThread->IsSOTolerant(FRAME_TOP));
}

BOOL RetailStackProbeNoThrowNoThread(unsigned int n)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_ANY;

    BEGIN_GETTHREAD_ALLOWED;
    Thread *pThread = GetThread();

    if (!pThread)
    {
        // we only probe on managed threads
        return TRUE;
    }
    return RetailStackProbeNoThrow(n, pThread);
    END_GETTHREAD_ALLOWED;
}

// This functions are used by the stack probe infrastucture that is outside the VM
// tree.  It needs to call into the VM code in order to probe properly.
void InitStackProbesRetail()
{
    LIMITED_METHOD_CONTRACT;
    g_fpCheckForSOInSOIntolerantCode = CheckForSOInSOIntolerantCode;
    g_fpSetSOIntolerantTransitionMarker = SetSOIntolerantTransitionMarker;
    g_fpDoProbe = RetailStackProbeNoThrowNoThread;
    g_fpHandleSoftStackOverflow = EEPolicy::HandleSoftStackOverflow;

    g_StackProbingEnabled = g_pConfig->ProbeForStackOverflow() != 0;
}

// Shared by both the nothrow and throwing version. FORCEINLINE into both to avoid the call overhead.
FORCEINLINE BOOL RetailStackProbeHelper(unsigned int n, Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    UINT_PTR probeLimit;

    // @TODO - Need to devise a probe that doesn't require the thread object
    if (pThread == NULL)
    {
        UINT_PTR stackLimit = (UINT_PTR)Thread::GetStackLowerBound();
        probeLimit = Thread::GetLastNormalStackAddress(stackLimit);
    }
    else
    {
        probeLimit = pThread->GetProbeLimit();
    }
    UINT_PTR probeAddress = (UINT_PTR)(&pThread) - (n * GetOsPageSize());

    // If the address we want to probe to is beyond the precalculated limit we fail
    // Note that we don't check for stack probing being disabled.  This is encoded in
    // the value returned from GetProbeLimit, which will be 0 if probing is disabled.
    if (probeAddress < probeLimit)
    {
#if 0 
        // @todo : remove this when iexplore, W3WP.EXE and friends allocate 512K instead
        // of 256K for their stack.
        if (((char *)(pThread->GetCachedStackBase()) - (char *)(pThread->GetCachedStackLimit())) < 0x41000)
        {
            return true;
        }
#endif
        return FALSE;
    }
    
    return TRUE;
}

BOOL RetailStackProbeNoThrowWorker(unsigned int n, Thread *pThread)
{
    WRAPPER_NO_CONTRACT;
    return RetailStackProbeHelper(n, pThread);
}

void RetailStackProbeWorker(unsigned int n, Thread *pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

    if (RetailStackProbeHelper(n, pThread))
    {
        return;
    }
    ReportStackOverflow();
}

void DefaultRetailStackProbeWorker(Thread *pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

    if (RetailStackProbeHelper(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT), pThread))
    {
        return;
    }
    ReportStackOverflow();
}

#endif // FEATURE_STACK_PROBE

#ifdef STACK_GUARDS_DEBUG

DWORD g_InteriorProbeAmount = DEFAULT_INTERIOR_PROBE_AMOUNT;

DWORD g_CurrentStackGuardTlsIdx = (DWORD) -1;
DWORD g_UniqueId = 0;

// If this has a non-zero value, we'll mark only those pages whose probe line number matches.  This allows us
// to turn protection on only for a specific probe so that can find multiple w/o having to rebuild.  Otherwise
// can never get past that first AV in the debugger.
unsigned int g_ProtectStackPagesInDebuggerForProbeAtLine = 0;

// These two are used to the amount probed for at a particular line number
unsigned int g_UpdateProbeAtLine = 0;
SString* g_pUpdateProbeAtLineInFile = NULL;
unsigned int g_UpdateProbeAtLineAmount = 0;

// If this is TRUE, we'll break in the debugger if we try to probe during the handling of a
// probe-induced stack overflow.
BOOL  g_BreakOnProbeDuringSO = FALSE;

// If this is TRUE, probe cookie validation via assertion is enabled
// disable assertions on debug build.  The stack consumption is different enough
// that we'll always be getting spurious failures.
BOOL  g_probeAssertOnOverrun = FALSE;

// SO logging pollutes the EH logging space and vice-versa.  The SOLogger class
// allows us to turn SO logging on separately and only produce SO logging, or
// to allow both.
#undef LOG
#define LOG(x) s_SOLogger.LogSpew x

class SOLogger {

    enum SOLogStyle {
        SO_LOGGING_NONE,            // No SO logging
        SO_LOGGING_SEPARATE_LOG,    // Log SO to separate file
        SO_LOGGING_STANDARD_LOG     // Log SO to standard log
    };

    SOLogStyle m_SOLogStyle;
    FILE *m_SOLoggerFile;

public:
    SOLogger();
    ~SOLogger();

    void Initialize();

    void LogSpew(DWORD facility, DWORD level, const char *fmt, ... );
};

static SOLogger s_SOLogger;

SOLogger::SOLogger()
    : m_SOLogStyle(SO_LOGGING_NONE), m_SOLoggerFile(NULL)
{
}

void SOLogger::Initialize()
{
    WRAPPER_NO_CONTRACT;

    DWORD SOLogger = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SOLogger, SO_LOGGING_NONE);
    if (SOLogger == SO_LOGGING_SEPARATE_LOG)
    {
        m_SOLogStyle = SO_LOGGING_SEPARATE_LOG;
        int ec = fopen_s(&m_SOLoggerFile, "SOLogSpewFile.log", "w");
        _ASSERTE(SUCCEEDED(ec));
    }
    else if (SOLogger == SO_LOGGING_STANDARD_LOG)
    {
        m_SOLogStyle = SO_LOGGING_STANDARD_LOG;
    }
    else if (SOLogger == SO_LOGGING_NONE)
    {
        m_SOLogStyle = SO_LOGGING_NONE;
    }
    else
    {
        _ASSERTE(!"Invalid SOLogger value");
    }
}

SOLogger::~SOLogger()
{
    LIMITED_METHOD_CONTRACT;
    if (m_SOLoggerFile != NULL)
    {
        fclose(m_SOLoggerFile);
    }
}

void SOLogger::LogSpew(DWORD facility, DWORD level, const char *fmt, ... )
{
    STATIC_CONTRACT_WRAPPER;

    if (m_SOLogStyle == SO_LOGGING_NONE)
    {
        return;
    }

    va_list     args;
    va_start(args, fmt);
    if (m_SOLogStyle == SO_LOGGING_SEPARATE_LOG)
    {
        vfprintf(m_SOLoggerFile, fmt, args);
    }
    else if (LoggingEnabled())
    {
        LogSpewValist (facility, level, fmt, args);
    }
    va_end(args);
}

#define MORE_INFO_STRING             \
    "\nPlease open a bug against the feature owner.\n"   \
    "\nFor details about this feature, see, in a CLR enlistment, src\\ndp\\clr\\doc\\OtherDevDocs\\untriaged\\clrdev_web\\SO Guide for CLR Developers.doc\n"


// The following are used to support the SO-injection framework
HMODULE BaseStackGuard::m_hProbeCallBack = 0;
BaseStackGuard::ProbeCallbackType BaseStackGuard::m_pfnProbeCallback = NULL;

//
// ShouldValidateSOToleranceOnThisThread determines if we should check for SO_Tolerance on this
// thread.
//
// If it is a thread we care about, then we will assert if it calls an SO-intolerant function
// outside of a probe
//
BOOL ShouldValidateSOToleranceOnThisThread()
{
    LIMITED_METHOD_CONTRACT;

    if (g_StackProbingEnabled == false || g_fEEShutDown == TRUE)
    {
        return FALSE;
    }

    BEGIN_GETTHREAD_ALLOWED;
    Thread *pThread = GetThread();
    if (pThread == NULL || ShouldProbeOnThisThread() == FALSE)
    {
        return FALSE;
    }

    // We only want to probe on managed threads that have IL on the stack behind them.  But
    // there's not an easy way to check for that, so we use whether or not we own the thread and
    // whether or not a stack guard is in place.
    //
    // If we don't own the thread, then just make sure that we didn't get here by leaving the EE and coming
    // back in.  (In which case we would have installed a probe and the GetCurrentStackGuard is non-NULL).
    // We are only probing on managed threads, but we want to avoid asserting for cases where an unmanaged
    // app starts the EE (thereby creating a managed thread), and runs completely unmanaged, but uses some of the CLR's
    // infrastructure, such as Crsts.
    if (pThread->DoWeOwn() == FALSE && pThread->GetCurrentStackGuard() == NULL)
    {
        return FALSE;
    }

    if (! IsHandleNullUnchecked(pThread->GetThrowableAsHandle()))
    {
        return FALSE;
    }

    return TRUE;
    END_GETTHREAD_ALLOWED;
}


BOOL BaseStackGuard_RequiresNStackPages(BaseStackGuardGeneric *pGuard, unsigned int n, BOOL fThrowOnSO)
{
    return ((BaseStackGuard*)pGuard)->RequiresNStackPages(n, fThrowOnSO);
}

void BaseStackGuard_CheckStack(BaseStackGuardGeneric *pGuard)
{
    WRAPPER_NO_CONTRACT;
    ((BaseStackGuard*)pGuard)->CheckStack();
}

BOOL CheckNStackPagesAvailable(unsigned int n)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_ANY;

    BEGIN_GETTHREAD_ALLOWED;
    Thread *pThread = GetThread();

    // If we don't have a managed thread object, we assume that we have the requested
    // number of pages available.
    if (!pThread)
        return TRUE;

    _ASSERTE(FitsIn<float>(n));
    return pThread->IsStackSpaceAvailable(static_cast<float>(n));
    END_GETTHREAD_ALLOWED;
}

void InitStackProbes()
{
    WRAPPER_NO_CONTRACT;

    g_CurrentStackGuardTlsIdx = TlsIdx_StackProbe;

    s_SOLogger.Initialize();

    // If we're in a debugger, and if the config word below is set, then we'll go ahead and protect stack pages
    // when we're run under a debugger.
    //if (IsDebuggerPresent())
    //{
        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SOEnableStackProtectionInDebugger) == 1)
        {
            g_ProtectStackPagesInDebugger = TRUE;
        }
        g_ProtectStackPagesInDebuggerForProbeAtLine =
            CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SOEnableStackProtectionInDebuggerForProbeAtLine);

        g_UpdateProbeAtLine = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SOUpdateProbeAtLine);
        g_UpdateProbeAtLineAmount = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SOUpdateProbeAtLineAmount);
        LPWSTR wszUpdateProbeAtLineInFile = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SOUpdateProbeAtLineInFile);
        g_pUpdateProbeAtLineInFile = new SString(wszUpdateProbeAtLineInFile);
        g_pUpdateProbeAtLineInFile->Normalize();

        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SOBreakOnProbeDuringSO) == 1)
        {
            g_BreakOnProbeDuringSO = TRUE;
        }
    //}

    // Never let g_EntryPointProbeAmount get set to an invalid value of <= 0 to avoid races in places that might be
    // about to probe as we set it.
    BOOL entryPointProbeAmount =  REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SOEntryPointProbe, g_EntryPointProbeAmount);
    if (entryPointProbeAmount > 0)
    {
        g_EntryPointProbeAmount = entryPointProbeAmount;
    }

    BOOL interiorProbeAmount =  REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SOInteriorProbe, g_InteriorProbeAmount);
    if (interiorProbeAmount > 0)
    {
        g_InteriorProbeAmount = interiorProbeAmount;
    }

    BOOL enableBackoutStackValidation = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SOEnableBackoutStackValidation, FALSE);

    g_EnableDefaultRWValidation = 1;

    BOOL enableDefaultRWValidation =  REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SOEnableDefaultRWValidation, g_EnableDefaultRWValidation);



    // put this first because it will cause probe validation via contract otherwise
    g_probeAssertOnOverrun = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SOProbeAssertOnOverrun, g_probeAssertOnOverrun);

    BaseStackGuard::InitProbeReportingToFaultInjectionFramework();

    g_EnableBackoutStackValidation = enableBackoutStackValidation;

    g_EnableDefaultRWValidation =  enableDefaultRWValidation;

    g_fpShouldValidateSOToleranceOnThisThread = ShouldValidateSOToleranceOnThisThread;

    g_fpRestoreCurrentStackGuard = BaseStackGuard::RestoreCurrentGuard;
    g_fpHandleStackOverflowAfterCatch = EEPolicy::HandleStackOverflowAfterCatch;


    g_fp_BaseStackGuard_RequiresNStackPages = BaseStackGuard_RequiresNStackPages;
    g_fp_BaseStackGuard_CheckStack = BaseStackGuard_CheckStack;

    g_fpCheckNStackPagesAvailable = CheckNStackPagesAvailable;

    InitStackProbesRetail();

}

void CloseSOTolerantViolationFile();

//
// This function is called when the EE is shutting down and we want to stop
// doing stack probing.  Don't clear the g_CurrentStackGuardTlsIdx field though,
// because there may still be other threads in the process of probing and
// they'll AV if we pull the g_CurrentStackGuardTlsIdx out from under them.
void TerminateStackProbes()
{
    WRAPPER_NO_CONTRACT;


    CloseSOTolerantViolationFile();

    // Don't actually shut down the SO infrastructure. We've got multiple threads
    // racing around in the runtime, and they can be left in an inconsisent state
    // if we flip this off.

    return;
#if 0
    // Yank the stack guard on this thread
    StackGuardDisabler __guardDisable;
    __guardDisable.NeverRestoreGuard();
    
    // Clear out the current guard in case we terminate and its cleanup code
    // does not get to run.
    BaseStackGuard::SetCurrentGuard(NULL);
   
    g_StackProbingEnabled = false;
    g_EnableBackoutStackValidation = FALSE;
    g_fpShouldValidateSOToleranceOnThisThread = NULL;
#endif
}

//-----------------------------------------------------------------------------
// Error handling when we go past a stack guard.
// We have different messages to more aggressively diagnose the problem
//-----------------------------------------------------------------------------

// Called by Check_Stack when we overwrite the cookie
void BaseStackGuard::HandleOverwrittenThisStackGuard(__in_z char *stackID)
{
    LIMITED_METHOD_CONTRACT;

    if (! g_probeAssertOnOverrun)
    {
        return;
    }

    ClrDebugState *pState = GetClrDebugState();
    _ASSERTE(pState);
    if (pState->IsSONotMainline())
    {
        return;
    }

    // This prevents infinite loops in this function if we call something that probes.
    // Must do it after the check for pState->IsSONotMainline() to give the first invocation
    // a chance to run.
    SO_NOT_MAINLINE_FUNCTION;

    // This fires at a closing Check_Stack.
    // The cookie set by Requires_?K_stack was overwritten. We detected that at
    // the closing call to check_stack.

    // To fix, increase the guard size at the specified ip.
    //
    // A debugging trick: If you can set a breakpoint at the opening Requires_?K_Stack
    // macro for this instance, you can step in and see where the cookie is actually
    // placed. Then, place a breakpoint that triggers when (DWORD*) 0xYYYYYYYY changes.
    // Continue execution. The breakpoint will fire exactly when the cookie is over-written.
    char buff[1024];
    buff[0] = '\0';

    sprintf_s(buff, COUNTOF(buff),
              "STACK GUARD VIOLATION\n"
              "The%s stack guard installed in %s at \"%s\" @ %d requested %d pages of stack.\n"
              "\nIf this is easily reproduced, please rerun the test under the debugger with the\n"
              "DWORD environment variable COMPlus_SOEnableStackProtectionInDebugger\n"
              "set to 1.  This will cause an AV at the point of overrun.\n"
              "Attach the stack trace at that point to the bug in addition to this assert."
              MORE_INFO_STRING, stackID ? stackID : "",
              m_szFunction, m_szFile, m_lineNum, m_numPages);

    LOG((LF_EH, LL_INFO100000, "%s", buff));

    DbgAssertDialog((char *)m_szFile, m_lineNum, buff);

}

void BaseStackGuard::HandleOverwrittenPreviousStackGuard(int probeShortFall, __in_z char *stackID)
{
    LIMITED_METHOD_CONTRACT;

    if (! g_probeAssertOnOverrun)
    {
        return;
    }

    ClrDebugState *pState = GetClrDebugState();
    _ASSERTE(pState);
    if (pState->IsSONotMainline())
    {
        return;
    }

    // This prevents infinite loops in this function if we call something that probes.
    // Must do it after the check for pState->IsSONotMainline() to give the first invocation
    // a chance to run.
    SO_NOT_MAINLINE_FUNCTION;

    // This fires at an opening Requires_?K_Stack
    // We detected that we were already passed our parent's stack guard. So this guard is
    // ok, but our parent's guard is too small. Note that if this test was removed,
    // the failure would be detected by our parent's closing Check_Stack. But if we detect it
    // here, we have more information.
    //
    // We can see how many bytes short our parent is and adjust it properly.
    char buff[2048];
    buff[0] = '\0';

    // We don't come in here unless we have a previous guard.
    _ASSERTE(m_pPrevGuard != NULL);

    sprintf_s(buff, COUNTOF(buff),
              "STACK GUARD VIOLATION\n"
              "    The%s stack guard being installed in %s at \"%s\" @ %d is already in violation of the previous stack guard.\n"
              "    The previous guard was installed in %s at \"%s\" @ %d and requested %d pages of stack.\n"
              "The stack requested by the previous guard is at least %d pages (%d bytes) short.\n"
              MORE_INFO_STRING, stackID ? stackID : "", m_szFunction, m_szFile, m_lineNum,
              m_pPrevGuard->m_szFunction, m_pPrevGuard->m_szFile, m_pPrevGuard->m_lineNum, m_pPrevGuard->m_numPages,
              probeShortFall/GetOsPageSize() + (probeShortFall%GetOsPageSize() ? 1 : 0), probeShortFall);

    LOG((LF_EH, LL_INFO100000, "%s", buff));

    DbgAssertDialog((char *)m_szFile, m_lineNum, buff);
}

void BaseStackGuard::HandleOverwrittenCurrentStackGuard(void *pGuard, int shortFall, __in_z char *stackID)
{
   ( (BaseStackGuard *)pGuard)->HandleOverwrittenCurrentStackGuard(shortFall, stackID);
}

void BaseStackGuard::HandleOverwrittenCurrentStackGuard(int probeShortFall, __in_z char *stackID)
{
    DEBUG_ONLY_FUNCTION;    

    if (! g_probeAssertOnOverrun)
    {
        return;
    }

    // This fires during probe invariant validation.
    // We detected that our current stack was already past the current probe depth. Note that if this
    // test were removed, the failure should be detected the current guard's closing Check_Stack.
    // But if we detect it here, we have more information.
    //
    // We can see how many bytes short the guard is and adjust it properly.
    char buff[2048];
    buff[0] = '\0';

    sprintf_s(buff, COUNTOF(buff),
              "STACK GUARD VIOLATION\n\n"
              "The%s stack guard installed in %s at \"%s\" @ %d has been violated\n\n"
              "The guard requested %d pages of stack and is at least %d pages (%d bytes) short.\n"
              MORE_INFO_STRING, stackID ? stackID : "", m_szFunction, m_szFile, m_lineNum, m_numPages,
              probeShortFall/GetOsPageSize() + (probeShortFall%GetOsPageSize() ? 1 : 0), probeShortFall);

    LOG((LF_EH, LL_INFO100000, buff));

    DbgAssertDialog((char *)m_szFile, m_lineNum, buff);
}

//-----------------------------------------------------------------------------
// Function to do the actual touching of memory during probing, so we can have
// a good approximation of the address we should be overflowing at.
//-----------------------------------------------------------------------------
static __declspec(noinline) void PlaceMarker(UINT_PTR *pMarker)
{
    LIMITED_METHOD_CONTRACT;
    *pMarker = STACK_COOKIE_VALUE;
}


StackGuardDisabler::StackGuardDisabler()
{
    LIMITED_METHOD_CONTRACT;
    BaseStackGuard *pGuard = BaseStackGuard::GetCurrentGuard();

    if (pGuard == NULL || !BaseStackGuard::IsProbeGuard(pGuard) || !pGuard->Enabled())
    {
        // If there's no guard or its a boundary guard, there's nothing to do
        m_fDisabledGuard = FALSE;
        return;
    }

    // If the guard is currently enabled, then we'll need to change the page protection
    pGuard->UndoPageProtectionInDebugger();
    pGuard->DisableGuard();
    m_fDisabledGuard = TRUE;
}// StackGuardDisabler

void StackGuardDisabler::NeverRestoreGuard()
{
    m_fDisabledGuard = FALSE;
}

StackGuardDisabler::~StackGuardDisabler()
{
    WRAPPER_NO_CONTRACT;
    if (m_fDisabledGuard)
    {
        BaseStackGuard::RestoreCurrentGuard(TRUE);
    }
}// ~StackProbeDisabler

//-----------------------------------------------------------------------------
// BaseStackGuard::RestoreCurrentGuard
//
// Function to restore the current marker's cookie after an EH.
//
// During an exception, we cannot restore stack guard cookies as we unwind our stack guards
// because the stack has not been unwound and we might corrupt it.  So we just pop off our
// guards as we go and deal with restoring the cookie after the exception.
// There are two cases:
//
// 1) the exception is caught outside the EE
// 2) the exception is caught in the EE
//
// Case 1: If we catch the exception outside the EE, then the boundary guard that we installed before
// leaving the EE will still be intact, so we have no work to do.
//
// Case 2: If we caught the exception in the EE, then on EX_END_CATCH, after we have unwound the stack, we need to
// restore the cookie for the topmost stack guard.  That is what RestoreCurrentGuard does.
//
//-----------------------------------------------------------------------------
void BaseStackGuard::RestoreCurrentGuard(BOOL fWasDisabled)
{
    if (!IsStackProbingEnabled())
    {
        // nothing to do
        return;
    }

    LPVOID pSP = (LPVOID)GetCurrentSP();
    BaseStackGuard *pGuard = GetCurrentGuard();

    if (pGuard == NULL || !IsProbeGuard(pGuard))
    {
        // If there's no guard or its a boundary guard, there's nothing to do
        // Just set state to SO-tolerant and quit.
        GetClrDebugState()->SetSOTolerance();
        return;
    }

    if (reinterpret_cast<LPVOID>(pGuard->m_pMarker) > pSP)
    {
        // We have caught an exception while processing an exception.  So can't restore the marker and must
        // wait until the catcher of the original exception handles it.
        if (!IsBackoutCalledForEH((BYTE *)(pGuard), static_cast<BYTE *>(pSP)))
        {
            // verfiy that really are processing an exception.  We could have some false positives here, but in
            // general this is a good check.
            _ASSERTE(!"After an exception was caught, we couldn't restore the marker because it is greater than the SP\n"
                      "This should only happen if we caught a nested exception when already processing an exception, but"
                      " the distance between the SP and the probe does not indicate an exception is in flight.");
        }
        return;
    }

    // Reset the SO-tolerance state

    // We should never get here with a guard beyond the current SP
    _ASSERTE(reinterpret_cast<LPVOID>(pGuard) > pSP);

    LOG((LF_EH, LL_INFO100000, "BSG::RSG: G: %p D: %d \n", pGuard, pGuard->m_depth));

    // If we have EX_TRY {EX_TRY {...}EX_CATCH{...}EX_END_CATCH}EX_CATCH{...}EX_END_CATCH,
    // the inner EX_END_CATCH will mark the current guard protected.  When we reach the
    // outer EX_END_CATCH, we will AV when placing marker.
    pGuard->UndoPageProtectionInDebugger();
    if (fWasDisabled)
        pGuard->EnableGuard();
    // Replace the marker for the current guard
    PlaceMarker(pGuard->m_pMarker);

    // Protect marker page in debugger if we need it
    pGuard->ProtectMarkerPageInDebugger();
    GetClrDebugState()->ResetSOTolerance();
    pGuard->m_fEHInProgress = FALSE;
}

//-----------------------------------------------------------------------------
// This places a marker outside the bounds of a probe.  We don't want to use
// PlaceMarker because that is how we detect if a proper SO was triggered (via
// StackProbeContainsIP
//-----------------------------------------------------------------------------
static __declspec(noinline) void PlaceMarkerBeyondProbe(UINT_PTR *pMarker)
{
    *pMarker = STACK_COOKIE_VALUE;
}

//---------------------------------------------------------------------------------------------
// Determine if we should check integrity of previous cookie.  Only check if the previous was a probe guard.
//---------------------------------------------------------------------------------------------
inline BOOL BaseStackGuard::ShouldCheckPreviousCookieIntegrity()
{
    WRAPPER_NO_CONTRACT;
    if (m_pPrevGuard == NULL || 
        IsBoundaryGuard(m_pPrevGuard) || 
        m_pPrevGuard->m_pMarker==NULL || 
        m_pPrevGuard->m_fEHInProgress || 
        !m_pPrevGuard->Enabled())
    {
        return FALSE;
    }
    return TRUE;
}

//---------------------------------------------------------------------------------------------
// Determine if we should check integrity of this cookie.
//---------------------------------------------------------------------------------------------
inline BOOL BaseStackGuard::ShouldCheckThisCookieIntegrity()
{
    WRAPPER_NO_CONTRACT;
    // We only need to check if this is a probe guard and it has a non-null marker.
    // Anything else, we don't care about.
    return IsProbeGuard(this) && m_pMarker != NULL && Enabled();
}

BOOL BaseStackGuard::RequiresNStackPages(unsigned int n, BOOL fThrowsOnSO)
{
    WRAPPER_NO_CONTRACT;

    return RequiresNStackPagesInternal(n, fThrowsOnSO);
}

BOOL BaseStackGuard::RequiresNStackPagesThrowing(unsigned int n)
{
//    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_GC_NOTRIGGER;

    return RequiresNStackPagesInternal(n, TRUE);
}

BOOL BaseStackGuard::RequiresNStackPagesNoThrow(unsigned int n)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_GC_NOTRIGGER;

    return RequiresNStackPagesInternal(n, FALSE);
}

//-----------------------------------------------------------------------------
// Place guard in stack.
//-----------------------------------------------------------------------------
BOOL BaseStackGuard::RequiresNStackPagesInternal(unsigned int n, BOOL fThrowOnSO)
{
    CONTRACTL
    {
        DISABLED(THROWS);
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BOOL fRet;

    // Temporarily initialize the exception occurred flag
    m_exceptionOccurred = FALSE;

    // Code below checks if there's a Thread, and exits immediately if not.
    // So the rest of the function rightly assumes there is a Thread
    BEGIN_GETTHREAD_ALLOWED;

    // only probe on managed threads.  No thread, no probe.
    if (! IsStackProbingEnabled() || GetThread() == NULL)
    {
        return TRUE;
    }
    
    // Don't try to probe if we are checking backout and there are active backout markers on 
    // the stack to avoid collision
    if (g_EnableBackoutStackValidation) 
    {
        if ((!(GetClrDebugState()->GetStackMarkerStack().IsEmpty())) 
            && (!(GetClrDebugState()->GetStackMarkerStack().IsDisabled())))
        {
            return TRUE;
        }
    }        

    if (n <= 1)
    {
        // Our calculation below doesn't handle 1-page probes.
        _ASSERTE(!"RequiresNStackPages called with a probe amount less than 2");
    }

    // Retrieve the current stack pointer which will be used to calculate the marker.
    LPVOID pStack = (LPVOID)GetCurrentSP();

    // Setup some helpful debugging information. Get our caller's ip. This is useful for debugging (so we can see
    // when the previous guard was set).
    m_UniqueId = g_UniqueId++;
    m_numPages = n;

    // Get the address of the last few bytes on the penultimate page we probed for.  This is slightly early than the probe point,
    // but gives us more conservatism in our overrun checking.  ("Last" here means the bytes with the smallest address.)
    m_pMarker = ((UINT_PTR*)pStack) - (GetOsPageSize() / sizeof(UINT_PTR) * (n-1));
    m_pMarker = (UINT_PTR*)((UINT_PTR)m_pMarker & ~(GetOsPageSize() - 1));

    // Grab the previous guard, if any, and update our depth.
    m_pPrevGuard = GetCurrentGuard();

    if (m_pPrevGuard == NULL)
    {
        m_depth = 0;
    }
    else
    {
        // If we've already got a probe in place that exceeds the reach of this one, then
        // don't install this one.  This avoids problems where we've installed an entry point
        // probe and then called into a function that happens to do an interior probe.  If we
        // install the interior probe, then we effectively lose our deep entry point probe
        // and end up with probe overrun violations.  Check for it being a probe guard
        // because boundary guards will always have 0 markers and we'd never probe
        // after a boundary guard otherwise.
        if (IsProbeGuard(m_pPrevGuard) && m_pPrevGuard->m_pMarker < m_pMarker)
        {
            return TRUE;
        }
        m_depth = m_pPrevGuard->m_depth + 1;

        // We need to undo the page protection that we setup when we put the previous guard in place so we don't
        // trip over it with this guard.  Also, track that we came next.
        if (IsProbeGuard(m_pPrevGuard) && m_pPrevGuard->m_pMarker != NULL)
        {
            m_pPrevGuard->UndoPageProtectionInDebugger();
            m_pPrevGuard->m_szNextFunction = m_szFunction;
            m_pPrevGuard->m_szNextFile = m_szFile;
            m_pPrevGuard->m_nextLineNum = m_lineNum;
        }
    }

    if (ShouldCheckPreviousCookieIntegrity())
    {
        UINT_PTR *approxSP = (UINT_PTR*)GetCurrentSP();
        if (approxSP <= m_pPrevGuard->m_pMarker)
        {
            UINT_PTR uProbeShortFall = (char*)m_pPrevGuard->m_pMarker - (char*)approxSP;
            _ASSERTE(FitsIn<int>(uProbeShortFall));
            HandleOverwrittenPreviousStackGuard(static_cast<int>(uProbeShortFall), NULL);
        }
    }

    m_eInitialized = cPartialInit;

    fRet = DoProbe(m_numPages, fThrowOnSO);
    END_GETTHREAD_ALLOWED;
    return fRet;
}

BOOL BaseStackGuard::DoProbe(unsigned int n, BOOL fThrowOnSO)
{
    CONTRACTL
    {
        DISABLED(THROWS);
        MODE_ANY;
        WRAPPER(GC_TRIGGERS);
        SO_TOLERANT;
    }
    CONTRACTL_END;

    if (! IsStackProbingEnabled() || m_eInitialized != cPartialInit)
    {
        return TRUE;
    }

    LOG((LF_EH, LL_INFO100000, "BSG::DP: %d pages, depth %d, probe 0x%p, fcn %s, prev 0x%p\n",
         m_numPages, m_depth, this, this->m_szFunction, m_pPrevGuard));

    // For cases where have a separate call to DoProbe, make sure the probe amounts match.
    _ASSERTE(n == m_numPages);

    // We'll probe for 12 pages + 4 for cleanup.... we'll just put our marker at the 12 page point.
    unsigned int nPagesToProbe = n + static_cast<unsigned int>(ADJUST_PROBE(BACKOUT_CODE_STACK_LIMIT));

    Thread *pThread = GetThread();

    // We already checked in RequiresNPagesStack that we've got a thread.  But ASSERT just to
    // be sure.
    _ASSERTE(pThread);

    // Check if we have enough space left in the stack
    if (fThrowOnSO)
    {
        RetailStackProbe(nPagesToProbe, pThread);
    }
    else if (! RetailStackProbeNoThrow(nPagesToProbe, pThread))
    {
        return FALSE;
    }

    // The fault injection framework will tell us when it want to inject
    // an SO.  If it returns TRUE, then inject an SO depending on the fThrowOnSO flag
    if (ReportProbeToFaultInjectionFramework() == TRUE)
    {
        if (fThrowOnSO)
        {
            COMPlusThrowSO();
        }
        // return probe failure (ie SO) if not in a throwing probe
        return FALSE;
    }

    LOG((LF_EH, LL_INFO100000, "BSG::DP: pushing to 0x%p\n", m_pMarker));

    // See if we're able to get a TLS slot to mark our guard page
    HRESULT hr = PrepGuard();

    // Since we can be here only with a valid managed thread object,
    // it will already have its TLS setup. Thus, accessing TLS in PrepGuard
    // call above shouldn't fail.
    _ASSERTE(SUCCEEDED(hr));
    
    // make sure the guard page is beyond the marker page, otherwise we could AV or when the guard
    // page moves up, it could wipe out our debugger page protection
    UINT_PTR *sp = (UINT_PTR*)GetCurrentSP();
    while (sp >= m_pMarker)
    {
        sp -= (GetOsPageSize() / sizeof(UINT_PTR));
        *sp = NULL;
    }

    // Write the cookie onto the stack.
    PlaceMarker(m_pMarker);

    // We'll protect the page where we put the marker if a debugger is attached. That way, you get an AV right away
    // when you go past the guard when running under a debugger.
    ProtectMarkerPageInDebugger();

    // Mark that we're initialized (and didn't get interupted from an exception)
    m_eInitialized = cInit;

    // Initialize the exception occurred flag
    m_exceptionOccurred = TRUE;

    // setup flag to tell if we're unwinding due to an exception
    m_fEHInProgress = FALSE;

    // By this point, everything is working, so go ahead and hook up.
    SetCurrentGuard(this);

    return TRUE;
}


//-----------------------------------------------------------------------------
// PopGuardForEH
//
// If we are being popped during an EH unwind, our cookie is likely corrupt so we can't check it.
// So just pop ourselves off the stack and return.  We will restore the markers
// after we've caught the exception.
//
// We also set the EHInProgress bit on the previous guard to indicate that the
// current guard was unwound during EH and couldn't restore the previous guard's
// cookie.
//
// Also need to clear the protection bit as go down because it will no
// longer be protected.
//-----------------------------------------------------------------------------
void BaseStackGuard::PopGuardForEH()
{
    LIMITED_METHOD_CONTRACT;
    // If we've protected this page, undo the protection
    UndoPageProtectionInDebugger();

    if (m_pPrevGuard)
    {
        m_pPrevGuard->m_fEHInProgress = TRUE;

        // Indicate that we haven't reprotected the previous guard
        m_pPrevGuard->m_fProtectedStackPage = FALSE;
    }
    // Mark it as unwound for EH.  This is for debugging purposes only so we
    // know how it was popped.
    m_eInitialized = cEHUnwound;
    SetCurrentGuard(m_pPrevGuard);
}

//-----------------------------------------------------------------------------
// Check guard in stack
// This must be called 1:1 with RequiresNPagesStack, else:
// - the function's stack cookie isn't restored
// - the stack chain in TLS gets out of wack.
//-----------------------------------------------------------------------------
void BaseStackGuard::CheckStack()
{
    WRAPPER_NO_CONTRACT;

    if (! IsStackProbingEnabled() || m_eInitialized != cInit)
    {
        return;
    }

    // If we are being popped during an EH unwind, our cookie is likely corrupt so we can't check it.
    // So just pop ourselves off the stack and return.  We will restore the markers
    // after we've caught the exception.
    if (DidExceptionOccur())
    {
        // We may not be the topmost in the stack, but we'd better not be called when we've already
        // unwound the stack past this guy.
        _ASSERTE(GetCurrentGuard() <= this);

        // Make sure that if we didn't get to the END_SO_INTOLERANT_CODE that the stack usage
        // indicates an exception.  This is only a rough check - we might miss some cases where the
        // stack grew a lot between construction and descrution of the guard.  However, it will
        // catch most short-circuits.
        if (!IsBackoutCalledForEH((BYTE *)(this), static_cast<BYTE *>((LPVOID)GetCurrentSP())))
        {
            _ASSERTE(!"Short-circuit of END_SO_INTOLERANT_CODE detected.  You cannot short-cirtuit return from an SO-intolerant region");
        }

        LOG((LF_EH, LL_INFO100000, "BSG::CS on EH path sp 0x %p popping probe 0x%p depth %d \n", GetCurrentSP(), this, m_depth));
        PopGuardForEH();
        return;
    }

    LOG((LF_EH, LL_INFO100000, "BSG::CS checking probe 0x%p depth %d \n", this, m_depth));

    // if we aren't being unwound during EH, then we shouldn't have our EHInProgress bit set.  That
    // means we caught the exception in the EE and didn't call RestoreGuard or we missed a SO-tolerant
    // transition out of the EE and the exception occurred above us.
    _ASSERTE(m_fEHInProgress == FALSE);

    // we should only ever be popping ourselves if we are not on the EH unwind path
    _ASSERTE(GetCurrentGuard() == this);

    // Can have 0-sized probes for cases where have an entry that is small enough not to need a probe.  But still
    // need to put something in place for the boundary probe assertions to work properly.  So just remove it and
    // don't do any cookie checking.
    if (m_numPages == 0)
    {
        // Just unhook our guard from the chain.  We're done. 0-page probes don't have anything preceding them.
        ResetCurrentGuard(m_pPrevGuard);
        return;
    }

    // We need to undo the page protection that we setup when we put the guard in place.
    UndoPageProtectionInDebugger();

    CheckMarkerIntegrity();

    RestorePreviousGuard();
}

void BaseStackGuard::CheckMarkerIntegrity()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pMarker == 0)
    {
        return;
    }

    // Make sure our cookie is still on the stack where it belongs.
    if (ShouldCheckThisCookieIntegrity() && IsMarkerOverrun(m_pMarker))
    {
        HandleOverwrittenThisStackGuard(NULL);
    }
}


void BaseStackGuard::RestorePreviousGuard()
{
    WRAPPER_NO_CONTRACT;

    if (! IsProbeGuard(m_pPrevGuard) || !m_pPrevGuard->Enabled())
    {
        LOG((LF_EH, LL_INFO100000, "BSG::RPG depth %d, probe 0x%p, prev 0x%p not probe\n",
             m_depth, this, m_pPrevGuard));
        // Unhook our guard from the chain.
        ResetCurrentGuard(m_pPrevGuard);
        return;
    }

    if (m_pPrevGuard->m_fEHInProgress)
    {
        // If the marker was lost during exception processing, we cannot restore it and it will be restored on the catch.
        // This can happen if we were partway through an EH unwind and then called something that probed.  We'll have unwound our
        // probe guards but won't have been able to put the cookie back, and we're still in that same position.
        LOG((LF_EH, LL_INFO100000, "BSG::RPG depth %d, probe 0x%p, EH in progress, not resetting prev 0x%p\n",
             m_depth, this, m_pPrevGuard));
        // Unhook our guard from the chain.
        ResetCurrentGuard(m_pPrevGuard);
        return;
    }

    if (m_pPrevGuard->m_pMarker == NULL)
    {
        // Previous guard had no marker.
        // We're done, so just unhook ourselves from the chain and leave.
        ResetCurrentGuard(m_pPrevGuard);
    }

        // Restore last cookie, so that our previous guard will be able to properly check whether it gets overwritten. Note:
        // we don't restore the previous cookie if we overwrote it with this guard. Doing so, by definition, corrupts the
        // stack. Its better to have the previous guard report the over-write.
    PlaceMarker(m_pPrevGuard->m_pMarker);
    LOG((LF_EH, LL_INFO100000, "BSG::RPG depth %d, probe 0x%p "
                           "for prev 0x%p at 0x%p in %s\n",
                 m_depth, this, m_pPrevGuard, m_pPrevGuard->m_pMarker, m_pPrevGuard->m_szFunction));
    // And, of course, restore the previous guard's page protection (if it had done any.)
    if (m_pPrevGuard->m_fProtectedStackPage)
    {
        m_pPrevGuard->ProtectMarkerPageInDebugger();
    }

    // Mark it as unwound on normal path.  This is for debugging purposes only so we
    // know how it was popped.
    m_eInitialized = cUnwound;

    // Unhook our guard from the chain.
    ResetCurrentGuard(m_pPrevGuard);
}

void BaseStackGuard::ProtectMarkerPageInDebugger(void *pGuard)
{
    ((BaseStackGuard *)pGuard)->ProtectMarkerPageInDebugger();
}

//-----------------------------------------------------------------------------
// Protect the page where we put the marker if a debugger is attached. That way, you get an AV right away
// when you go past the guard when running under a debugger.
//-----------------------------------------------------------------------------
void BaseStackGuard::ProtectMarkerPageInDebugger()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    DEBUG_ONLY_FUNCTION;

    if (! (g_ProtectStackPagesInDebugger || g_ProtectStackPagesInDebuggerForProbeAtLine))
    {
        return;
    }

    DWORD flOldProtect;

    LOG((LF_EH, LL_INFO100000, "BSG::PMP: m_pMarker 0x%p, value 0x%p\n", m_pMarker, *m_pMarker));

    // We cannot call into host for VirtualProtect. EEVirtualProtect will try to restore previous
    // guard, but the location has been marked with PAGE_NOACCESS.
#undef VirtualProtect
    BOOL fSuccess = ::VirtualProtect(m_pMarker, 1, PAGE_NOACCESS, &flOldProtect);
    _ASSERTE(fSuccess);

#define VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) \
        Dont_Use_VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)

    m_fProtectedStackPage = fSuccess;
}


void BaseStackGuard::UndoPageProtectionInDebugger(void *pGuard)
{
    ((BaseStackGuard *)pGuard)->UndoPageProtectionInDebugger();
}

//-----------------------------------------------------------------------------
// Remove page protection installed for this probe
//-----------------------------------------------------------------------------
void BaseStackGuard::UndoPageProtectionInDebugger()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
 
    DEBUG_ONLY_FUNCTION;

    if (!m_fProtectedStackPage)
    {
        return;
    }

    _ASSERTE(IsProbeGuard());

    DWORD flOldProtect;
    // EEVirtualProtect installs a BoundaryStackGuard.  To avoid recursion, we call
    // into OS for VirtualProtect instead.
#undef VirtualProtect
    BOOL fSuccess = ::VirtualProtect(m_pMarker, 1, PAGE_READWRITE, &flOldProtect);
    _ASSERTE(fSuccess);

    LOG((LF_EH, LL_INFO100000, "BSG::UMP m_pMarker 0x%p\n", m_pMarker));
    // Frankly, if we had protected the stack page, then we shouldn't have gone past the guard, right? :)
    _ASSERTE(!Enabled() || !IsMarkerOverrun(m_pMarker));

#define VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) \
        Dont_Use_VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)
}

void BaseStackGuard::InitProbeReportingToFaultInjectionFramework()
{
    WRAPPER_NO_CONTRACT;

    if (! g_pConfig->ShouldInjectFault(INJECTFAULT_SO))
    {
        return;
    }

    m_hProbeCallBack = CLRLoadLibrary(MAKEDLLNAME_W(W("FaultHostingLayer")));
    if (!m_hProbeCallBack) {
        fprintf(stderr, "StackProbing:  Failed to load " MAKEDLLNAME_A("FaultHostingLayer") ".  LastErr=%d\n",
           GetLastError());
        return;
    }

    m_pfnProbeCallback = (ProbeCallbackType)GetProcAddress(m_hProbeCallBack, "StackProbeCallback");
    if (!m_pfnProbeCallback) {
        fprintf(stderr, "StackProbing:  Couldn't find StackProbeCallback() in FaultHostingLayer\n");
            return;
    }
}

// The fault injection framework will return TRUE if we should
// inject an SO at the point of the current probe.
BOOL BaseStackGuard::ReportProbeToFaultInjectionFramework()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_ANY;

    if (! g_pConfig->ShouldInjectFault(INJECTFAULT_SO) || ! m_pfnProbeCallback)
    {
        return FALSE;
    }

    // FORBIDGC_LOADER_USE_ENABLED says we are only doing a minimal amount of work and will not
    // update global state (just read it.)  Code running in this state cannot tolerate a fault injection.
    if (FORBIDGC_LOADER_USE_ENABLED())
    {
        return FALSE;
    }

    // For codepaths that are not mainline or are debug only, we don't care about fault injection because
    // taking an SO here won't matter (or can't happen).  However, we'd like to still probe on those paths
    // just to give us more conservative probe coverage, so we still do the probe, just not the fault injection.
    ClrDebugState *pDebugState = GetClrDebugState();
    if (pDebugState && pDebugState->IsSONotMainline() || pDebugState->IsDebugOnly())
    {
        return FALSE;
    }

    
    // Faults injected into the default domain are process fatal.  Probing is still going to occur
    // but we never trigger fault injection.
    {
        //Attempting to figure out if we are in the default domain will trigger SO probes so
        //  we temporarily mark ourselves SONotMainline during the check to prevent recursive probes
        SO_NOT_MAINLINE_REGION();
        Thread *pThread = GetThreadNULLOk();
        if (pThread && pThread->GetDomain(TRUE)->IsDefaultDomain())
        {
            return FALSE;
        }
    }

    return m_pfnProbeCallback(m_lineNum, m_szFile);
}

void BaseStackGuard::SetCurrentGuard(BaseStackGuard* pGuard)
{
    WRAPPER_NO_CONTRACT;
    
    ClrFlsSetValue(g_CurrentStackGuardTlsIdx, pGuard);

    Thread * pThread = GetThreadNULLOk();
    if (pThread)
    {
        // For faster access, store the guard in the thread object, if available
        pThread->SetCurrentStackGuard(pGuard);
    }
}

// Reset the current guard state back to this one's
void BaseStackGuard::ResetCurrentGuard(BaseStackGuard* pGuard)
{
    WRAPPER_NO_CONTRACT;

    SetCurrentGuard(pGuard);
}

// This puts a boundary probe in the list when we leave the EE
DEBUG_NOINLINE void BoundaryStackGuard::Push()
{
    SCAN_SCOPE_BEGIN;
    ANNOTATION_FN_SO_TOLERANT;

    if (! IsStackProbingEnabled())
    {
        return;
    }


    m_isBoundaryGuard = TRUE;
    m_pPrevGuard = GetCurrentGuard();

    if (m_pPrevGuard)
    {
        // @todo  can remove the check for IsProbeGuard when have all the probes in place
        if (IsProbeGuard(m_pPrevGuard))
        {
            // ensure that the previous probe was sufficiently large
            if (ShouldCheckPreviousCookieIntegrity())
            {
                // Grab an approximation of our current stack pointer.
                void *approxStackPointer = (LPVOID)GetCurrentSP();

                if (((UINT_PTR*) approxStackPointer <= m_pPrevGuard->Marker()))
                {
                    UINT_PTR uProbeShortFall = (char*)m_pPrevGuard->Marker() - (char*)this;
                    _ASSERTE(FitsIn<int>(uProbeShortFall));
                    HandleOverwrittenPreviousStackGuard(static_cast<int>(uProbeShortFall), NULL);
                }
            }
            m_pPrevGuard->UndoPageProtectionInDebugger();  // undo previuos guard's page protection
            m_pPrevGuard->m_szNextFunction = m_szFunction;  // track that we came next
            m_pPrevGuard->m_szNextFile = m_szFile;
            m_pPrevGuard->m_nextLineNum= m_lineNum;
        }
        m_depth = m_pPrevGuard->Depth();    // don't increment, but record so can transfer to next probe
    }
    LOG((LF_EH, LL_INFO100000, "BNSG::PS probe 0x%p, depth %d, prev 0x%p in %s\n",
        this,  m_depth, m_pPrevGuard, m_pPrevGuard ? m_pPrevGuard->FunctionName() : NULL));

    // See if we're able to get a TLS slot to mark our guard page. If not, this will just be an unitialized 
    // guard. This generally happens in callbacks to the host before the EE infrastructure is set up on
    // the thread, so there won't be interesting probes to protect anyway.
    if (FAILED(PrepGuard()))
    {
        return;
    }            
        
    // Mark that we're initialized (and didn't get interupted from an exception)
    m_eInitialized = cInit;

    // setup flag to tell if we're unwinding due to an exception
    m_exceptionOccurred = TRUE;

    SetCurrentGuard(this);
}



// Pop the boundary probe and reset the original probe's cookie when
// return into the EE
DEBUG_NOINLINE void BoundaryStackGuard::Pop()
{
    SCAN_SCOPE_END;

    if (! IsStackProbingEnabled() || m_eInitialized != cInit)
    {
        return;
    }

    // If we are being popped during an EH unwind, we cannot restore the probe cookie because it will
    // corrupt the stack.  So just pop ourselves off the stack and return.  We will restore the markers
    // after we've caught the exception.
    if (DidExceptionOccur())
    {
        // We may not be the topmost in the stack, but we'd better not be called when we've already
        // unwound the stack past this guy.
        _ASSERTE(GetCurrentGuard() <= this);

        // Make sure that if we didn't get to the END_SO_TOLERANT_CODE that the stack usage
        // indicates an exception.  This is only a rough check - we might miss some cases where the
        // stack grew a lot between construction and descrution of the guard.  However, it will
        // catch most short-circuits.
        if (!IsBackoutCalledForEH((BYTE *)(this), static_cast<BYTE *>((LPVOID)GetCurrentSP())))
        {
            _ASSERTE(!"Short-circuit of END_SO_TOLERANT_CODE detected.  You cannot short-cirtuit return from an SO-tolerant region");
        }

        LOG((LF_EH, LL_INFO100000, "BNSG::PP popping on EH path 0x%p depth %d \n", this, m_depth));
        PopGuardForEH();
        return;
    }

    LOG((LF_EH, LL_INFO100000, "BNSG::PP 0x%p depth %d restoring CK at 0x%p "
                                " probe 0x%p in %s\n",
         this, m_depth, (!IsProbeGuard(m_pPrevGuard) ? 0 : m_pPrevGuard->Marker()),
         m_pPrevGuard, m_pPrevGuard ? m_pPrevGuard->FunctionName() : NULL));

    // we should only ever be popping ourselves
    _ASSERTE(GetCurrentGuard() == this);

    RestorePreviousGuard();
}


//
// IsBackoutCalledForEH
//
// Uses heuristics to determines whether the backout code is being called on an EH path or
// not based on the original SP and the SP when the backout code is called.
//
// origSP:      The SP when the mainline code was called.  For example, the SP of a ctor or code in a try block
//
// backoutSP:   The SP when the backout code is called.
//
// Returns: boolean indicating whether or not the backout code is being called on an EH path.
//
BOOL IsBackoutCalledForEH(BYTE *origSP,
                          BYTE *backoutSP)
{
    // We need to determine if we are being called in the normal or exception path.  (Sure would be
    // nice if the CRT would tell us.)   We use the stack pointer to determine this.  On the normal path
    // the stack pointer should be not far from the this pointer, whereas on the exception path it
    // will typically be a lot higher up the stack.  We will make the following assumptions:
    //
    // 1) on EH path the OS has to push a context onto the stack.  So the SP will be increased by
    //     at least the size of a context when calling a destructor through EH path.
    //
    // 2) the CRT will use minimal stack space to call a destructor.  This is assumed to be less
    //     than the size of a context.
    //
    // Caveats:
    //
    // 1) If there is less than a context on the stack on the EH path, we will miss the fact that
    //     an exception occurred
    //
    // 2) If the CRT uses near the size of a context before calling the destructor in the normal case,
    //     we will assume we've got an exception and ASSERT.
    //
    // So if we arrive at our backout code and the SP is more than the size of a context beyond the original SP,
    // we assume we are on an EH path.
    //
    return (origSP - sizeof(CONTEXT)) > backoutSP;

}


DebugSOIntolerantTransitionHandlerBeginOnly::DebugSOIntolerantTransitionHandlerBeginOnly(EEThreadHandle thread)
{
    SCAN_SCOPE_BEGIN;
    ANNOTATION_FN_SO_INTOLERANT;

    // save the SP so that we can check if the dtor is being called with a much bigger one
    m_ctorSP = (char *)GetCurrentSP();
    m_clrDebugState = GetClrDebugState();
    m_prevSOTolerantState = m_clrDebugState->BeginSOIntolerant();
}

DebugSOIntolerantTransitionHandlerBeginOnly::~DebugSOIntolerantTransitionHandlerBeginOnly()
{
    SCAN_SCOPE_END;

    // A DebugSOIntolerantTransitionHandlerBeginOnly is instantiated only for cases where we will not see
    // an exception.  So the desctructor should never be called on an exception path.  This will check if
    // we are handling an exception and raise an assert if so.

    //
    // We need to determine if we are being called in the normal or exception path.  (Sure would be
    // nice if the CRT would tell us.)   We use the stack pointer to determine this.  On the normal path
    // the stack pointer should be not far from the this pointer, whereas on the exception path it
    // will typically be a lot higher up the stack.  We will make the following assumptions:
    //
    // 1) on EH path the OS has to push a context onto the stack.  So the SP will be increased by
    //     at least the size of a context when calling a destructor through EH path.
    //
    // 2) the CRT will use minimal stack space to call a destructor.  This is assumed to be less
    //     than the size of a context.
    //
    // Caveats:
    //
    // 1) If there is less than a context on the stack on the EH path, we will miss the fact that
    //     an exception occurred
    //
    // 2) If the CRT uses near the size of a context before calling the destructor in the normal case,
    //     we will assume we've got an exception and ASSERT.
    //
    // So if we arrive at our destructor and the SP is within the size of a context beyond the SP when
    // we called the ctor, we assume we are on normal path.
    if ((m_ctorSP - sizeof(CONTEXT)) > (LPVOID)GetCurrentSP())
    {
        _ASSERTE(!"An exception cannot leak through a SO_INTOLERANT_CODE_NOTHROW boundary");
    }

    m_clrDebugState->SetSOTolerance(m_prevSOTolerantState);
}
#endif // STACK_GUARDS_DEBUG

#if defined(FEATURE_STACK_PROBE) && defined(_DEBUG)

#undef __STACKPROBE_inl__

#define INCLUDE_RETAIL_STACK_PROBE

#include "stackprobe.inl"

#endif // defined(FEATURE_STACK_PROBE) && defined(_DEBUG)

#if 0 //FEATURE_FUSION_FAST_CLOSURE - was too buggy at the end of Dev10, not used since then. Delete it after Dev12 if it is still not fixed and used.

#ifdef FEATURE_STACK_PROBE
// This is a helper that fusion (CFastAssemblyBindingClosure) uses to
// do an interior stack probe.
HRESULT InteriorStackProbeNothrowCheckThread()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    HRESULT hr = S_OK;
    INTERIOR_STACK_PROBE_NOTHROW_CHECK_THREAD(hr = E_OUTOFMEMORY;);
    END_INTERIOR_STACK_PROBE;
    
    return hr;
}
#endif

#endif //0 - FEATURE_FUSION_FAST_CLOSURE
