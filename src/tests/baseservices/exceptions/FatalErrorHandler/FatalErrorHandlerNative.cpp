// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Native helper library for the FatalErrorHandler test.
// Validates that the public FatalErrorHandling.h header is usable
// from a third-party C++ library.

#include <stdio.h>
#include <string.h>
#include <platformdefines.h>

#include <FatalErrorHandling.h> // Public API for fatal error handling

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#endif

// Write raw bytes to stderr without any managed runtime involvement.
static void WriteStdErr(const char* msg)
{
#ifdef _WIN32
    HANDLE hStdErr = GetStdHandle(STD_ERROR_HANDLE);
    DWORD written;
    WriteFile(hStdErr, msg, (DWORD)strlen(msg), &written, NULL);
#else
    ssize_t unused = write(STDERR_FILENO, msg, strlen(msg));
    (void)unused;
#endif // _WIN32
}

// Handler that skips the default fatal error handling.
static int DOTNET_CALLCONV HandlerSkipDefault(int /*hresult*/, FatalErrorPropertyGetter /*getProperty*/)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");
    return SkipDefaultHandler;
}

// Handler that allows the default fatal error handling to proceed.
static int DOTNET_CALLCONV HandlerRunDefault(int /*hresult*/, FatalErrorPropertyGetter /*getProperty*/)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");
    return RunDefaultHandler;
}

// Handler that retrieves the crash log before skipping the default handling.
static void DOTNET_CALLCONV LogCallback(const char* logString, void* /*userContext*/)
{
    WriteStdErr("FATAL_LOG_RECEIVED:");
    if (logString != NULL)
        WriteStdErr(logString);
    WriteStdErr("\n");
}

static int DOTNET_CALLCONV HandlerWithLog(int /*hresult*/, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    const void* pLogFunc = NULL;
    if (getProperty(FEP_FatalErrorLogFunc, &pLogFunc) != 0 && pLogFunc != NULL)
    {
        FatalErrorLogFunc pfnGetFatalErrorLog = reinterpret_cast<FatalErrorLogFunc>(reinterpret_cast<uintptr_t>(pLogFunc));
        pfnGetFatalErrorLog(LogCallback, NULL);
    }

    return SkipDefaultHandler;
}

// Handler that reports whether the crash address (faulting instruction pointer)
// was surfaced. The managed fatal path provides only the IP; the platform-native
// signal/exception records are not surfaced for faults that flow through managed
// code.
static int DOTNET_CALLCONV HandlerCheckInfo(int /*hresult*/, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    const void* pValue = NULL;
    bool addressPopulated = getProperty(FEP_Address, &pValue) != 0 && pValue != NULL;

    WriteStdErr("FATAL_ADDRESS:");
    WriteStdErr(addressPopulated ? "addr=true\n" : "addr=false\n");

    return SkipDefaultHandler;
}

// Handler that reports whether the live platform-native fault structures were surfaced
// for a genuinely unmanaged fatal exception (a fault whose instruction pointer is inside
// native code).
static int DOTNET_CALLCONV HandlerCheckNativeInfo(int /*hresult*/, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    const void* pAddress = NULL;
    bool addressPopulated = getProperty(FEP_Address, &pAddress) != 0 && pAddress != NULL;
    WriteStdErr(addressPopulated ? "FATAL_ADDRESS:addr=true\n" : "FATAL_ADDRESS:addr=false\n");

#ifdef _WIN32
    const void* pExceptionRecord = NULL;
    bool exceptionRecordPopulated = getProperty(FEP_WindowsExceptionRecord, &pExceptionRecord) != 0 && pExceptionRecord != NULL;
    WriteStdErr(exceptionRecordPopulated ? "FATAL_EXCEPTIONRECORD:excrec=true\n" : "FATAL_EXCEPTIONRECORD:excrec=false\n");

    const void* pContextRecord = NULL;
    bool contextRecordPopulated = getProperty(FEP_WindowsContextRecord, &pContextRecord) != 0 && pContextRecord != NULL;
    WriteStdErr(contextRecordPopulated ? "FATAL_CONTEXTRECORD:ctxrec=true\n" : "FATAL_CONTEXTRECORD:ctxrec=false\n");
#else
    const void* pSigInfo = NULL;
    bool sigInfoPopulated = getProperty(FEP_PosixSigInfo, &pSigInfo) != 0 && pSigInfo != NULL;
    WriteStdErr(sigInfoPopulated ? "FATAL_SIGINFO:siginfo=true\n" : "FATAL_SIGINFO:siginfo=false\n");

    const void* pContext = NULL;
    bool contextPopulated = getProperty(FEP_UContext, &pContext) != 0 && pContext != NULL;
    WriteStdErr(contextPopulated ? "FATAL_UCONTEXT:ucontext=true\n" : "FATAL_UCONTEXT:ucontext=false\n");
#endif

    return SkipDefaultHandler;
}

// Exported accessors — managed code P/Invokes these to get native function pointers.
using FatalErrorHandler = int (DOTNET_CALLCONV *)(int hresult, FatalErrorPropertyGetter getProperty);

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerSkipDefault()
{
    return HandlerSkipDefault;
}

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerRunDefault()
{
    return HandlerRunDefault;
}

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerWithLog()
{
    return HandlerWithLog;
}

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerCheckInfo()
{
    return HandlerCheckInfo;
}

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerCheckNativeInfo()
{
    return HandlerCheckNativeInfo;
}

// Triggers an access violation from native code — a genuinely-unmanaged fatal fault whose
// faulting instruction pointer is not managed code, so the runtime does not translate it
// into a managed exception. Reaches the runtime's unmanaged fatal chokepoint directly.
extern "C" DLL_EXPORT void TriggerNativeAccessViolation()
{
    volatile int* p = NULL;
    *p = 0;
}
