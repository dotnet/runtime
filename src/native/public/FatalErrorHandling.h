// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header defines the native types used by the
// ExceptionHandling.SetFatalErrorHandler API. A native fatal error handler
// receives an HRESULT and a property-getter callback through which it can
// request additional crash information on demand.

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

// Function pointer retrieved through the property getter as the value of
// FEP_FatalErrorLogFunc. When called, it invokes pfnLogAction one or more
// times with UTF-8 encoded crash log fragments. The combined output contains
// the same information the runtime would print to standard error during its
// default fatal error handling.
//
// The handler may call this function at most once. Calling it after the
// handler returns produces undefined behavior.
typedef void (DOTNET_CALLCONV *FatalErrorLogFunc)(FatalErrorLogAction pfnLogAction, void* userContext);

// Properties that a fatal error handler can request through the property
// getter passed to it. Each property has a documented value shape. The getter
// writes the value through its out parameter. New properties may be added over
// time, so handlers must tolerate the getter reporting a property as
// unavailable.
enum FatalErrorProperty : int32_t
{
    // Value: FatalErrorLogFunc. Entry point for retrieving the crash log.
    FEP_FatalErrorLogFunc = 0x1,

    // Value: void*. Code location correlated with the failure (for example,
    // the address where FailFast was called). May be unavailable.
    FEP_Address,

    // Value: PEXCEPTION_RECORD. Windows exception record for the failure.
    FEP_WindowsExceptionRecord,

    // Value: PCONTEXT. Windows thread context at the point of failure.
    FEP_WindowsContextRecord,

    // Value: ucontext_t*. Thread context on signal-based Unix platforms.
    FEP_UContext,

    // Value: siginfo_t*. Signal information on signal-based Unix platforms.
    FEP_PosixSigInfo,

    // Value: Mach thread state for the current architecture
    // (arm_thread_state64_t* on arm64, x86_thread_state64_t* on x64).
    FEP_MachExceptionInfo,
};

// Property-getter callback passed to the fatal error handler. The handler
// calls it with a FatalErrorProperty value and a pointer that receives the
// property's value. The retrieved value is a pointer to read-only crash state
// owned by the runtime. The handler must not modify the pointed-to data.
// Returns a nonzero value if the property is available (and *value has been
// written), or 0 if the property is not available.
typedef int32_t (DOTNET_CALLCONV *FatalErrorPropertyGetter)(FatalErrorProperty prop, const void** value);

#endif // FATAL_ERROR_HANDLING_H
