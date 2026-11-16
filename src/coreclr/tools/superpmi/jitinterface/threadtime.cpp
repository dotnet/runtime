// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

// Not dllexport.h, whose STDMETHODCALLTYPE conflicts with windows.h.
#ifdef _WIN32
#include <windows.h>
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#include <time.h>
#define DLL_EXPORT extern "C" __attribute__((visibility("default")))
#endif

// The CPU time used by the current thread: in cycles on Windows, and elsewhere in nanoseconds (as the PAL's
// QueryThreadCycleTime).
DLL_EXPORT uint64_t GetThreadCycleTime()
{
#ifdef _WIN32
    ULONG64 cycles = 0;
    QueryThreadCycleTime(GetCurrentThread(), &cycles);
    return cycles;
#else
    struct timespec ts;
    clock_gettime(CLOCK_THREAD_CPUTIME_ID, &ts);
    return (uint64_t)ts.tv_sec * 1000000000 + (uint64_t)ts.tv_nsec;
#endif
}
