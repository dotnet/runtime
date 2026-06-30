// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header defines the native structures and types used by the
// ExceptionHandling.SetFatalErrorHandler API. A native fatal error handler
// receives a pointer to FatalErrorInfo and can query additional crash
// information through the callback stored in that structure.

#ifndef FATAL_ERROR_HANDLING_H
#define FATAL_ERROR_HANDLING_H

#include <stdint.h>
#include <stddef.h>

#if defined(_MSC_VER) && defined(_M_IX86)
#define DOTNET_CALLCONV __stdcall
#else
#define DOTNET_CALLCONV
#endif

enum FatalErrorHandlerResult : int32_t
{
    // Allow the runtime to continue with its default fatal error handling
    // (printing crash information, generating a crash dump, etc.).
    RunDefaultHandler = 0,

    // Suppress the runtime's default fatal error handling. The process will
    // still be terminated, but the runtime will not print crash information
    // or generate a crash dump.
    SkipDefaultHandler = 1,
};

// Callback signature for receiving crash log text. The runtime may invoke
// pfnLogAction multiple times, each time passing a UTF-8 encoded fragment
// of the crash log.
typedef void (DOTNET_CALLCONV *FatalErrorLogAction)(const char* logString, void* userContext);

// Callback signature for requesting the crash log. The handler calls this
// function pointer (stored in FatalErrorInfo) to receive the crash log text
// via pfnLogAction.
typedef void (DOTNET_CALLCONV *FatalErrorLogFunc)(
    struct FatalErrorInfo* errorData,
    FatalErrorLogAction pfnLogAction,
    void* userContext);

struct FatalErrorInfo
{
    // Size of this structure in bytes. Consumers should check this field to
    // determine which subsequent fields are available, enabling forward
    // compatibility when new fields are added.
    size_t size;

    // Code location correlated with the failure (for example, the address
    // where FailFast was called). May be NULL if not available.
    void* address;

    // Platform-specific exception/signal information, if available.
    // On Windows, cast to PEXCEPTION_RECORD.
    // On Linux and other signal-based Unix platforms, cast to siginfo_t*.
    // On Apple platforms (macOS, iOS, ...), this is always NULL because the
    // runtime handles hardware faults through Mach exceptions rather than POSIX
    // signals; the faulting code location is reported through the address field.
    // May be NULL.
    void* info;

    // Platform-specific thread context at the point of failure, if available.
    // On Windows, cast to PCONTEXT.
    // On Linux and other signal-based Unix platforms, cast to ucontext_t*.
    // On Apple platforms (macOS, iOS, ...), cast to the Mach thread state for
    // the current architecture: arm_thread_state64_t* on arm64 or
    // x86_thread_state64_t* on x64.
    // May be NULL.
    void* context;

    // Entry point for retrieving the crash log. The runtime populates this
    // field with a function that, when called, invokes pfnLogAction one or
    // more times with UTF-8 encoded crash log fragments. The combined output
    // contains the same information the runtime would print to standard error
    // during its default fatal error handling.
    //
    // The handler may call this function at most once. Calling it after the
    // handler returns produces undefined behavior.
    FatalErrorLogFunc pfnGetFatalErrorLog;
};

#endif // FATAL_ERROR_HANDLING_H
