// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header defines the native types used by the
// ExceptionHandling.SetFatalErrorHandler API. A native fatal error handler
// receives an HRESULT and a property-getter callback through which it can
// request additional crash information on demand.
//
// The HRESULT reflects the runtime's own classification of the failure on the
// managed fatal-error path (for example COR_E_STACKOVERFLOW, COR_E_FAILFAST).
// When the handler is instead invoked from a raw POSIX signal (a hardware
// fault such as SIGSEGV/SIGILL/SIGFPE/SIGBUS, or SIGABRT) there is no managed
// exit code to report, so the HRESULT is always E_UNEXPECTED; a handler that
// needs to know the actual signal should inspect FEP_PosixSigInfo
// (siginfo_t::si_signo) instead of relying on the HRESULT value.

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
// of the crash log. The logString pointer is valid only for the duration of
// the callback; the handler must copy the text if it needs to retain it after
// the callback returns.
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
    FEP_Address = 0x2,

    // Value: PEXCEPTION_RECORD. Windows exception record for the failure.
    FEP_WindowsExceptionRecord = 0x3,

    // Value: PCONTEXT. Windows thread context at the point of failure.
    FEP_WindowsContextRecord = 0x4,

    // Value: ucontext_t*. Thread context on signal-based Unix platforms.
    FEP_UContext = 0x5,

    // Value: siginfo_t*. Signal information on signal-based Unix platforms.
    FEP_PosixSigInfo = 0x6,

    // Value: Mach thread state for the current architecture
    // (arm_thread_state64_t* on arm64, x86_thread_state64_t* on x64).
    FEP_MachExceptionInfo = 0x7,

    // Value: struct sigaction*. The previous signal handler action for the
    // failing signal on signal-based Unix platforms, captured before the
    // runtime chains to it. A handler can use this to replicate the runtime's
    // default signal chaining/restoration itself. May be unavailable.
    FEP_PosixPreviousAction = 0x8,

    // Value: DiagnosticDataFunc. Entry point for producing diagnostic data
    // on demand, streamed back to the handler through a caller-provided sink.
    FEP_DiagnosticDataFunc = 0x9,
};

// Property-getter callback passed to the fatal error handler. The handler
// calls it with a FatalErrorProperty value and a pointer that receives the
// property's value. The retrieved value is a pointer to read-only crash state
// owned by the runtime or callbacks. The handler must not modify pointed-to data.
// Any returned pointer is valid only until the fatal error handler returns and
// must not be cached.
// Returns a nonzero value if the property is available (and *value has been
// written), or 0 if the property is not available.
typedef int32_t (DOTNET_CALLCONV *FatalErrorPropertyGetter)(FatalErrorProperty prop, const void** value);

// Types of diagnostic data that can be produced on demand through the
// DiagnosticDataFunc entry point. Each type has an associated configuration
// struct (see below) whose first member is a DiagnosticDataConfig.
enum DiagnosticDataType : int32_t
{
    // Crash report serialized as JSON. Streamed as length-delimited UTF-8
    // fragments that are NOT NUL-terminated; use the length passed to the sink.
    JsonCrashReport = 0,

    // Crash report serialized as human-readable text. Streamed as UTF-8
    // fragments that ARE NUL-terminated (each fragment is a valid C string);
    // the length passed to the sink excludes the terminator.
    LogCrashReport = 1,
};

// Callback that receives diagnostic data. The runtime invokes it one or more
// times, each time passing a fragment of the requested data. 'data' points to
// 'length' bytes and is valid only for the duration of the call; the callback
// must copy anything it needs to retain. For text types the data is UTF-8; for
// binary types (for example memory/core dumps) it is raw bytes. Returns true to
// continue, or false to abort.
typedef bool (DOTNET_CALLCONV *DiagnosticDataOutputFunc)(const char* data, size_t length, void* userContext);

// Configuration shared by every diagnostic data type. Concrete diagnostic data
// types that need no additional fields alias this type directly (see below);
// a future type that requires extra fields would embed this as its first member
// so a pointer to it can still be treated as a pointer to DiagnosticDataConfig
// and, after validating 'type', cast back to the concrete type.
//
//   type        - the DiagnosticDataType being requested; selects the concrete
//                 configuration struct.
//   size        - sizeof the concrete configuration struct.
//   pfnOutput   - sink that receives the produced data (must not be NULL).
//   userContext - opaque value forwarded to pfnOutput (can be NULL).
//   signal      - POSIX signal number describing the fault the report should
//                 reflect (for example, the value passed to the fatal error
//                 handler, or retrieved via FEP_PosixSigInfo). Pass 0 to let
//                 the runtime substitute its default.
//   context     - platform crash context describing the faulting thread (for
//                 example the value retrieved via FEP_UContext). Pass NULL to
//                 let the runtime substitute its default (no context).
struct DiagnosticDataConfig
{
    int32_t                  type;
    uint32_t                 size;
    DiagnosticDataOutputFunc pfnOutput;
    void*                    userContext;
    int32_t                  signal;
    void*                    context;
};

// Configuration for DiagnosticDataType::JsonCrashReport. Adds no fields beyond
// the shared configuration, so it is an alias for DiagnosticDataConfig.
typedef DiagnosticDataConfig JsonCrashReportConfig;

// Configuration for DiagnosticDataType::LogCrashReport. Adds no fields beyond
// the shared configuration, so it is an alias for DiagnosticDataConfig.
typedef DiagnosticDataConfig LogCrashReportConfig;

// Function pointer retrieved through the property getter as the value of
// FEP_DiagnosticDataFunc. It produces the diagnostic data described by 'config'
// and streams it to config->pfnOutput. Returns true on success, or false if
// config is NULL, config->size is smaller than sizeof(DiagnosticDataConfig),
// config->type is unrecognized, config->pfnOutput is NULL, or the sink aborts
// or an error occurs while producing the data.
//
// The function pointer must not be cached or invoked after the handler returns.
typedef bool (DOTNET_CALLCONV *DiagnosticDataFunc)(const DiagnosticDataConfig* config);

#endif // FATAL_ERROR_HANDLING_H
