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
static int DOTNET_CALLCONV HandlerSkipDefault(int hresult, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");
    return SkipDefaultHandler;
}

// Handler that allows the default fatal error handling to proceed.
static int DOTNET_CALLCONV HandlerRunDefault(int hresult, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");
    return RunDefaultHandler;
}

// Handler that retrieves the crash log before skipping the default handling.
static void DOTNET_CALLCONV LogCallback(const char* logString, void* userContext)
{
    WriteStdErr("FATAL_LOG_RECEIVED:");
    if (logString != NULL)
        WriteStdErr(logString);
    WriteStdErr("\n");
}

static int DOTNET_CALLCONV HandlerWithLog(int hresult, FatalErrorPropertyGetter getProperty)
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

// Handler that reports which native exception properties were populated. Used to
// verify that a native exception (for example, an access violation) supplies
// the platform-specific siginfo/exception-record and thread context pointers.
static int DOTNET_CALLCONV HandlerCheckInfo(int hresult, FatalErrorPropertyGetter getProperty)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    const void* pValue = NULL;

    // "info" corresponds to the platform's exception/signal record. On Apple platforms
    // CoreCLR uses Mach exceptions (so no signal record is provided), whereas NativeAOT
    // uses POSIX signals (so a siginfo/ucontext is provided). Query both context shapes.
#ifdef _WIN32
    bool infoPopulated = getProperty(FEP_WindowsExceptionRecord, &pValue) != 0 && pValue != NULL;
    pValue = NULL;
    bool contextPopulated = getProperty(FEP_WindowsContextRecord, &pValue) != 0 && pValue != NULL;
#elif defined(__APPLE__)
    bool infoPopulated = getProperty(FEP_PosixSigInfo, &pValue) != 0 && pValue != NULL;
    pValue = NULL;
    bool contextPopulated = getProperty(FEP_MachExceptionInfo, &pValue) != 0 && pValue != NULL;
    if (!contextPopulated)
    {
        pValue = NULL;
        contextPopulated = getProperty(FEP_UContext, &pValue) != 0 && pValue != NULL;
    }
#else
    bool infoPopulated = getProperty(FEP_PosixSigInfo, &pValue) != 0 && pValue != NULL;
    pValue = NULL;
    bool contextPopulated = getProperty(FEP_UContext, &pValue) != 0 && pValue != NULL;
#endif

    WriteStdErr("FATAL_INFO:");
    WriteStdErr(infoPopulated ? "info=true," : "info=false,");
    WriteStdErr(contextPopulated ? "context=true\n" : "context=false\n");

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
