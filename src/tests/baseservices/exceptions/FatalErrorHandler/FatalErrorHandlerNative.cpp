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
static int DOTNET_CALLCONV HandlerSkipDefault(int hresult, void* pErrorInfo)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");
    return SkipDefaultHandler;
}

// Handler that allows the default fatal error handling to proceed.
static int DOTNET_CALLCONV HandlerRunDefault(int hresult, void* pErrorInfo)
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

static int DOTNET_CALLCONV HandlerWithLog(int hresult, void* pErrorInfo)
{
    WriteStdErr("FATAL_HANDLER_INVOKED\n");

    FatalErrorInfo* info = static_cast<FatalErrorInfo*>(pErrorInfo);
    if (info->pfnGetFatalErrorLog != NULL)
    {
        info->pfnGetFatalErrorLog(info, LogCallback, NULL);
    }

    return SkipDefaultHandler;
}

// Exported accessors — managed code P/Invokes these to get native function pointers.
using FatalErrorHandler = int (DOTNET_CALLCONV *)(int hresult, void* errorData);

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
