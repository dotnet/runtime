// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Native helper library for the FatalErrorHandler test.
// Validates that the public FatalErrorHandling.h header is usable
// from a third-party C++ library.

#include <stdio.h>
#include <stdlib.h>
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

// Write a 32-bit value to stderr as 0xXXXXXXXX without touching the CRT's buffered
// I/O, so it is safe to call from a signal handler context.
static void WriteStdErrHex(uint32_t value)
{
    static const char digits[] = "0123456789ABCDEF";
    char buf[11];
    buf[0] = '0';
    buf[1] = 'x';
    for (int i = 0; i < 8; i++)
        buf[2 + i] = digits[(value >> ((7 - i) * 4)) & 0xF];
    buf[10] = '\0';

    WriteStdErr(buf);
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

// Handler for the PAL fatal-signal path (invoke_previous_action) on Unix. A genuine
// native crash (for example an access violation whose faulting instruction pointer is
// native code, or a native abort()) does not flow through the managed fatal path, so it
// reaches the handler through the signal path instead. Reports the Win32 exception code
// the handler received (the signal converted via CONTEXTGetExceptionCodeForSignal, passed
// as the first argument on this path) and whether the POSIX crash context (siginfo_t /
// ucontext_t / previous struct sigaction) was surfaced.
static int DOTNET_CALLCONV HandlerCheckSignalInfo(int faultCode, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    WriteStdErr("FATAL_FAULTCODE:");
    WriteStdErrHex(static_cast<uint32_t>(faultCode));
    WriteStdErr("\n");

    const void* pSigInfo = NULL;
    bool sigInfoPopulated = getProperty(FEP_PosixSigInfo, &pSigInfo) != 0 && pSigInfo != NULL;
    WriteStdErr(sigInfoPopulated ? "FATAL_SIGINFO:siginfo=true\n" : "FATAL_SIGINFO:siginfo=false\n");

    const void* pContext = NULL;
    bool contextPopulated = getProperty(FEP_UContext, &pContext) != 0 && pContext != NULL;
    WriteStdErr(contextPopulated ? "FATAL_UCONTEXT:ucontext=true\n" : "FATAL_UCONTEXT:ucontext=false\n");

    const void* pPrevAction = NULL;
    bool prevActionPopulated = getProperty(FEP_PosixPreviousAction, &pPrevAction) != 0 && pPrevAction != NULL;
    WriteStdErr(prevActionPopulated ? "FATAL_PREVACTION:prevaction=true\n" : "FATAL_PREVACTION:prevaction=false\n");

    // Skip the runtime's default crash handling. The PAL restores the previous signal
    // action and returns, so the fault re-executes and terminates through the previous
    // handler (the OS's natural fatal mechanism) instead of the runtime dumping / chaining.
    return SkipDefaultHandler;
}

// Sink that receives on-demand diagnostic data fragments and counts the bytes it saw.
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

// Sink that immediately aborts by returning false on the first fragment, exercising the
// documented DiagnosticDataOutputFunc abort contract (the report generation must stop and
// the entry point must return false).
static bool DOTNET_CALLCONV AbortingSink(const char* /*data*/, size_t /*length*/, void* /*userContext*/)
{
    return false;
}

// Produces one on-demand report of the requested type and reports whether the runtime
// streamed any data back through the sink. Returns true when the report succeeded.
static bool ProduceReport(DiagnosticDataFunc pfnDiag, DiagnosticDataType type)
{
    DiagnosticSinkState state = {};
    DiagnosticDataConfig config = {};
    config.type = type;
    config.size = static_cast<uint32_t>(sizeof(config));
    config.pfnOutput = DiagnosticSink;
    config.userContext = &state;
    return pfnDiag(&config) && state.bytes > 0;
}

// Verifies the entry point rejects the malformed requests enumerated in the header
// contract (NULL config, undersized config, NULL sink, unrecognized type) and honors a
// sink that aborts. Returns true only when every such request returns false.
static bool RejectsBadRequests(DiagnosticDataFunc pfnDiag)
{
    if (pfnDiag(NULL))
        return false;

    DiagnosticDataConfig undersized = {};
    undersized.type = JsonCrashReport;
    undersized.size = static_cast<uint32_t>(sizeof(DiagnosticDataConfig)) - 1;
    undersized.pfnOutput = DiagnosticSink;
    if (pfnDiag(&undersized))
        return false;

    DiagnosticDataConfig noSink = {};
    noSink.type = JsonCrashReport;
    noSink.size = static_cast<uint32_t>(sizeof(noSink));
    noSink.pfnOutput = NULL;
    if (pfnDiag(&noSink))
        return false;

    DiagnosticDataConfig badType = {};
    badType.type = 0x7FFFFFFF;
    badType.size = static_cast<uint32_t>(sizeof(badType));
    badType.pfnOutput = DiagnosticSink;
    if (pfnDiag(&badType))
        return false;

    DiagnosticDataConfig aborts = {};
    aborts.type = JsonCrashReport;
    aborts.size = static_cast<uint32_t>(sizeof(aborts));
    aborts.pfnOutput = AbortingSink;
    if (pfnDiag(&aborts))
        return false;

    return true;
}

// Handler that requests the on-demand diagnostic-data entry point and generates both a
// JSON and a Log crash report. Where the in-proc crash reporter is not compiled (for
// example CoreCLR on Windows, or NativeAOT), FEP_DiagnosticDataFunc is reported as
// unavailable; the handler reports that so the managed test can decide whether the
// platform was expected to support it.
static int DOTNET_CALLCONV HandlerCheckDiagnosticData(int /*hresult*/, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    const void* pFunc = NULL;
    if (getProperty(FEP_DiagnosticDataFunc, &pFunc) == 0 || pFunc == NULL)
    {
        WriteStdErr("FATAL_DIAG:unsupported\n");
        return SkipDefaultHandler;
    }

    DiagnosticDataFunc pfnDiag = reinterpret_cast<DiagnosticDataFunc>(reinterpret_cast<uintptr_t>(pFunc));

    WriteStdErr(RejectsBadRequests(pfnDiag) ? "FATAL_DIAG_NEG:ok\n" : "FATAL_DIAG_NEG:fail\n");
    WriteStdErr(ProduceReport(pfnDiag, JsonCrashReport) ? "FATAL_DIAG_JSON:ok\n" : "FATAL_DIAG_JSON:fail\n");
    WriteStdErr(ProduceReport(pfnDiag, LogCrashReport) ? "FATAL_DIAG_LOG:ok\n" : "FATAL_DIAG_LOG:fail\n");

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

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerCheckDiagnosticData()
{
    return HandlerCheckDiagnosticData;
}

extern "C" DLL_EXPORT FatalErrorHandler GetHandlerCheckSignalInfo()
{
    return HandlerCheckSignalInfo;
}

// Triggers an access violation from native code - a genuinely-unmanaged fatal fault whose
// faulting instruction pointer is not managed code, so the runtime does not translate it
// into a managed exception. Reaches the runtime's unmanaged fatal chokepoint directly.
extern "C" DLL_EXPORT void TriggerNativeAccessViolation()
{
    volatile int* p = NULL;
    *p = 0;
}

// Triggers a native abort() - raises SIGABRT, which the PAL delivers to its signal
// handler and, on Unix, reaches invoke_previous_action without flowing through the
// managed fatal path. Used to validate that the fatal error handler is invoked from the
// fatal-signal path for SIGABRT.
extern "C" DLL_EXPORT void TriggerNativeAbort()
{
    abort();
}
