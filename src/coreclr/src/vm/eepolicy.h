// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ---------------------------------------------------------------------------
// EEPolicy.h
// ---------------------------------------------------------------------------


#ifndef EEPOLICY_H_
#define EEPOLICY_H_

#include "vars.hpp"
#include "corhost.h"
#include "ceemain.h"

extern "C" UINT_PTR STDCALL GetCurrentIP();

enum StackOverflowDetector
{
    SOD_ManagedFrameHandler,
    SOD_UnmanagedFrameHandler,
    SOD_SOTolerantTransitor,
    SOD_SOIntolerantTransitor,
};

// EEPolicy maintains actions for resource failure and timeout
class EEPolicy
{
public:
    enum ThreadAbortTypes
    {
        TA_None,  // No Abort
        // Abort at a safe spot: not having any lock, not inside finally, not inside catch
        TA_Safe,
        // Do not run user finally, no attention to lock count
        TA_Rude
    };

    enum AppDomainUnloadTypes
    {
        ADU_Safe,
        ADU_Rude
    };

    EEPolicy ();

    HRESULT SetTimeout(EClrOperation operation, DWORD timeout);

    DWORD GetTimeout(EClrOperation operation)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(static_cast<UINT>(operation) < MaxClrOperation);
        return m_Timeout[operation];
    }

    HRESULT SetActionOnTimeout(EClrOperation operation, EPolicyAction action);
    EPolicyAction GetActionOnTimeout(EClrOperation operation, Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(static_cast<UINT>(operation) < MaxClrOperation);
        return GetFinalAction(m_ActionOnTimeout[operation], pThread);
    }

    void NotifyHostOnTimeout(EClrOperation operation, EPolicyAction action);

    HRESULT SetTimeoutAndAction(EClrOperation operation, DWORD timeout, EPolicyAction action);
    
    HRESULT SetDefaultAction(EClrOperation operation, EPolicyAction action);
    EPolicyAction GetDefaultAction(EClrOperation operation, Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(static_cast<UINT>(operation) < MaxClrOperation);
        return GetFinalAction(m_DefaultAction[operation], pThread);
    }

    void NotifyHostOnDefaultAction(EClrOperation operation, EPolicyAction action);

    HRESULT SetActionOnFailure(EClrFailure failure, EPolicyAction action);

    // Generally GetActionOnFailure should be used so that a host can get notification.
    // But if we have notified host on the same failure, but we need to check escalation again,
    // GetActionOnFailureNoHostNotification can be used.
    EPolicyAction GetActionOnFailure(EClrFailure failure);
    EPolicyAction GetActionOnFailureNoHostNotification(EClrFailure failure);

    // get and set unhandled exception policy
    HRESULT SetUnhandledExceptionPolicy(EClrUnhandledException policy)
    {
        LIMITED_METHOD_CONTRACT;
        if (policy != eRuntimeDeterminedPolicy && policy != eHostDeterminedPolicy)
        {
            return E_INVALIDARG;
        }
        else
        {
            m_unhandledExceptionPolicy = policy;
            return S_OK;
        }
    }
    EClrUnhandledException GetUnhandledExceptionPolicy()
    {
        LIMITED_METHOD_CONTRACT;
        return m_unhandledExceptionPolicy;
    }

    static EPolicyAction DetermineResourceConstraintAction(Thread *pThread);

    static void PerformResourceConstraintAction(Thread *pThread, EPolicyAction action, UINT exitCode, BOOL haveStack);

    static void HandleOutOfMemory();

    static void HandleStackOverflow(StackOverflowDetector detector, void * pLimitFrame);

    static void HandleSoftStackOverflow(BOOL fSkipDebugger = FALSE);

    static void HandleStackOverflowAfterCatch();

    static void HandleExitProcess(ShutdownCompleteAction sca = SCA_ExitProcessWhenShutdownComplete);

    static int NOINLINE HandleFatalError(UINT exitCode, UINT_PTR address, LPCWSTR pMessage=NULL, PEXCEPTION_POINTERS pExceptionInfo= NULL, LPCWSTR errorSource=NULL, LPCWSTR argExceptionString=NULL);

    static void DECLSPEC_NORETURN HandleFatalStackOverflow(EXCEPTION_POINTERS *pException, BOOL fSkipDebugger = FALSE);

    static void HandleExitProcessFromEscalation(EPolicyAction action, UINT exitCode);

    static void HandleCodeContractFailure(LPCWSTR pMessage, LPCWSTR pCondition, LPCWSTR pInnerExceptionAsString);

private:
    DWORD m_Timeout[MaxClrOperation];
    EPolicyAction m_ActionOnTimeout[MaxClrOperation];
    EPolicyAction m_DefaultAction[MaxClrOperation];
    EPolicyAction m_ActionOnFailure[MaxClrFailure];
    EClrUnhandledException m_unhandledExceptionPolicy;
    
    // TODO: Support multiple methods to set policy: hosting, config, managed api.

    // Return BOOL if action is acceptable for operation.
    BOOL IsValidActionForOperation(EClrOperation operation, EPolicyAction action);
    BOOL IsValidActionForTimeout(EClrOperation operation, EPolicyAction action);
    BOOL IsValidActionForFailure(EClrFailure failure, EPolicyAction action);
    EPolicyAction GetFinalAction(EPolicyAction action, Thread *pThread);

    static void LogFatalError(UINT exitCode, UINT_PTR address, LPCWSTR pMessage, PEXCEPTION_POINTERS pExceptionInfo, LPCWSTR errorSource, LPCWSTR argExceptionString=NULL);

    // IMPORTANT NOTE: only the following two functions should be calling ExitProcessViaShim.
    // - CorHost2::ExitProcess
    // - SafeExitProcess
    friend class CorHost2;
    friend void SafeExitProcess(UINT , BOOL , ShutdownCompleteAction);

    static void ExitProcessViaShim(UINT exitCode);
};

void InitEEPolicy();

extern BYTE g_EEPolicyInstance[];

inline EEPolicy* GetEEPolicy()
{
    return (EEPolicy*)&g_EEPolicyInstance;
}

//
// Use EEPOLICY_HANDLE_FATAL_ERROR when you have a situtation where the Runtime's internal state would be
// inconsistent if execution were allowed to continue. This will apply the proper host's policy for fatal
// errors. Note: this call will never return.
//
// NOTE: make sure to use the macro instead of claling EEPolicy::HandleFatalError directly. The macro grabs the IP
// of where you are calling this from, so we can log it to help when debugging these failures later.
//

// FailFast with specific error code
#define EEPOLICY_HANDLE_FATAL_ERROR(_exitcode) EEPolicy::HandleFatalError(_exitcode, GetCurrentIP());

// FailFast with specific error code and message (LPWSTR)
#define EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(_exitcode, _message) EEPolicy::HandleFatalError(_exitcode, GetCurrentIP(), _message);

// FailFast with specific error code and exception details
#define EEPOLICY_HANDLE_FATAL_ERROR_USING_EXCEPTION_INFO(_exitcode, _pExceptionInfo) EEPolicy::HandleFatalError(_exitcode, GetCurrentIP(), NULL, _pExceptionInfo);

#endif  // EEPOLICY_H_
