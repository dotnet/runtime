// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_WASM_ENTRYPOINTS_H
#define HAVE_WASM_ENTRYPOINTS_H

#include <stdint.h>
#include <string.h>

#ifdef TARGET_BROWSER
extern "C" void SystemJS_ResolveMainPromise(int exitCode);
extern "C" void SystemJS_RejectMainPromise(const char16_t *message, int messageLength, const char16_t *stackTrace, int stackTraceLength);
extern "C" void SystemJS_ScheduleTimer(int shortestDueTimeMs);
extern "C" void SystemJS_ScheduleBackgroundJob();

#endif

#endif // HAVE_WASM_ENTRYPOINTS_H