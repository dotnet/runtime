// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generation.
//
// Emits a minimal createdump-shaped JSON payload to logcat / stderr.

#pragma once

#include <signal.h>
#include <stdint.h>

// Generate an in-proc crash report. Called from PROCCreateCrashDumpIfEnabled.
// All arguments come from the signal handler and are signal-safe to read.
void InProcCrashReportGenerate(int signal, siginfo_t* siginfo, void* context);

typedef int (*InProcCrashReportIsManagedThreadCallback)();

void InProcCrashReportSetCurrentThreadManagedResolver(InProcCrashReportIsManagedThreadCallback callback);

typedef void (*InProcCrashReportFrameCallback)(
    uint64_t ip,
    uint64_t stackPointer,
    const char* methodName,
    const char* className,
    const char* moduleName,
    uint32_t nativeOffset,
    uint32_t token,
    void* ctx);

typedef void (*InProcCrashReportWalkStackCallback)(
    InProcCrashReportFrameCallback frameCallback,
    void* ctx);

void InProcCrashReportSetStackWalker(InProcCrashReportWalkStackCallback callback);

typedef int (*InProcCrashReportGetExceptionCallback)(
    char* exceptionTypeBuf,
    int exceptionTypeBufSize,
    char* exceptionMsgBuf,
    int exceptionMsgBufSize,
    uint32_t* hresult);

void InProcCrashReportSetExceptionResolver(InProcCrashReportGetExceptionCallback callback);

typedef void (*InProcCrashReportThreadCallback)(
    uint64_t osThreadId,
    int isCrashThread,
    const char* exceptionType,
    uint32_t exceptionHResult,
    void* ctx);

typedef void (*InProcCrashReportEnumerateThreadsCallback)(
    uint64_t crashingTid,
    InProcCrashReportThreadCallback threadCallback,
    InProcCrashReportFrameCallback frameCallback,
    void* ctx);

void InProcCrashReportSetThreadEnumerator(InProcCrashReportEnumerateThreadsCallback callback);
