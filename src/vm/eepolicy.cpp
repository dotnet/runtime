// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ---------------------------------------------------------------------------
// EEPolicy.cpp
// ---------------------------------------------------------------------------


#include "common.h"
#include "eepolicy.h"
#include "corhost.h"
#include "dbginterface.h"
#include "eemessagebox.h"

#include "eventreporter.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#include "typestring.h"

#ifndef FEATURE_PAL
#include "dwreport.h"
#endif // !FEATURE_PAL

#include "eventtrace.h"
#undef ExitProcess

BYTE g_EEPolicyInstance[sizeof(EEPolicy)];

void InitEEPolicy()
{
    WRAPPER_NO_CONTRACT;
    new (g_EEPolicyInstance) EEPolicy();
}

EEPolicy::EEPolicy ()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    
    int n;
    for (n = 0; n < MaxClrOperation; n++) {
        m_Timeout[n] = INFINITE;
        m_ActionOnTimeout[n] = eNoAction;
        m_DefaultAction[n] = eNoAction;
    }
    m_Timeout[OPR_ProcessExit] = 40000;
    m_ActionOnTimeout[OPR_ProcessExit] = eRudeExitProcess;
    m_ActionOnTimeout[OPR_ThreadAbort] = eAbortThread;
    m_ActionOnTimeout[OPR_ThreadRudeAbortInNonCriticalRegion] = eRudeAbortThread;
    m_ActionOnTimeout[OPR_ThreadRudeAbortInCriticalRegion] = eRudeAbortThread;

    m_DefaultAction[OPR_ThreadAbort] = eAbortThread;
    m_DefaultAction[OPR_ThreadRudeAbortInNonCriticalRegion] = eRudeAbortThread;
    m_DefaultAction[OPR_ThreadRudeAbortInCriticalRegion] = eRudeAbortThread;
    m_DefaultAction[OPR_AppDomainUnload] = eUnloadAppDomain;
    m_DefaultAction[OPR_AppDomainRudeUnload] = eRudeUnloadAppDomain;
    m_DefaultAction[OPR_ProcessExit] = eExitProcess;
    m_DefaultAction[OPR_FinalizerRun] = eNoAction;

    for (n = 0; n < MaxClrFailure; n++) {
        m_ActionOnFailure[n] = eNoAction;
    }
    m_ActionOnFailure[FAIL_CriticalResource] = eThrowException;
    m_ActionOnFailure[FAIL_NonCriticalResource] = eThrowException;
    m_ActionOnFailure[FAIL_OrphanedLock] = eNoAction;
    m_ActionOnFailure[FAIL_FatalRuntime] = eRudeExitProcess;
    // For CoreCLR, initialize the default action for AV processing to all
    // all kind of code to catch AV exception. If the host wants, they can
    // specify a different action for this.
    m_ActionOnFailure[FAIL_AccessViolation] = eNoAction;
    m_ActionOnFailure[FAIL_StackOverflow] = eRudeExitProcess;
    m_ActionOnFailure[FAIL_CodeContract] = eThrowException;
    m_unhandledExceptionPolicy = eRuntimeDeterminedPolicy;
}

BOOL EEPolicy::IsValidActionForOperation(EClrOperation operation, EPolicyAction action)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    
    switch (operation) {
    case OPR_ThreadAbort:
        return action >= eAbortThread &&
            action < MaxPolicyAction;
    case OPR_ThreadRudeAbortInNonCriticalRegion:
    case OPR_ThreadRudeAbortInCriticalRegion:
        return action >= eRudeAbortThread && action != eUnloadAppDomain &&
            action < MaxPolicyAction;
    case OPR_AppDomainUnload:
        return action >= eUnloadAppDomain &&
            action < MaxPolicyAction;
    case OPR_AppDomainRudeUnload:
        return action >= eRudeUnloadAppDomain &&
            action < MaxPolicyAction;
    case OPR_ProcessExit:
        return action >= eExitProcess &&
            action < MaxPolicyAction;
    case OPR_FinalizerRun:
        return action == eNoAction ||
            (action >= eAbortThread &&
             action < MaxPolicyAction);
    default:
        _ASSERT (!"Do not know valid action for this operation");
        break;
    }
    return FALSE;
}

BOOL EEPolicy::IsValidActionForTimeout(EClrOperation operation, EPolicyAction action)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    
    switch (operation) {
    case OPR_ThreadAbort:
        return action > eAbortThread &&
            action < MaxPolicyAction;
    case OPR_ThreadRudeAbortInNonCriticalRegion:
    case OPR_ThreadRudeAbortInCriticalRegion:
        return action > eRudeUnloadAppDomain &&
            action < MaxPolicyAction;
    case OPR_AppDomainUnload:
        return action > eUnloadAppDomain &&
            action < MaxPolicyAction;
    case OPR_AppDomainRudeUnload:
        return action > eRudeUnloadAppDomain &&
            action < MaxPolicyAction;
    case OPR_ProcessExit:
        return action > eExitProcess &&
            action < MaxPolicyAction;
    case OPR_FinalizerRun:
        return action == eNoAction ||
            (action >= eAbortThread &&
             action < MaxPolicyAction);
    default:
        _ASSERT (!"Do not know valid action for this operation");
        break;
    }
    return FALSE;
}

BOOL EEPolicy::IsValidActionForFailure(EClrFailure failure, EPolicyAction action)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    
    switch (failure) {
    case FAIL_NonCriticalResource:
        return action >= eThrowException &&
            action < MaxPolicyAction;
    case FAIL_CriticalResource:
        return action >= eThrowException &&
            action < MaxPolicyAction;
    case FAIL_FatalRuntime:
        return action >= eRudeExitProcess &&
            action < MaxPolicyAction;
    case FAIL_OrphanedLock:
        return action >= eUnloadAppDomain &&
            action < MaxPolicyAction;
    case FAIL_AccessViolation:
        // Allowed actions on failure are:
        // 
        // eNoAction or eRudeExitProcess.
        return ((action == eNoAction) || (action == eRudeExitProcess));
    case FAIL_StackOverflow:
        return action >= eRudeUnloadAppDomain &&
            action < MaxPolicyAction;
    case FAIL_CodeContract:
        return action >= eThrowException && 
            action <= eExitProcess;
    default:
        _ASSERTE (!"Do not know valid action for this failure");
        break;
    }

    return FALSE;
}

HRESULT EEPolicy::SetTimeout(EClrOperation operation, DWORD timeout)
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    if (static_cast<UINT>(operation) < MaxClrOperation)
    {
    m_Timeout[operation] = timeout;
    if (operation == OPR_FinalizerRun &&
        g_fEEStarted)
    {
        FastInterlockOr((DWORD*)&g_FinalizerWaiterStatus, FWS_WaitInterrupt);
        FinalizerThread::SignalFinalizationDone(FALSE);
    }
    return S_OK;
}
    else
    {
        return E_INVALIDARG;
    }
}

HRESULT EEPolicy::SetActionOnTimeout(EClrOperation operation, EPolicyAction action)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    
    if (static_cast<UINT>(operation) < MaxClrOperation &&
        IsValidActionForTimeout(operation, action))
    {
        m_ActionOnTimeout[operation] = action;
        return S_OK;
    }
    else
    {
        return E_INVALIDARG;
    }
}

EPolicyAction EEPolicy::GetFinalAction(EPolicyAction action, Thread *pThread)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(static_cast<UINT>(action) < MaxPolicyAction);

    if (action < eAbortThread || action > eFastExitProcess)
    {
        return action;
    }

    while(TRUE)
    {
        // Look at default action.  If the default action is more severe,
        // use the default action instead.
        EPolicyAction defaultAction = action;
        switch (action)
        {
            case eAbortThread:
            defaultAction = m_DefaultAction[OPR_ThreadAbort];
                break;
            case eRudeAbortThread:
                if (pThread && !pThread->HasLockInCurrentDomain())
                {
                defaultAction = m_DefaultAction[OPR_ThreadRudeAbortInNonCriticalRegion];
                }
                else
                {
                defaultAction = m_DefaultAction[OPR_ThreadRudeAbortInCriticalRegion];
                }
                break;
            case eUnloadAppDomain:
            defaultAction = m_DefaultAction[OPR_AppDomainUnload];
                break;
            case eRudeUnloadAppDomain:
            defaultAction = m_DefaultAction[OPR_AppDomainRudeUnload];
                break;
            case eExitProcess:
            case eFastExitProcess:
            defaultAction = m_DefaultAction[OPR_ProcessExit];
            if (defaultAction < action)
                {
                defaultAction = action;
                }
                break;
            default:
                break;
            }
        _ASSERTE(static_cast<UINT>(defaultAction) < MaxPolicyAction);

        if (defaultAction == action)
            {
            return action;
            }

        _ASSERTE(defaultAction > action);
        action = defaultAction;
    }
}

// Allow setting timeout and action in one call.
// If we decide to have atomical operation on Policy, we can use lock here
// while SetTimeout and SetActionOnTimeout can not.
HRESULT EEPolicy::SetTimeoutAndAction(EClrOperation operation, DWORD timeout, EPolicyAction action)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    
    if (static_cast<UINT>(operation) < MaxClrOperation &&
        IsValidActionForTimeout(operation, action))
    {
        m_ActionOnTimeout[operation] = action;
        m_Timeout[operation] = timeout;
        if (operation == OPR_FinalizerRun &&
            g_fEEStarted)
        {
            FastInterlockOr((DWORD*)&g_FinalizerWaiterStatus, FWS_WaitInterrupt);
            FinalizerThread::SignalFinalizationDone(FALSE);
        }
        return S_OK;
    }
    else
    {
        return E_INVALIDARG;
    }
}

HRESULT EEPolicy::SetDefaultAction(EClrOperation operation, EPolicyAction action)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    
    if (static_cast<UINT>(operation) < MaxClrOperation &&
        IsValidActionForOperation(operation, action))
    {
        m_DefaultAction[operation] = action;
        return S_OK;
    }
    else
    {
        return E_INVALIDARG;
    }
}

HRESULT EEPolicy::SetActionOnFailure(EClrFailure failure, EPolicyAction action)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    if (static_cast<UINT>(failure) < MaxClrFailure &&
        IsValidActionForFailure(failure, action))
    {
        m_ActionOnFailure[failure] = action;
        return S_OK;
    }
    else
    {
        return E_INVALIDARG;
    }
}

EPolicyAction EEPolicy::GetActionOnFailureNoHostNotification(EClrFailure failure)
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }CONTRACTL_END;

    _ASSERTE (failure < MaxClrFailure);
    if (failure == FAIL_StackOverflow)
    {
        return m_ActionOnFailure[failure];
    }

    return GetFinalAction(m_ActionOnFailure[failure], GetThread());
}

EPolicyAction EEPolicy::GetActionOnFailure(EClrFailure failure)
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }CONTRACTL_END;

    _ASSERTE(static_cast<UINT>(failure) < MaxClrFailure);
    if (failure == FAIL_StackOverflow)
    {
        return m_ActionOnFailure[failure];
    }

    EPolicyAction finalAction = GetActionOnFailureNoHostNotification(failure);
    return finalAction;
}


void EEPolicy::NotifyHostOnTimeout(EClrOperation operation, EPolicyAction action)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

}


void EEPolicy::NotifyHostOnDefaultAction(EClrOperation operation, EPolicyAction action)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

}

void SafeExitProcess(UINT exitCode, BOOL fAbort = FALSE, ShutdownCompleteAction sca = SCA_ExitProcessWhenShutdownComplete)
{
    STRESS_LOG2(LF_SYNC, LL_INFO10, "SafeExitProcess: exitCode = %d, fAbort = %d\n", exitCode, fAbort);
    CONTRACTL
    {
        DISABLED(GC_TRIGGERS);
        NOTHROW;
    }
    CONTRACTL_END;

    // The runtime must be in the appropriate thread mode when we exit, so that we
    // aren't surprised by the thread mode when our DLL_PROCESS_DETACH occurs, or when
    // other DLLs call Release() on us in their detach [dangerous!], etc.
    GCX_PREEMP_NO_DTOR();
    
    FastInterlockExchange((LONG*)&g_fForbidEnterEE, TRUE);

    // Note that for free and retail builds StressLog must also be enabled
    if (g_pConfig && g_pConfig->StressLog())
    {
        if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_BreakOnBadExit))
        {
            // Workaround for aspnet
            PathString  wszFilename;
            bool bShouldAssert = true;
            if (WszGetModuleFileName(NULL, wszFilename))
            {
                wszFilename.LowerCase();
                
                if (wcsstr(wszFilename, W("aspnet_compiler"))) 
                {
                    bShouldAssert = false;
                }                   
            }
            
            unsigned goodExit = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_SuccessExit);
            if (bShouldAssert && exitCode != goodExit)
            {
                _ASSERTE(!"Bad Exit value");
                FAULT_NOT_FATAL();      // if we OOM we can simply give up
                SetErrorMode(0);        // Insure that we actually cause the messsage box to pop. 
                EEMessageBoxCatastrophic(IDS_EE_ERRORMESSAGETEMPLATE, IDS_EE_ERRORTITLE, exitCode, W("BreakOnBadExit: returning bad exit code"));
            }
        }
    }
    
    // If we call ExitProcess, other threads will be torn down 
    // so we don't get to debug their state.  Stop this!
#ifdef _DEBUG
    if (_DbgBreakCount)
        _ASSERTE(!"In SafeExitProcess: An assert was hit on some other thread");
#endif

    // Turn off exception processing, because if some other random DLL has a
    //  fault in DLL_PROCESS_DETACH, we could get called for exception handling.
    //  Since we've turned off part of the runtime, we can't, for instance,
    //  properly execute the GC that handling an exception might trigger.
    g_fNoExceptions = true;
    LOG((LF_EH, LL_INFO10, "SafeExitProcess: turning off exceptions\n"));

    if (sca == SCA_ExitProcessWhenShutdownComplete)
    {
        // disabled because if we fault in this code path we will trigger our
        // Watson code
        CONTRACT_VIOLATION(ThrowsViolation);

#ifdef FEATURE_PAL
        if (fAbort)
        {
            TerminateProcess(GetCurrentProcess(), exitCode);
        }
#endif

        EEPolicy::ExitProcessViaShim(exitCode);
    }
}

// This is a helper to exit the process after coordinating with the shim. It is used by 
// SafeExitProcess above, as well as from CorHost2::ExitProcess when we know that we must
// exit the process without doing further work to shutdown this runtime. This first attempts
// to call back to the Shim to shutdown any other runtimes within the process. 
//
// IMPORTANT NOTE: exercise extreme caution when adding new calls to this method. It is highly
// likely that you want to call SafeExitProcess, or EEPolicy::HandleExitProcess instead of this.
// This function only exists to factor some common code out of the methods mentioned above.

//static 
void EEPolicy::ExitProcessViaShim(UINT exitCode)
{
    LIMITED_METHOD_CONTRACT;

    // We must call back to the Shim in order to exit the process, as this may be just one
    // runtime in a process with many. We need to give the other runtimes a chance to exit
    // cleanly. If we can't make the call, or if the call fails for some reason, then we
    // simply exit the process here, which is rude to the others, but the best we can do.

    ExitProcess(exitCode);
}

//---------------------------------------------------------------------------------------
// HandleExitProcessHelper is used to shutdown the runtime as specified by the given
// action, then to exit the process. Note, however, that the process will not exit if
// sca is SCA_ReturnWhenShutdownComplete. In that case, this method will simply return after
// performing the shutdown actions.
//---------------------------------------------------------------------------------------

// If g_fFastExitProcess is 0, normal shutdown
// If g_fFastExitProcess is 1, fast shutdown.  Only doing log.
// If g_fFastExitProcess is 2, do not run EEShutDown.
DWORD g_fFastExitProcess = 0;

extern void STDMETHODCALLTYPE EEShutDown(BOOL fIsDllUnloading);

static void HandleExitProcessHelper(EPolicyAction action, UINT exitCode, ShutdownCompleteAction sca)
{
    WRAPPER_NO_CONTRACT;
    
    switch (action) {
    case eFastExitProcess:
        g_fFastExitProcess = 1;
    case eExitProcess:
        if (g_fEEStarted)
        {
            EEShutDown(FALSE);
        }
        if (exitCode == 0)
        {
            exitCode = GetLatchedExitCode();
        }
        SafeExitProcess(exitCode, FALSE, sca);
        break;
    case eRudeExitProcess:
        g_fFastExitProcess = 2;
        SafeExitProcess(exitCode, TRUE, sca);
        break;
    default:
        _ASSERTE (!"Invalid policy");
        break;
    }
}


EPolicyAction EEPolicy::DetermineResourceConstraintAction(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    EPolicyAction action;
    if (pThread->HasLockInCurrentDomain()) {
        action = GetEEPolicy()->GetActionOnFailure(FAIL_CriticalResource);
    }
    else
        action = GetEEPolicy()->GetActionOnFailure(FAIL_NonCriticalResource);

    AppDomain *pDomain = GetAppDomain();
    // If it is default domain, we can not unload the appdomain 
    if (pDomain == SystemDomain::System()->DefaultDomain() &&
        (action == eUnloadAppDomain || action == eRudeUnloadAppDomain))
    {
        action = eThrowException;
    }
    return action;
}

void EEPolicy::PerformResourceConstraintAction(Thread *pThread, EPolicyAction action, UINT exitCode, BOOL haveStack)
    {
    WRAPPER_NO_CONTRACT;

    _ASSERTE(GetAppDomain() != NULL);

    switch (action) {
    case eThrowException:
        // Caller is going to rethrow.
        return;
        break;
    case eAbortThread:
        pThread->UserAbort(Thread::TAR_Thread, TA_Safe, GetEEPolicy()->GetTimeout(OPR_ThreadAbort), Thread::UAC_Normal);
        break;
    case eRudeAbortThread:
        pThread->UserAbort(Thread::TAR_Thread, TA_Rude, GetEEPolicy()->GetTimeout(OPR_ThreadAbort), Thread::UAC_Normal);
        break;
    case eExitProcess:
    case eFastExitProcess:
    case eRudeExitProcess:
        HandleExitProcessFromEscalation(action, exitCode);
        break;
    default:
        _ASSERTE (!"Invalid policy");
        break;
    }
}

void EEPolicy::HandleOutOfMemory()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE (g_pOutOfMemoryExceptionClass);

    Thread *pThread = GetThread();
    _ASSERTE (pThread);

    EPolicyAction action = DetermineResourceConstraintAction(pThread);
    
    // Check if we are executing in the context of a Constrained Execution Region.
    if (action != eThrowException && Thread::IsExecutingWithinCer())
    {
        // Hitting OOM in a CER region should throw the OOM without regard to the escalation policy 
        // since the CER author has declared they are hardened against such failures. That's 
        // the whole point of CERs, to denote regions where code knows exactly how to deal with 
        // failures in an attempt to minimize the need for rollback or recycling.
        return;
    }

    PerformResourceConstraintAction(pThread, action, HOST_E_EXITPROCESS_OUTOFMEMORY, TRUE);
}

//---------------------------------------------------------------------------------------
//
// EEPolicy::HandleStackOverflow - Handle stack overflow according to policy
//
// Arguments:
//    detector: 
//    pLimitFrame: the limit of search for frames in order to decide if in SO tolerant
//
// Return Value:
//    None.
// 
// How is stack overflow handled?
// If stack overflows in non-hosted case, we terminate the process.
// For hosted case with escalation policy
// 1. If stack overflows in managed code, or in VM before switching to SO intolerant region, and the GC mode is Cooperative 
//    the domain is rudely unloaded, or the process is terminated if the current domain is default domain.
//    a. This action is done through BEGIN_SO_TOLERANT_CODE if there is one.
//    b. If there is not this macro on the stack, we mark the domain being unload requested, and when the thread
//       dies or is recycled, we finish the AD unload.
// 2. If stack overflows in SO tolerant region, but the GC mode is Preemptive, the process is killed in vector handler, or our
//    managed exception handler (COMPlusFrameHandler or ProcessCLRException).
// 3. If stack overflows in SO intolerant region, the process is killed as soon as the exception is seen by our vector handler, or
//    our managed exception handler.
//
// The process is terminated if there is StackOverflow as all clr code is considered SO Intolerant.
void EEPolicy::HandleStackOverflow(StackOverflowDetector detector, void * pLimitFrame)
{
    WRAPPER_NO_CONTRACT;
    
    STRESS_LOG0(LF_EH, LL_INFO100, "In EEPolicy::HandleStackOverflow\n");

    Thread *pThread = GetThread();

    if (pThread == NULL)
    {
        // For security reason, it is not safe to continue execution if stack overflow happens
        return;
    }

    EXCEPTION_POINTERS exceptionInfo;
    GetCurrentExceptionPointers(&exceptionInfo);

    _ASSERTE(exceptionInfo.ExceptionRecord);

    EEPolicy::HandleFatalStackOverflow(&exceptionInfo);
}


// We provide WatsonLastChance with a SO exception record. The ExceptionAddress is set to 0
// here.  This ExceptionPointers struct is handed off to the debugger as is. A copy of this struct
// is made before invoking Watson and the ExceptionAddress is set by inspecting the stack. Note
// that the ExceptionContext member is unused and so it's ok to set it to NULL.
static EXCEPTION_RECORD g_SOExceptionRecord = {
               STATUS_STACK_OVERFLOW, // ExceptionCode
               0,                     // ExceptionFlags
               NULL,                  // ExceptionRecord
               0,                     // ExceptionAddress
               0,                     // NumberOfParameters
               {} };                  // ExceptionInformation
               
EXCEPTION_POINTERS g_SOExceptionPointers = {&g_SOExceptionRecord, NULL};

//---------------------------------------------------------------------------------------
// HandleExitProcess is used to shutdown the runtime, based on policy previously set,
// then to exit the process. Note, however, that the process will not exit if
// sca is SCA_ReturnWhenShutdownComplete. In that case, this method will simply return after
// performing the shutdown actions.
//---------------------------------------------------------------------------------------
void EEPolicy::HandleExitProcess(ShutdownCompleteAction sca)
{
    WRAPPER_NO_CONTRACT;    

    STRESS_LOG0(LF_EH, LL_INFO100, "In EEPolicy::HandleExitProcess\n");
    
    EPolicyAction action = GetEEPolicy()->GetDefaultAction(OPR_ProcessExit, NULL);
    GetEEPolicy()->NotifyHostOnDefaultAction(OPR_ProcessExit,action);
    HandleExitProcessHelper(action, 0, sca);
}

StackWalkAction LogCallstackForLogCallback(
    CrawlFrame       *pCF,      //
    VOID*             pData     // Caller's private data
)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SmallStackSString *pWordAt = ((SmallStackSString*)pData);

    MethodDesc *pMD = pCF->GetFunction();
    _ASSERTE(pMD != NULL);

    StackSString str;
    str = *pWordAt;

    TypeString::AppendMethodInternal(str, pMD, TypeString::FormatNamespace|TypeString::FormatFullInst|TypeString::FormatSignature); 
    PrintToStdErrW(str.GetUnicode());
    PrintToStdErrA("\n");

    return SWA_CONTINUE;
}

//---------------------------------------------------------------------------------------
//
// A worker to save managed stack trace.
//
// Arguments:
//    None
//
// Return Value:
//    None
//
inline void LogCallstackForLogWorker()
{
    Thread* pThread = GetThread();
    _ASSERTE (pThread);

    SmallStackSString WordAt;

    if (!WordAt.LoadResource(CCompRC::Optional, IDS_ER_WORDAT))
    {
        WordAt.Set(W("   at"));
    }
    else
    {
        WordAt.Insert(WordAt.Begin(), W("   "));
    }
    WordAt += W(" ");

    pThread->StackWalkFrames(&LogCallstackForLogCallback, &WordAt, QUICKUNWIND | FUNCTIONSONLY);
}

//---------------------------------------------------------------------------------------
//
// Print information on fatal error to stderr.
//
// Arguments:
//    exitCode - code of the fatal error
//    pszMessage - error message (can be NULL)
//    errorSource - details on the source of the error
//    argExceptionString - exception details
//
// Return Value:
//    None
//
void LogInfoForFatalError(UINT exitCode, LPCWSTR pszMessage, LPCWSTR errorSource, LPCWSTR argExceptionString)
{
    WRAPPER_NO_CONTRACT;

    Thread *pThread = GetThread();
    EX_TRY
    {
        if ((exitCode == (UINT)COR_E_FAILFAST) && (errorSource == NULL))
        {
            PrintToStdErrA("FailFast:\n");
        }

        if (errorSource != NULL)
        {
            PrintToStdErrW(errorSource);
            PrintToStdErrA("\n");
        }

        if (pszMessage != NULL)
        {
            PrintToStdErrW(pszMessage);
        }
        else
        {
            // If no message was passed in, generate it from the exitCode
            SString exitCodeMessage;
            GetHRMsg(exitCode, exitCodeMessage);
            PrintToStdErrW((LPCWSTR)exitCodeMessage);
        }

        PrintToStdErrA("\n");

        if (pThread && errorSource == NULL)
        {
            LogCallstackForLogWorker();

            if (argExceptionString != NULL) {
                PrintToStdErrA("\n");
                PrintToStdErrA("Exception details:");
                PrintToStdErrA("\n");
                PrintToStdErrW(argExceptionString);
                PrintToStdErrA("\n");
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)
}

//This starts FALSE and then converts to true if HandleFatalError has ever been called by a GC thread
BOOL g_fFatalErrorOccuredOnGCThread = FALSE;
//
// Log an error to the event log if possible, then throw up a dialog box.
//

void EEPolicy::LogFatalError(UINT exitCode, UINT_PTR address, LPCWSTR pszMessage, PEXCEPTION_POINTERS pExceptionInfo, LPCWSTR errorSource, LPCWSTR argExceptionString)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    _ASSERTE(pExceptionInfo != NULL);

    // Log exception to StdErr
    LogInfoForFatalError(exitCode, pszMessage, errorSource, argExceptionString);

    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, FailFast))
    {
        // Fire an ETW FailFast event
        FireEtwFailFast(pszMessage, 
                        (const PVOID)address, 
                        ((pExceptionInfo && pExceptionInfo->ExceptionRecord) ? pExceptionInfo->ExceptionRecord->ExceptionCode : 0), 
                        exitCode, 
                        GetClrInstanceId());
    }

#ifndef FEATURE_PAL
    // Write an event log entry. We do allocate some resources here (spread between the stack and maybe the heap for longer
    // messages), so it's possible for the event write to fail. If needs be we can use a more elaborate scheme here in the future
    // (maybe trying multiple approaches and backing off on failure, falling back on a limited size static buffer as a last
    // resort). In all likelihood the Win32 event reporting mechanism requires resources though, so it's not clear how much
    // effort we should put into this without knowing the benefit we'd receive.
    EX_TRY
    {
        if (ShouldLogInEventLog())
        {
            // If the exit code is COR_E_FAILFAST then the fatal error was raised by managed code and the address argument points to a
            // unicode message buffer rather than a faulting EIP.
            EventReporter::EventReporterType failureType = EventReporter::ERT_UnmanagedFailFast;
            if (exitCode == (UINT)COR_E_FAILFAST)
                failureType = EventReporter::ERT_ManagedFailFast;
            else if (exitCode == (UINT)COR_E_CODECONTRACTFAILED)
                failureType = EventReporter::ERT_CodeContractFailed;
            EventReporter reporter(failureType);
            StackSString s(argExceptionString);

            if ((exitCode == (UINT)COR_E_FAILFAST) || (exitCode == (UINT)COR_E_CODECONTRACTFAILED) || (exitCode == (UINT)CLR_E_GC_OOM))
            {
                if (pszMessage)
                {
                    reporter.AddDescription((WCHAR*)pszMessage);
                }

                if (argExceptionString)
                {
                    reporter.AddFailFastStackTrace(s);
                }

                if (exitCode != (UINT)CLR_E_GC_OOM)
                    LogCallstackForEventReporter(reporter);
            }
            else
            {
                // Fetch the localized Fatal Execution Engine Error text or fall back on a hardcoded variant if things get dire.
                InlineSString<80> ssMessage;
                InlineSString<80> ssErrorFormat;
                if(!ssErrorFormat.LoadResource(CCompRC::Optional, IDS_ER_UNMANAGEDFAILFASTMSG ))
                    ssErrorFormat.Set(W("at IP %1 (%2) with exit code %3."));
                SmallStackSString addressString;
                addressString.Printf(W("%p"), pExceptionInfo? (UINT_PTR)pExceptionInfo->ExceptionRecord->ExceptionAddress : address);

                // We should always have the reference to the runtime's instance
                _ASSERTE(g_pMSCorEE != NULL);

                // Setup the string to contain the runtime's base address. Thus, when customers report FEEE with just
                // the event log entry containing this string, we can use the absolute and base addresses to determine
                // where the fault happened inside the runtime.
                SmallStackSString runtimeBaseAddressString;
                runtimeBaseAddressString.Printf(W("%p"), g_pMSCorEE);

                SmallStackSString exitCodeString;
                exitCodeString.Printf(W("%x"), exitCode);

                // Format the string
                ssMessage.FormatMessage(FORMAT_MESSAGE_FROM_STRING, (LPCWSTR)ssErrorFormat, 0, 0, addressString, runtimeBaseAddressString, 
                    exitCodeString);
                reporter.AddDescription(ssMessage);
            }

            reporter.Report();
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)
#endif // !FEATURE_PAL

#ifdef _DEBUG
    // If we're native-only (Win32) debugging this process, we'd love to break now.
    // However, we should not do this because a managed debugger attached to a 
    // SxS runtime also appears to be a native debugger. Unfortunately, the managed
    // debugger won't handle any native event from another runtime, which means this
    // breakpoint would go unhandled and terminate the process. Instead, we will let
    // the process continue so at least the fatal error is logged rather than abrupt
    // termination.
    //
    // This behavior can still be overridden if the right config value is set.
    if (IsDebuggerPresent())
    {
        bool fBreak = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgOOBinFEEE) != 0);

        if (fBreak)
        {
            DebugBreak();
        }
    }
#endif // _DEBUG

    {
#ifdef DEBUGGING_SUPPORTED
        //Give a managed debugger a chance if this fatal error is on a managed thread.
        Thread *pThread = GetThread();

        if (pThread && !g_fFatalErrorOccuredOnGCThread)
        {
            GCX_COOP();

            OBJECTHANDLE ohException = NULL;

            if (exitCode == (UINT)COR_E_STACKOVERFLOW)
            {
                // If we're going down because of stack overflow, go ahead and use the preallocated SO exception.
                ohException = CLRException::GetPreallocatedStackOverflowExceptionHandle();
            }
            else
            {
                // Though we would like to remove the usage of ExecutionEngineException in any manner,
                // we cannot. Its okay to use it in the case below since the process is terminating
                // and this will serve as an exception object for debugger.
                ohException = CLRException::GetPreallocatedExecutionEngineExceptionHandle();
            }

            // Preallocated exception handles can be null if FailFast is invoked before LoadBaseSystemClasses 
            // (in SystemDomain::Init) finished.  See Dev10 Bug 677432 for the detail.
            if (ohException != NULL)
            {
                // for fail-fast, if there's a LTO available then use that as the inner exception object
                // for the FEEE we'll be reporting.  this can help the Watson back-end to generate better
                // buckets for apps that call Environment.FailFast() and supply an exception object.
                OBJECTREF lto = pThread->LastThrownObject();

                if (exitCode == static_cast<UINT>(COR_E_FAILFAST) && lto != NULL)
                {
                    EXCEPTIONREF curEx = (EXCEPTIONREF)ObjectFromHandle(ohException);
                    curEx->SetInnerException(lto);
                }
                pThread->SetLastThrownObject(ObjectFromHandle(ohException), TRUE);
            }

            // If a managed debugger is already attached, and if that debugger is thinking it might be inclined to
            // try to intercept this excepiton, then tell it that's not possible.
            if (pThread->IsExceptionInProgress())
            {
                pThread->GetExceptionState()->GetFlags()->SetDebuggerInterceptNotPossible();
            }
        }

        if  (EXCEPTION_CONTINUE_EXECUTION == WatsonLastChance(pThread, pExceptionInfo, TypeOfReportedError::FatalError))
        {
            LOG((LF_EH, LL_INFO100, "EEPolicy::LogFatalError: debugger ==> EXCEPTION_CONTINUE_EXECUTION\n"));
            _ASSERTE(!"Debugger should not have returned ContinueExecution");
        }
#endif // DEBUGGING_SUPPORTED
    }
}

void DisplayStackOverflowException()
{
    LIMITED_METHOD_CONTRACT;
    PrintToStdErrA("\n");

    PrintToStdErrA("Process is terminating due to StackOverflowException.\n");
}

void DECLSPEC_NORETURN EEPolicy::HandleFatalStackOverflow(EXCEPTION_POINTERS *pExceptionInfo, BOOL fSkipDebugger)
{
    // This is fatal error.  We do not care about SO mode any more.
    // All of the code from here on out is robust to any failures in any API's that are called.
    CONTRACT_VIOLATION(GCViolation | ModeViolation | FaultNotFatal | TakesLockViolation);

    WRAPPER_NO_CONTRACT;

    STRESS_LOG0(LF_EH, LL_INFO100, "In EEPolicy::HandleFatalStackOverflow\n");

    DisplayStackOverflowException();

    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, FailFast))
    {
        // Fire an ETW FailFast event
        FireEtwFailFast(W("StackOverflowException"),  
                       (const PVOID)((pExceptionInfo && pExceptionInfo->ContextRecord) ? GetIP(pExceptionInfo->ContextRecord) : 0), 
                       ((pExceptionInfo && pExceptionInfo->ExceptionRecord) ? pExceptionInfo->ExceptionRecord->ExceptionCode : 0), 
                       COR_E_STACKOVERFLOW, 
                       GetClrInstanceId());
    }

    if (!fSkipDebugger)
    {
        Thread *pThread = GetThread();
        BOOL fTreatAsNativeUnhandledException = FALSE;
        if (pThread)
        {
            GCX_COOP();
            // If we had a SO before preallocated exception objects are initialized, we will AV here. This can happen
            // during the initialization of SystemDomain during EEStartup. Thus, setup the SO throwable only if its not 
            // NULL. 
            //
            // When WatsonLastChance (WLC) is invoked below, it treats this case as UnhandledException. If there is no
            // managed exception object available, we should treat this case as NativeUnhandledException. This aligns
            // well with the fact that there cannot be a managed debugger attached at this point that will require
            // LastChanceManagedException notification to be delivered. Also, this is the same as how
            // we treat an unhandled exception as NativeUnhandled when throwable is not available.
            OBJECTHANDLE ohSO = CLRException::GetPreallocatedStackOverflowExceptionHandle();
            if (ohSO != NULL)
            {
                pThread->SafeSetThrowables(ObjectFromHandle(ohSO) 
                                           DEBUG_ARG(ThreadExceptionState::STEC_CurrentTrackerEqualNullOkHackForFatalStackOverflow),
                                           TRUE);
            }
            else
            {
                // We dont have a throwable - treat this as native unhandled exception
                fTreatAsNativeUnhandledException = TRUE;
            }   
        }
        FrameWithCookie<FaultingExceptionFrame> fef;
#if defined(WIN64EXCEPTIONS)
        *((&fef)->GetGSCookiePtr()) = GetProcessGSCookie();
#endif // WIN64EXCEPTIONS
        if (pExceptionInfo && pExceptionInfo->ContextRecord)
        {
            GCX_COOP();
            fef.InitAndLink(pExceptionInfo->ContextRecord);
        }

#ifndef FEATURE_PAL        
        if (IsWatsonEnabled() && (g_pDebugInterface != NULL))
        {
            _ASSERTE(pExceptionInfo != NULL);

            ResetWatsonBucketsParams param;
            param.m_pThread = pThread;
            param.pExceptionRecord = pExceptionInfo->ExceptionRecord;
            g_pDebugInterface->RequestFavor(ResetWatsonBucketsFavorWorker, reinterpret_cast<void *>(&param));
        }
#endif // !FEATURE_PAL        

        WatsonLastChance(pThread, pExceptionInfo, 
            (fTreatAsNativeUnhandledException == FALSE)? TypeOfReportedError::UnhandledException: TypeOfReportedError::NativeThreadUnhandledException);
    }

    TerminateProcess(GetCurrentProcess(), COR_E_STACKOVERFLOW);
    UNREACHABLE();
}

#if defined(_TARGET_X86_) && defined(PLATFORM_WINDOWS)
// This noinline method is required to ensure that RtlCaptureContext captures
// the context of HandleFatalError. On x86 RtlCaptureContext will not capture
// the current method's context
// NOTE: explicitly turning off optimizations to force the compiler to spill to the
//       stack and establish a stack frame. This is required to ensure that 
//       RtlCaptureContext captures the context of HandleFatalError
#pragma optimize("", off)
int NOINLINE WrapperClrCaptureContext(CONTEXT* context)
{
    ClrCaptureContext(context);
    return 0;
}
#pragma optimize("", on)
#endif // defined(_TARGET_X86_) && defined(PLATFORM_WINDOWS)

// This method must return a value to avoid getting non-actionable dumps on x86.
// If this method were a DECLSPEC_NORETURN then dumps would not provide the necessary
// context at the point of the failure
int NOINLINE EEPolicy::HandleFatalError(UINT exitCode, UINT_PTR address, LPCWSTR pszMessage /* = NULL */, PEXCEPTION_POINTERS pExceptionInfo /* = NULL */, LPCWSTR errorSource /* = NULL */, LPCWSTR argExceptionString /* = NULL */)
{
    WRAPPER_NO_CONTRACT;

    // All of the code from here on out is robust to any failures in any API's that are called.
    FAULT_NOT_FATAL();

    EXCEPTION_RECORD   exceptionRecord;
    EXCEPTION_POINTERS exceptionPointers;
    CONTEXT            context;

    if (pExceptionInfo == NULL)
    {
        ZeroMemory(&exceptionPointers, sizeof(exceptionPointers));
        ZeroMemory(&exceptionRecord, sizeof(exceptionRecord));
        ZeroMemory(&context, sizeof(context));
        
        context.ContextFlags = CONTEXT_CONTROL;
#if defined(_TARGET_X86_) && defined(PLATFORM_WINDOWS)
        // Add a frame to ensure that the context captured is this method and not the caller
        WrapperClrCaptureContext(&context);
#else // defined(_TARGET_X86_) && defined(PLATFORM_WINDOWS)
        ClrCaptureContext(&context);
#endif

        exceptionRecord.ExceptionCode = exitCode;
        exceptionRecord.ExceptionAddress = reinterpret_cast< PVOID >(address);

        exceptionPointers.ExceptionRecord = &exceptionRecord;
        exceptionPointers.ContextRecord   = &context;
        pExceptionInfo = &exceptionPointers;
    }

    // All of the code from here on out is allowed to trigger a GC, even if we're in a no-trigger region. We're
    // ripping the process down due to a fatal error... our invariants are already gone.
    {
        // This is fatal error.  We do not care about SO mode any more.
        // All of the code from here on out is robust to any failures in any API's that are called.
        CONTRACT_VIOLATION(GCViolation | ModeViolation | FaultNotFatal | TakesLockViolation);


        // Setting g_fFatalErrorOccuredOnGCThread allows code to avoid attempting to make GC mode transitions which could
        // block indefinately if the fatal error occured during the GC.
        if (IsGCSpecialThread() && GCHeapUtilities::IsGCInProgress())
        {
            g_fFatalErrorOccuredOnGCThread = TRUE;
        }

        // ThreadStore lock needs to be released before continuing with the FatalError handling should 
        // because debugger is going to take CrstDebuggerMutex, whose lock level is higher than that of 
        // CrstThreadStore.  It should be safe to release the lock since execution will not be resumed 
        // after fatal errors.
        if (ThreadStore::HoldingThreadStore(GetThread()))
        {   
            ThreadSuspend::UnlockThreadStore();
        }

        g_fFastExitProcess = 2;

        STRESS_LOG0(LF_CORDB,LL_INFO100, "D::HFE: About to call LogFatalError\n");
        switch (GetEEPolicy()->GetActionOnFailure(FAIL_FatalRuntime))
        {
        case eRudeExitProcess:
            LogFatalError(exitCode, address, pszMessage, pExceptionInfo, errorSource, argExceptionString);
	        SafeExitProcess(exitCode, TRUE);
            break;
        default:
            _ASSERTE(!"Invalid action for FAIL_FatalRuntime");
            break;
        }
    }

    UNREACHABLE();
    return -1;
}

void EEPolicy::HandleExitProcessFromEscalation(EPolicyAction action, UINT exitCode)
{
    WRAPPER_NO_CONTRACT;
    CONTRACT_VIOLATION(GCViolation); 

    _ASSERTE (action >= eExitProcess);
    // If policy for ExitProcess is not default action, i.e. ExitProcess, we will use it.
    // Otherwise overwrite it with passing arg action;
    EPolicyAction todo = GetEEPolicy()->GetDefaultAction(OPR_ProcessExit, NULL);
    if (todo == eExitProcess)
    {
        todo = action;
    }
    GetEEPolicy()->NotifyHostOnDefaultAction(OPR_ProcessExit,todo);

    HandleExitProcessHelper(todo, exitCode, SCA_ExitProcessWhenShutdownComplete);
}

void EEPolicy::HandleCodeContractFailure(LPCWSTR pMessage, LPCWSTR pCondition, LPCWSTR pInnerExceptionAsString)
{
    WRAPPER_NO_CONTRACT;

    EEPolicy* pPolicy = GetEEPolicy();
    // GetActionOnFailure will notify the host for us.
    EPolicyAction action = pPolicy->GetActionOnFailure(FAIL_CodeContract);
    Thread* pThread = GetThread();

    switch(action) {
    case eThrowException:
        // Let managed code throw a ContractException (it's easier to pass the right parameters to the constructor).
        break;
    case eAbortThread:
        pThread->UserAbort(Thread::TAR_Thread, TA_Safe, GetEEPolicy()->GetTimeout(OPR_ThreadAbort), Thread::UAC_Normal);
        break;
    case eRudeAbortThread:
        pThread->UserAbort(Thread::TAR_Thread, TA_Rude, GetEEPolicy()->GetTimeout(OPR_ThreadAbort), Thread::UAC_Normal);
        break;
    case eExitProcess:  // Merged w/ default case
    default:
        _ASSERTE(action == eExitProcess);
        // Since we have no exception object, make sure
        // UE tracker is clean so that RetrieveManagedBucketParameters
        // does not take any bucket details.
#ifndef FEATURE_PAL        
        pThread->GetExceptionState()->GetUEWatsonBucketTracker()->ClearWatsonBucketDetails();
#endif // !FEATURE_PAL
        pPolicy->HandleFatalError(COR_E_CODECONTRACTFAILED, NULL, pMessage);
        break;
    }
}

