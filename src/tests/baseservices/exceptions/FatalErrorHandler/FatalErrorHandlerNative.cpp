// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Native helper library for the FatalErrorHandler test.
// Validates that the public fatal_error_handling.h header is usable
// from a third-party C++ library.

#include <stdio.h>
#include <stdlib.h>
#include <signal.h>
#include <string.h>
#include <platformdefines.h>

#include <fatal_error_handling.h> // Public API for fatal error handling

#include <thread>

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

static void WriteStdErrHex(uint32_t value)
{
    static const char digits[] = "0123456789ABCDEF";
    char buffer[11];
    buffer[0] = '0';
    buffer[1] = 'x';
    for (int i = 0; i < 8; i++)
        buffer[2 + i] = digits[(value >> ((7 - i) * 4)) & 0xF];
    buffer[10] = '\0';

    WriteStdErr(buffer);
}

// Handler that allows the default fatal error handling to proceed.
static int DOTNET_CALLCONV HandlerRunDefault(int /*hresult*/, FatalErrorPropertyGetter /*getProperty*/)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");
    return RunDefaultHandler;
}

// Handler that retrieves the crash log before default handling.
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

    return RunDefaultHandler;
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

    return RunDefaultHandler;
}

// Handler that reports whether the live platform-native fault structures were surfaced
// for a genuinely unmanaged fatal exception (a fault whose instruction pointer is inside
// native code).
static int DOTNET_CALLCONV HandlerCheckNativeInfo(int /*hresult*/, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    const void* pLogFunc = NULL;
    bool logFuncPopulated = getProperty(FEP_FatalErrorLogFunc, &pLogFunc) != 0 && pLogFunc != NULL;
    WriteStdErr(logFuncPopulated ? "FATAL_LOGFUNC:logfunc=true\n" : "FATAL_LOGFUNC:logfunc=false\n");

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
    if (sigInfoPopulated)
    {
        WriteStdErr("FATAL_SIGNO:");
        WriteStdErrHex(static_cast<uint32_t>(reinterpret_cast<const siginfo_t*>(pSigInfo)->si_signo));
        WriteStdErr("\n");
    }

    const void* pContext = NULL;
    bool contextPopulated = getProperty(FEP_UContext, &pContext) != 0 && pContext != NULL;
    WriteStdErr(contextPopulated ? "FATAL_UCONTEXT:ucontext=true\n" : "FATAL_UCONTEXT:ucontext=false\n");

#ifdef __APPLE__
    const void* pMachExceptionInfo = NULL;
    bool machExceptionInfoPopulated = getProperty(FEP_MachExceptionInfo, &pMachExceptionInfo) != 0 && pMachExceptionInfo != NULL;
    WriteStdErr(machExceptionInfoPopulated ? "FATAL_MACHINFO:machinfo=true\n" : "FATAL_MACHINFO:machinfo=false\n");
#endif // __APPLE__
#endif

    return RunDefaultHandler;
}

struct DiagnosticSinkState
{
    size_t bytes;
};

static bool DOTNET_CALLCONV DiagnosticSink(const char* data, size_t length, void* userContext)
{
    DiagnosticSinkState* state = reinterpret_cast<DiagnosticSinkState*>(userContext);
    if (state != NULL && data != NULL)
        state->bytes += length;
    return true;
}

static bool DOTNET_CALLCONV AbortingSink(const char* /*data*/, size_t /*length*/, void* /*userContext*/)
{
    return false;
}

static bool ProduceReport(DiagnosticDataFunc pfnDiagnosticData, DiagnosticDataType type, int signal, const void* context)
{
    DiagnosticSinkState state = {};

    if (type == JsonInProcCrashReport)
    {
        JsonInProcCrashReportConfig config = {};
        config.base.type = type;
        config.base.size = static_cast<uint32_t>(sizeof(config));
        config.base.pfnOutput = DiagnosticSink;
        config.base.userContext = &state;
        config.signal = signal;
        config.context = context;
        return pfnDiagnosticData(&config.base) == DiagnosticDataSuccess && state.bytes > 0;
    }

    LogInProcCrashReportConfig config = {};
    config.base.type = type;
    config.base.size = static_cast<uint32_t>(sizeof(config));
    config.base.pfnOutput = DiagnosticSink;
    config.base.userContext = &state;
    config.signal = signal;
    config.context = context;
    return pfnDiagnosticData(&config.base) == DiagnosticDataSuccess && state.bytes > 0;
}

static bool ValidateDiagnosticDataResults(DiagnosticDataFunc pfnDiagnosticData)
{
    if (pfnDiagnosticData(NULL) != DiagnosticDataInvalidArgument)
        return false;

    JsonInProcCrashReportConfig undersized = {};
    undersized.base.type = JsonInProcCrashReport;
    undersized.base.size = static_cast<uint32_t>(sizeof(undersized)) - 1;
    undersized.base.pfnOutput = DiagnosticSink;
    if (pfnDiagnosticData(&undersized.base) != DiagnosticDataInvalidArgument)
        return false;

    DiagnosticDataConfig noSink = {};
    noSink.type = JsonInProcCrashReport;
    noSink.size = static_cast<uint32_t>(sizeof(noSink));
    if (pfnDiagnosticData(&noSink) != DiagnosticDataInvalidArgument)
        return false;

    DiagnosticDataConfig unsupported = {};
    unsupported.type = 0x7FFFFFFF;
    unsupported.size = static_cast<uint32_t>(sizeof(unsupported));
    unsupported.pfnOutput = DiagnosticSink;
    if (pfnDiagnosticData(&unsupported) != DiagnosticDataUnsupported)
        return false;

    JsonInProcCrashReportConfig aborting = {};
    aborting.base.type = JsonInProcCrashReport;
    aborting.base.size = static_cast<uint32_t>(sizeof(aborting));
    aborting.base.pfnOutput = AbortingSink;
    aborting.signal = SIGABRT;
    if (pfnDiagnosticData(&aborting.base) != DiagnosticDataFailure)
        return false;

    return true;
}

static int DOTNET_CALLCONV HandlerCheckDiagnosticData(int /*hresult*/, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    const void* pFunction = NULL;
    if (getProperty(FEP_DiagnosticDataFunc, &pFunction) == 0 || pFunction == NULL)
    {
        WriteStdErr("FATAL_DIAG:unsupported\n");
        return RunDefaultHandler;
    }

    DiagnosticDataFunc pfnDiagnosticData =
        reinterpret_cast<DiagnosticDataFunc>(reinterpret_cast<uintptr_t>(pFunction));

    int signal = SIGABRT;
    const void* context = NULL;
#ifndef _WIN32
    const void* pSigInfo = NULL;
    if (getProperty(FEP_PosixSigInfo, &pSigInfo) != 0 && pSigInfo != NULL)
        signal = reinterpret_cast<const siginfo_t*>(pSigInfo)->si_signo;

    (void)getProperty(FEP_UContext, &context);
#endif

    WriteStdErr(ValidateDiagnosticDataResults(pfnDiagnosticData) ? "FATAL_DIAG_RESULTS:ok\n" : "FATAL_DIAG_RESULTS:fail\n");
    WriteStdErr(ProduceReport(pfnDiagnosticData, JsonInProcCrashReport, signal, context) ? "FATAL_DIAG_JSON:ok\n" : "FATAL_DIAG_JSON:fail\n");
    WriteStdErr(ProduceReport(pfnDiagnosticData, LogInProcCrashReport, signal, context) ? "FATAL_DIAG_LOG:ok\n" : "FATAL_DIAG_LOG:fail\n");

    return RunDefaultHandler;
}

// Exported accessors — managed code P/Invokes these to get native function pointers.
using FatalErrorHandler = int (DOTNET_CALLCONV *)(int hresult, FatalErrorPropertyGetter getProperty);

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

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerCheckDiagnosticData()
{
    return HandlerCheckDiagnosticData;
}

// Triggers an access violation from native code — a genuinely-unmanaged fatal fault whose
// faulting instruction pointer is not managed code, so the runtime does not translate it
// into a managed exception. Reaches the runtime's unmanaged fatal chokepoint directly.
extern "C" DLL_EXPORT void TriggerNativeAccessViolation()
{
    volatile int* p = NULL;
    *p = 0;
}

// Triggers a native access violation on a raw OS thread that has no managed
// Thread object, matching the thread model used by CoreCLR server GC workers.
extern "C" DLL_EXPORT void TriggerNativeAccessViolationOnNewThread()
{
    std::thread(TriggerNativeAccessViolation).join();

    // The process should have terminated from the access violation.
    abort();
}

extern "C" DLL_EXPORT void TriggerNativeAbort()
{
    abort();
}
