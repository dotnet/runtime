// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generation.
//
// Emits a minimal createdump-shaped JSON payload to a *.crashreport.json file
// on disk.

#pragma once

#include <signal.h>
#include <stdint.h>

#include "signalsafejsonwriter.h"

using InProcCrashReportIsManagedThreadCallback = bool (*)();

using InProcCrashReportFrameCallback = void (*)(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    uint32_t ilOffset,
    uint32_t moduleTimestamp,
    uint32_t moduleSize,
    const char* moduleGuid,
    void* ctx);

using InProcCrashReportWalkStackCallback = void (*)(
    InProcCrashReportFrameCallback frameCallback,
    void* ctx);

using InProcCrashReportGetExceptionCallback = bool (*)(
    char* exceptionTypeBuf,
    size_t exceptionTypeBufSize,
    uint32_t* hresult);

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

struct InProcCrashReporterSettings
{
    const char* reportPath;
    InProcCrashReportIsManagedThreadCallback isManagedThreadCallback;
    InProcCrashReportWalkStackCallback walkStackCallback;
    InProcCrashReportGetExceptionCallback getExceptionCallback;
    InProcCrashReportEnumerateThreadsCallback enumerateThreadsCallback;
};

class InProcCrashReporter
{
public:
    static InProcCrashReporter& GetInstance();

    // Capture configuration and the crash-report template path. Must be called
    // before the PAL enables signal-handler dispatch to CreateReport.
    void Initialize(const InProcCrashReporterSettings& settings);

    // Generate an in-proc crash report. Called from PROCCreateCrashDumpIfEnabled.
    // All arguments come from the signal handler and are signal-safe to read.
    void CreateReport(int signal, siginfo_t* siginfo, void* context);

private:
    InProcCrashReporter() = default;
    InProcCrashReporter(const InProcCrashReporter&) = delete;
    InProcCrashReporter& operator=(const InProcCrashReporter&) = delete;

    void EmitSynthesizedCrashThread(
        void* context,
        bool hasException,
        const char* crashExceptionType,
        uint32_t crashExceptionHResult,
        bool walkStack);

    SignalSafeJsonWriter m_jsonWriter;
    InProcCrashReportIsManagedThreadCallback m_isManagedThreadCallback = nullptr;
    InProcCrashReportWalkStackCallback m_walkStackCallback = nullptr;
    InProcCrashReportGetExceptionCallback m_getExceptionCallback = nullptr;
    InProcCrashReportEnumerateThreadsCallback m_enumerateThreadsCallback = nullptr;
    char m_reportPath[256] = {};
    char m_processName[256] = {};
};
