// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generation.
//
// Emits a minimal createdump-shaped JSON payload to a *.crashreport.json file
// on disk.

#pragma once

#include <signal.h>
#include <stdint.h>

void InitializeInProcCrashReport(const char* dumpPath);

// Generate an in-proc crash report. Called from PROCCreateCrashDumpIfEnabled.
// All arguments come from the signal handler and are signal-safe to read.
void CreateInProcCrashReport(int signal, siginfo_t* siginfo, void* context);

using InProcCrashReportIsManagedThreadCallback = bool (*)();

void InProcCrashReportSetCurrentThreadManagedResolver(InProcCrashReportIsManagedThreadCallback callback);

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

void InProcCrashReportSetStackWalker(InProcCrashReportWalkStackCallback callback);

using InProcCrashReportGetExceptionCallback = bool (*)(
    char* exceptionTypeBuf,
    size_t exceptionTypeBufSize,
    uint32_t* hresult);

void InProcCrashReportSetExceptionResolver(InProcCrashReportGetExceptionCallback callback);

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

void InProcCrashReportSetThreadEnumerator(InProcCrashReportEnumerateThreadsCallback callback);
