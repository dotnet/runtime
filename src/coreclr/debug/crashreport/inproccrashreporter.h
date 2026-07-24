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

// IL-offset sentinel: ilOffset starts here and is overwritten only on a
// successful native->IL mapping, so a real IL offset of 0 stays distinct from
// "unavailable". Matches ICorDebugInfo::NO_MAPPING (0xffffffff is not a valid
// IL offset).
static constexpr uint32_t CRASHREPORT_NO_IL_OFFSET = 0xFFFFFFFFu;

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

struct InProcCrashReporterSettings
{
    const char* reportRootPath;
    int timeoutSeconds;
    InProcCrashReportIsManagedThreadCallback isManagedThreadCallback;
    InProcCrashReportWalkStackCallback walkStackCallback;
    InProcCrashReportEnumerateThreadsCallback enumerateThreadsCallback;
    InProcCrashReportModuleInfoCallback moduleInfoCallback;
    uint32_t frameLimitPerThread;
    int32_t maxFileCount;
};

// Free-function entry point used by the runtime to wire the in-proc crash
// reporter into the PAL signal-handler path. Captures `settings` into an
// init-time allocated reporter and registers a signal-safe dispatcher with PAL
// via PAL_SetInProcCrashReportCallback. PAL has no direct dependency on the
// reporter; the only coupling is through this registered callback.
void InProcCrashReportInitialize(const InProcCrashReporterSettings& settings);

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
