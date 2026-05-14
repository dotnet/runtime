// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generation.
//
// Emits a minimal createdump-shaped JSON payload to a *.crashreport.json file
// on disk.

#pragma once

#include <signal.h>
#include <stdint.h>

#include <minipal/guid.h>

#include "signalsafejsonwriter.h"

// Scratch-buffer sizes used throughout the in-proc crash reporter:
// - 1024 (matching createdump's MAX_LONGPATH) for paths (report paths and
//   expanded dump templates), so DOTNET_DbgMiniDumpName values that work
//   with createdump also work here.
// - 256 for identifiers (process name, type/class/exception names).
// - 32 for a single hex-or-decimal integer formatted as a C string
//   (addresses, thread IDs, hresults).
static constexpr size_t CRASHREPORT_PATH_BUFFER_SIZE = 1024;
static constexpr size_t CRASHREPORT_STRING_BUFFER_SIZE = 256;
static constexpr size_t CRASHREPORT_NUMBER_BUFFER_SIZE = 32;

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
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const GUID* moduleGuid,
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
    const char* reportPath;
    InProcCrashReportIsManagedThreadCallback isManagedThreadCallback;
    InProcCrashReportWalkStackCallback walkStackCallback;
    InProcCrashReportEnumerateThreadsCallback enumerateThreadsCallback;
    InProcCrashReportModuleInfoCallback moduleInfoCallback;
    uint32_t frameLimitPerThread;
};

class InProcCrashReporter
{
public:
    static InProcCrashReporter& GetInstance();

    // Capture configuration and the crash-report template path. Must be called
    // before the PAL enables signal-handler dispatch to CreateReport.
    void Initialize(const InProcCrashReporterSettings& settings);

    void CreateReport(
        int signal,
        siginfo_t* siginfo,
        void* context);

private:
    InProcCrashReporter() = default;
    InProcCrashReporter(const InProcCrashReporter&) = delete;
    InProcCrashReporter& operator=(const InProcCrashReporter&) = delete;

    void EmitSynthesizedCrashThread(
        void* context,
        bool walkStack);

    void EmitStackOverflowCrashThread();

    void EmitThreads(
        InProcCrashReportCrashKind crashKind,
        void* context);

    void BeginConsoleReport(int signal);
    void EndConsoleReport();

    void BeginJsonReport();
    void EndJsonReport(
        int signal,
        bool jsonEnabled,
        int fd);

    SignalSafeJsonWriter m_jsonWriter;
    InProcCrashReportIsManagedThreadCallback m_isManagedThreadCallback = nullptr;
    InProcCrashReportWalkStackCallback m_walkStackCallback = nullptr;
    InProcCrashReportEnumerateThreadsCallback m_enumerateThreadsCallback = nullptr;
    InProcCrashReportModuleInfoCallback m_moduleInfoCallback = nullptr;
    char m_reportPath[CRASHREPORT_PATH_BUFFER_SIZE] = {};
    char m_reportFilePathScratch[CRASHREPORT_PATH_BUFFER_SIZE] = {};
    char m_expandedReportPathScratch[CRASHREPORT_PATH_BUFFER_SIZE] = {};
    char m_numberScratch[CRASHREPORT_NUMBER_BUFFER_SIZE] = {};
    char m_methodNameScratch[CRASHREPORT_STRING_BUFFER_SIZE] = {};
    char m_processName[CRASHREPORT_STRING_BUFFER_SIZE] = {};
    char m_processNameScratch[CRASHREPORT_STRING_BUFFER_SIZE] = {};
    char m_hostName[CRASHREPORT_STRING_BUFFER_SIZE] = {};
#ifdef __APPLE__
    char m_osVersion[CRASHREPORT_STRING_BUFFER_SIZE] = {};
    char m_systemModel[CRASHREPORT_STRING_BUFFER_SIZE] = {};
#endif
    uint32_t m_frameLimitPerThread = 0;
};

// Free-function entry point used by the runtime to wire the in-proc crash
// reporter into the PAL signal-handler path. Captures `settings` into the
// singleton and registers a signal-safe dispatcher with PAL via
// PAL_SetInProcCrashReportCallback. PAL has no direct dependency on the
// reporter; the only coupling is through this registered callback.
void InProcCrashReportInitialize(const InProcCrashReporterSettings& settings);

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
void InProcCrashReportCompleteStackOverflowTrace(uint32_t truncatedFrameCount);
