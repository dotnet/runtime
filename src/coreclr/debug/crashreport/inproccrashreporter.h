// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generation.
//
// Emits a minimal createdump-shaped JSON payload to a *.crashreport.json file
// on disk.

#pragma once

#include <signal.h>
#include <stddef.h>
#include <stdint.h>

#include <minipal/guid.h>

// Scratch-buffer sizes used throughout the in-proc crash reporter:
// - 1024 (matching createdump's MAX_LONGPATH) for report paths.
// - 256 for identifiers (process name, type/class/exception names).
static constexpr size_t CRASHREPORT_PATH_BUFFER_SIZE = 1024;
static constexpr size_t CRASHREPORT_STRING_BUFFER_SIZE = 256;
static constexpr int32_t CRASHREPORT_DEFAULT_MAX_FILE_COUNT = 32;

#if defined(__ANDROID__)
static const char CRASHREPORT_LOG_TAG[] = "DOTNET_CRASH";
#endif

enum class InProcCrashReportCrashKind : uint32_t
{
    Unknown = 0,
    StackOverflow = 1,
};

using InProcCrashReportIsManagedThreadCallback = bool (*)();

using InProcCrashReportFrameCallback = void (*)(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    const void* moduleHandle,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    void* ctx);

using InProcCrashReportWalkStackCallback = void (*)(
    InProcCrashReportFrameCallback frameCallback,
    void* ctx);

using InProcCrashReportThreadCallback = void (*)(
    uint64_t osThreadId,
    bool isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult,
    void* ctx);

using InProcCrashReportEnumerateThreadsCallback = void (*)(
    uint64_t crashingTid,
    InProcCrashReportThreadCallback threadCallback,
    InProcCrashReportFrameCallback frameCallback,
    void* ctx);

using InProcCrashReportModuleInfoCallback = bool (*)(
    const void* moduleHandle,
    const char** moduleName,
    GUID* moduleGuid);

// Output format for an on-demand report (see InProcCrashReportCreateReport).
enum class InProcCrashReportOutputFormat : uint32_t
{
    Json = 0,
    Log = 1,
};

// Receives on-demand report bytes as they are produced: raw JSON document chunks
// for the Json format, newline-delimited lines for the Log format. Returns false
// on write failure. Must be async-signal-safe when invoked from a crash context.
using InProcCrashReportOutputCallback = bool (*)(const char* buffer, size_t length, void* context);

struct InProcCrashReporterSettings
{
    InProcCrashReportIsManagedThreadCallback isManagedThreadCallback;
    InProcCrashReportWalkStackCallback walkStackCallback;
    InProcCrashReportEnumerateThreadsCallback enumerateThreadsCallback;
    InProcCrashReportModuleInfoCallback moduleInfoCallback;
    uint32_t frameLimitPerThread;
};

struct InProcCrashReporterServicesSettings
{
    const char* reportRootPath;
    int timeoutSeconds;
    int32_t maxFileCount;
    bool enableCreateCrashDump;
    bool enableWatchdog;
    bool enableLifecycle;
};

// Free-function entry points used by the runtime to bring up the in-proc crash
// reporter. InProcCrashReportInitialize captures `settings` into an init-time
// allocated reporter (VM callbacks only) and is idempotent, so it can be called
// both from the runtime's startup configuration and from a fatal-error-handler
// setup path; the reporter is then available for on-demand reports regardless of
// crash-dump configuration.
void InProcCrashReportInitialize(const InProcCrashReporterSettings& settings);

// InProcCrashReportInitializeServices starts the env-gated crash-dump services:
// when settings.enableCreateCrashDump is true it starts configured services like
// the watchdog / lifecycle and registers a signal-safe dispatcher with PAL via
// PAL_SetInProcCrashReportCallback so the default reporter runs on a fatal
// signal. Requires the reporter to have been initialized first via
// InProcCrashReportInitialize (it is a no-op otherwise) and starts the services
// exactly once. PAL has no direct dependency on the reporter; the only coupling is
// through the optionally-registered callback.
void InProcCrashReportInitializeServices(const InProcCrashReporterServicesSettings& settings);

// Generates a crash report on demand, independent of the fatal-signal path.
// Runs the reporter's emit core over the current process without the watchdog
// or lifecycle/file management, streaming the selected `outputFormat` to
// `outputCallback` (called with `callbackContext`). `signal` and `context`
// describe the crash site: `context` is the platform machine context used to
// walk the crashing (calling) thread's stack, or null to defer to the
// registered stack-walk callback's default. Intended for a user fatal-error
// handler to drive report generation while the runtime is still up.
//
// Re-runnable sequentially, but yields to any report already in flight: returns
// false if the reporter is not initialized, `outputCallback` is null, or a
// signal-handler / on-demand report is currently being generated.
bool InProcCrashReportCreateReport(
    InProcCrashReportOutputFormat outputFormat,
    int signal,
    void* context,
    InProcCrashReportOutputCallback outputCallback,
    void* callbackContext);

// Emits initialization failures before crash-report storage exists.
void InProcCrashReportLogInitializationFailure(const char* message);

// Records crash kind hints from VM fatal paths that later terminate through PAL
// as a generic signal (for example stack overflow -> SIGABRT).
void InProcCrashReportSetCrashKind(InProcCrashReportCrashKind crashKind);

// Captures the compressed stack-overflow trace built by the runtime SO helper
// thread so the later in-proc crash reporter can include the same managed stack
// without trying to walk from the exhausted crashing stack.
void InProcCrashReportBeginStackOverflowTrace(uint64_t crashingTid, uint32_t totalFrameCount);
void InProcCrashReportAddStackOverflowTraceFrame(
    const char* methodName,
    uint32_t repeatCount,
    uint32_t repeatSequenceLength);
void InProcCrashReportEndStackOverflowTrace();
