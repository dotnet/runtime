// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

    static void HandleStackOverflow();

    static void HandleExitProcess(ShutdownCompleteAction sca = SCA_ExitProcessWhenShutdownComplete);

    static int NOINLINE HandleFatalError(UINT exitCode, UINT_PTR address, LPCWSTR pMessage=NULL, PEXCEPTION_POINTERS pExceptionInfo= NULL, LPCWSTR errorSource=NULL, LPCWSTR argExceptionString=NULL);

    static void DECLSPEC_NORETURN HandleFatalStackOverflow(EXCEPTION_POINTERS *pException, BOOL fSkipDebugger = FALSE);

private:
    static void LogFatalError(UINT exitCode, UINT_PTR address, LPCWSTR pMessage, PEXCEPTION_POINTERS pExceptionInfo, LPCWSTR errorSource, LPCWSTR argExceptionString=NULL);
};

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
