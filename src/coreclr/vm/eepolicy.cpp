// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ---------------------------------------------------------------------------
// EEPolicy.cpp
// ---------------------------------------------------------------------------


#include "common.h"
#include "eepolicy.h"
#include "corhost.h"
#include "dbginterface.h"

#include "eventreporter.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#include "typestring.h"

#ifndef TARGET_UNIX
#include "dwreport.h"
#endif // !TARGET_UNIX

#include "eventtrace.h"
#undef ExitProcess

void SafeExitProcess(UINT exitCode, ShutdownCompleteAction sca = SCA_ExitProcessWhenShutdownComplete)
{
    STRESS_LOG2(LF_SYNC, LL_INFO10, "SafeExitProcess: exitCode = %d sca = %d\n", exitCode, sca);
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

    InterlockedExchange((LONG*)&g_fForbidEnterEE, TRUE);

    // Note that for free and retail builds StressLog must also be enabled
    if (g_pConfig && g_pConfig->StressLog())
    {
        if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_BreakOnBadExit))
        {
            unsigned goodExit = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_SuccessExit);
            if (exitCode != goodExit)
            {
                _ASSERTE(!"Bad Exit value");
                FAULT_NOT_FATAL();      // if we OOM we can simply give up
                fprintf(stderr, "Error 0x%08x.\n\nBreakOnBadExit: returning bad exit code.", exitCode);
                DebugBreak();
            }
        }
    }

    // Turn off exception processing, because if some other random DLL has a
    //  fault in DLL_PROCESS_DETACH, we could get called for exception handling.
    //  Since we've turned off part of the runtime, we can't, for instance,
    //  properly execute the GC that handling an exception might trigger.
    g_fNoExceptions = true;
    LOG((LF_EH, LL_INFO10, "SafeExitProcess: turning off exceptions\n"));

    if (sca == SCA_TerminateProcessWhenShutdownComplete)
    {
        // disabled because if we fault in this code path we will trigger our Watson code
        CONTRACT_VIOLATION(ThrowsViolation);

        CrashDumpAndTerminateProcess(exitCode);
    }
    else if (sca == SCA_ExitProcessWhenShutdownComplete)
    {
        // disabled because if we fault in this code path we will trigger our Watson code
        CONTRACT_VIOLATION(ThrowsViolation);

        ExitProcess(exitCode);
    }
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

//---------------------------------------------------------------------------------------
//
// EEPolicy::HandleStackOverflow - Handle stack overflow according to policy
//
// Return Value:
//    None.
//
// How is stack overflow handled?
// If stack overflows, we terminate the process.
void EEPolicy::HandleStackOverflow()
{
    WRAPPER_NO_CONTRACT;

    STRESS_LOG0(LF_EH, LL_INFO100, "In EEPolicy::HandleStackOverflow\n");

    Thread *pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        // For security reason, it is not safe to continue execution if stack overflow happens
        return;
    }

    EXCEPTION_POINTERS exceptionInfo;
    GetCurrentExceptionPointers(&exceptionInfo DEBUG_ARG(!pThread->IsExecutingOnAltStack()));

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

    if (g_fEEStarted)
    {
        EEShutDown(FALSE);
    }
    SafeExitProcess(GetLatchedExitCode(), sca);
}


//---------------------------------------------------------------------------------------
// This class is responsible for displaying a stack trace. It uses a condensed way for
// stack overflow stack traces where there are possibly many repeated frames.
// It displays a count and a repeated sequence of frames at the top of the stack in
// such a case, instead of displaying possibly thousands of lines with the same
// method.
//---------------------------------------------------------------------------------------
class CallStackLogger
{
    // MethodDescs of the stack frames, the TOS is at index 0
    CDynArray<MethodDesc*> m_frames;

    // Index of a stack frame where a possible repetition of frames starts
    int m_commonStartIndex = -1;
    // Length of the largest found repeated sequence of frames
    int m_largestCommonStartLength = 0;
    // Number of repetitions of the largest repeated sequence of frames
    int m_largestCommonStartRepeat = 0;

    StackWalkAction LogCallstackForLogCallbackWorker(CrawlFrame *pCF)
    {
        WRAPPER_NO_CONTRACT;

        MethodDesc *pMD = pCF->GetFunction();

        if (m_commonStartIndex != -1)
        {
            // Some common frames were already found

            if (m_frames[m_frames.Count() - m_commonStartIndex] != pMD)
            {
                // The frame being added is not part of the repeated sequence
                if (m_frames.Count() / m_commonStartIndex >= 2)
                {
                    // A sequence repeated at least twice was found. It is the largest one that was found so far
                    m_largestCommonStartLength = m_commonStartIndex;
                    m_largestCommonStartRepeat = m_frames.Count() / m_commonStartIndex;
                }

                m_commonStartIndex = -1;
            }
        }

        if (m_commonStartIndex == -1)
        {
            if ((m_frames.Count() != 0) && (pMD == m_frames[0]))
            {
                // We have found a frame with the same MethodDesc as the frame at the top of the stack,
                // possibly a new repeated sequence is starting.
                m_commonStartIndex = m_frames.Count();
            }
        }

        MethodDesc** itemPtr = m_frames.Append();
        if (itemPtr == nullptr)
        {
            // Memory allocation failure
            return SWA_ABORT;
        }

        *itemPtr = pMD;

        return SWA_CONTINUE;
    }

    void PrintFrame(int index, const WCHAR* pWordAt)
    {
        WRAPPER_NO_CONTRACT;

        SString str(pWordAt);

        MethodDesc* pMD = m_frames[index];
        TypeString::AppendMethodInternal(str, pMD, TypeString::FormatNamespace|TypeString::FormatFullInst|TypeString::FormatSignature);
        PrintToStdErrW(str.GetUnicode());
        PrintToStdErrA("\n");
    }

public:

    // Callback called by the stack walker for each frame on the stack
    static StackWalkAction LogCallstackForLogCallback(CrawlFrame *pCF, VOID* pData)
    {
        WRAPPER_NO_CONTRACT;

        CallStackLogger* logger = (CallStackLogger*)pData;
        return logger->LogCallstackForLogCallbackWorker(pCF);
    }

    void PrintStackTrace(const WCHAR* pWordAt)
    {
        WRAPPER_NO_CONTRACT;

        if (m_largestCommonStartLength != 0)
        {
            SmallStackSString repeatStr;
            repeatStr.AppendPrintf("Repeat %d times:\n", m_largestCommonStartRepeat);

            PrintToStdErrW(repeatStr.GetUnicode());
            PrintToStdErrA("--------------------------------\n");
            for (int i = 0; i < m_largestCommonStartLength; i++)
            {
                PrintFrame(i, pWordAt);
            }
            PrintToStdErrA("--------------------------------\n");
        }

        for (int i = m_largestCommonStartLength * m_largestCommonStartRepeat; i < m_frames.Count(); i++)
        {
            PrintFrame(i, pWordAt);
        }
    }
};

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
inline void LogCallstackForLogWorker(Thread* pThread)
{
    WRAPPER_NO_CONTRACT;

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

    CallStackLogger logger;

    pThread->StackWalkFrames(&CallStackLogger::LogCallstackForLogCallback, &logger, QUICKUNWIND | FUNCTIONSONLY | ALLOW_ASYNC_STACK_WALK);

    logger.PrintStackTrace(WordAt.GetUnicode());

}

//---------------------------------------------------------------------------------------
//
// Print information on fatal error to stderr.
//
// Arguments:
//    exitCode - code of the fatal error
//    pszMessage - error message (can be NULL)
//    errorSource - details on the source of the error (can be NULL)
//    argExceptionString - exception details (can be NULL)
//
// Return Value:
//    None
//
void LogInfoForFatalError(UINT exitCode, LPCWSTR pszMessage, LPCWSTR errorSource, LPCWSTR argExceptionString)
{
    WRAPPER_NO_CONTRACT;

    static size_t s_pCrashingThreadID;

    size_t currentThreadID;
#ifndef TARGET_UNIX
    currentThreadID = GetCurrentThreadId();
#else
    currentThreadID = PAL_GetCurrentOSThreadId();
#endif

    size_t previousThreadID = InterlockedCompareExchangeT<size_t>(&s_pCrashingThreadID, currentThreadID, 0);

    // Let the first crashing thread take care of the reporting.
    if (previousThreadID != 0)
    {
        if (previousThreadID == currentThreadID)
        {
            PrintToStdErrA("Fatal error while logging another fatal error.\n");
        }
        else
        {
            // Switch to preemptive mode to avoid blocking the crashing thread. It may try to suspend the runtime
            // for GC during the stacktrace reporting.
            GCX_PREEMP();

            ClrSleepEx(INFINITE, /*bAlertable*/ FALSE);
        }
        return;
    }

    EX_TRY
    {
        if (exitCode == (UINT)COR_E_FAILFAST)
        {
            PrintToStdErrA("Process terminated. ");
        }
        else
        {
            PrintToStdErrA("Fatal error. ");
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

        Thread* pThread = GetThreadNULLOk();
        if (pThread && errorSource == NULL)
        {
            LogCallstackForLogWorker(pThread);

            if (argExceptionString != NULL) {
                PrintToStdErrW(argExceptionString);
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)
}

//This starts FALSE and then converts to true if HandleFatalError has ever been called by a GC thread
BOOL g_fFatalErrorOccurredOnGCThread = FALSE;
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

    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, FailFast))
    {
        // Fire an ETW FailFast event
        FireEtwFailFast(pszMessage,
                        (const PVOID)address,
                        ((pExceptionInfo && pExceptionInfo->ExceptionRecord) ? pExceptionInfo->ExceptionRecord->ExceptionCode : 0),
                        exitCode,
                        GetClrInstanceId());
    }

#ifndef TARGET_UNIX
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
                    ssErrorFormat.Set(W("at IP 0x%x (0x%x) with exit code 0x%x."));
                SmallStackSString addressString;
                addressString.Printf(W("%p"), pExceptionInfo? (PVOID)pExceptionInfo->ExceptionRecord->ExceptionAddress : (PVOID)address);

                // We should always have the reference to the runtime's instance
                _ASSERTE(GetClrModuleBase() != NULL);

                // Setup the string to contain the runtime's base address. Thus, when customers report FEEE with just
                // the event log entry containing this string, we can use the absolute and base addresses to determine
                // where the fault happened inside the runtime.
                SmallStackSString runtimeBaseAddressString;
                runtimeBaseAddressString.Printf(W("%p"), GetClrModuleBase());

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
#endif // !TARGET_UNIX

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
        Thread *pThread = GetThreadNULLOk();

        if (pThread && !g_fFatalErrorOccurredOnGCThread)
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
                ohException = g_pPreallocatedExecutionEngineException;
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

    PrintToStdErrA("Stack overflow.\n");
}

DWORD LogStackOverflowStackTraceThread(void* arg)
{
    LogCallstackForLogWorker((Thread*)arg);

    return 0;
}

void DECLSPEC_NORETURN EEPolicy::HandleFatalStackOverflow(EXCEPTION_POINTERS *pExceptionInfo, BOOL fSkipDebugger)
{
    // This is fatal error.  We do not care about SO mode any more.
    // All of the code from here on out is robust to any failures in any API's that are called.
    CONTRACT_VIOLATION(GCViolation | ModeViolation | FaultNotFatal | TakesLockViolation);

    WRAPPER_NO_CONTRACT;

    // Disable GC stress triggering GC at this point, we don't want the GC to start running
    // on this thread when we have only a very limited space left on the stack
    GCStressPolicy::InhibitHolder iholder;

    STRESS_LOG0(LF_EH, LL_INFO100, "In EEPolicy::HandleFatalStackOverflow\n");

    FrameWithCookie<FaultingExceptionFrame> fef;
#if defined(FEATURE_EH_FUNCLETS)
    *((&fef)->GetGSCookiePtr()) = GetProcessGSCookie();
#endif // FEATURE_EH_FUNCLETS
    if (pExceptionInfo && pExceptionInfo->ContextRecord)
    {
        GCX_COOP();
#if defined(TARGET_X86) && defined(TARGET_WINDOWS)
        // For Windows x86, we don't have a reliable method to unwind to the first managed call frame,
        // so we handle at least the cases when the stack overflow happens in JIT helpers
        AdjustContextForJITHelpers(pExceptionInfo->ExceptionRecord, pExceptionInfo->ContextRecord);
#else
        Thread::VirtualUnwindToFirstManagedCallFrame(pExceptionInfo->ContextRecord);
#endif
        fef.InitAndLink(pExceptionInfo->ContextRecord);
    }

    static volatile LONG g_stackOverflowCallStackLogged = 0;

    // Dump stack trace only for the first thread failing with stack overflow to prevent mixing
    // multiple stack traces together.
    if (InterlockedCompareExchange(&g_stackOverflowCallStackLogged, 1, 0) == 0)
    {
        DisplayStackOverflowException();

        HandleHolder stackDumpThreadHandle = Thread::CreateUtilityThread(Thread::StackSize_Small, LogStackOverflowStackTraceThread, GetThreadNULLOk(), W(".NET Stack overflow trace logger"));
        if (stackDumpThreadHandle != INVALID_HANDLE_VALUE)
        {
            // Wait for the stack trace logging completion
            DWORD res = WaitForSingleObject(stackDumpThreadHandle, INFINITE);
            _ASSERTE(res == WAIT_OBJECT_0);
        }

        g_stackOverflowCallStackLogged = 2;
    }
    else
    {
        // Wait for the thread that is logging the stack trace to complete
        while (g_stackOverflowCallStackLogged != 2)
        {
            Sleep(50);
        }
    }

    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, FailFast))
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
        Thread *pThread = GetThreadNULLOk();
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

#ifndef TARGET_UNIX
        if (IsWatsonEnabled() && (g_pDebugInterface != NULL))
        {
            _ASSERTE(pExceptionInfo != NULL);

            ResetWatsonBucketsParams param;
            param.m_pThread = pThread;
            param.pExceptionRecord = pExceptionInfo->ExceptionRecord;
            g_pDebugInterface->RequestFavor(ResetWatsonBucketsFavorWorker, reinterpret_cast<void *>(&param));
        }
#endif // !TARGET_UNIX

        WatsonLastChance(pThread, pExceptionInfo,
            (fTreatAsNativeUnhandledException == FALSE)? TypeOfReportedError::UnhandledException: TypeOfReportedError::NativeThreadUnhandledException);
    }

    CrashDumpAndTerminateProcess(COR_E_STACKOVERFLOW);
    UNREACHABLE();
}

#if defined(TARGET_X86) && defined(TARGET_WINDOWS)
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
#endif // defined(TARGET_X86) && defined(TARGET_WINDOWS)

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
#if defined(TARGET_X86) && defined(TARGET_WINDOWS)
        // Add a frame to ensure that the context captured is this method and not the caller
        WrapperClrCaptureContext(&context);
#else // defined(TARGET_X86) && defined(TARGET_WINDOWS)
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


        // Setting g_fFatalErrorOccurredOnGCThread allows code to avoid attempting to make GC mode transitions which could
        // block indefinitely if the fatal error occurred during the GC.
        if (IsGCSpecialThread() && GCHeapUtilities::IsGCInProgress())
        {
            g_fFatalErrorOccurredOnGCThread = TRUE;
        }

        // ThreadStore lock needs to be released before continuing with the FatalError handling should
        // because debugger is going to take CrstDebuggerMutex, whose lock level is higher than that of
        // CrstThreadStore.  It should be safe to release the lock since execution will not be resumed
        // after fatal errors.
        if (ThreadStore::HoldingThreadStore(GetThreadNULLOk()))
        {
            ThreadSuspend::UnlockThreadStore();
        }

        g_fFastExitProcess = 2;

        STRESS_LOG0(LF_CORDB,LL_INFO100, "D::HFE: About to call LogFatalError\n");
        LogFatalError(exitCode, address, pszMessage, pExceptionInfo, errorSource, argExceptionString);
        SafeExitProcess(exitCode, SCA_TerminateProcessWhenShutdownComplete);
    }

    UNREACHABLE();
    return -1;
}
