// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef BROWSERPROFILER_H
#define BROWSERPROFILER_H

#ifdef TARGET_BROWSER

// Browser DevTools profiler for CoreCLR interpreter on WASM.
// Records method enter/leave events to the browser's Performance tab
// via performance.measure(). Uses a shadow stack to track method timing.
//
// The shadow stack is not thread-safe, so the profiler is only available on
// single-threaded (PERFTRACING_DISABLE_THREADS) builds, matching the
// condition under which INTOP_PROF_ENTER/INTOP_PROF_LEAVE are emitted.
#ifdef PERFTRACING_DISABLE_THREADS

void BrowserProfiler_OnMethodEnter(void *pMethodDesc);
void BrowserProfiler_OnMethodLeave(void *pMethodDesc);

// Returns a freshly malloc'd UTF-8 method name for a MethodDesc*.
// Called from JS; the caller owns the returned buffer and must free() it.
extern "C" const char* SystemJS_GetMethodName(void *pMethodDesc);

#endif // PERFTRACING_DISABLE_THREADS
#endif // TARGET_BROWSER

#endif // BROWSERPROFILER_H
