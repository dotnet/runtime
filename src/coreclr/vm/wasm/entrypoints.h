// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_WASM_ENTRYPOINTS_H
#define HAVE_WASM_ENTRYPOINTS_H

#include <stdint.h>
#include <string.h>

#ifdef TARGET_BROWSER
extern "C" void SystemJS_MarkAsyncMain();
extern "C" void SystemJS_ResolveMainPromise(int exitCode);
extern "C" void SystemJS_RejectMainPromise(const char16_t *message, int messageLength, const char16_t *stackTrace, int stackTraceLength);
extern "C" void SystemJS_ScheduleTimer(int shortestDueTimeMs);
extern "C" void SystemJS_ScheduleBackgroundJob();
extern "C" void SystemJS_DiagnosticServerQueueJob(size_t (*cb)(void *data), void *data);

#endif

#ifdef TARGET_WASI
// Native callback that runs the finalizer worker; called by managed code from
// the WasiEventLoop pump after EnableFinalization schedules it via
// WasiFinalizerScheduler. See src/coreclr/vm/finalizerthread.cpp.
extern "C" void SystemJS_ExecuteFinalizationCallback();
// Registration entry point used by managed System.Threading.WasiFinalizerScheduler
// to install its ThreadPool-based scheduling callback at startup.
typedef void (*WasiScheduleFinalizationFn)();
extern "C" void QCALLTYPE WasiFinalizerScheduler_Register(WasiScheduleFinalizationFn callback);
#endif // TARGET_WASI

#endif // HAVE_WASM_ENTRYPOINTS_H