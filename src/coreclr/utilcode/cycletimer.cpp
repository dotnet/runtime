// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include "cycletimer.h"
#include "winbase.h"
#include "winwrap.h"
#include "assert.h"
#include "utilcode.h"

bool CycleTimer::GetThreadCyclesS(uint64_t* cycles)
{
    BOOL res = FALSE;
    res = QueryThreadCycleTime(GetCurrentThread(), cycles);
    return res != FALSE;
}

static const int SampleLoopSize = 1000000;

// static
double CycleTimer::CyclesPerSecond()
{
    // Windows does not provide a way of converting cycles to time -- reasonably enough,
    // since the frequency of a machine may vary, due, e.g., to power management.
    // Windows *does* allow you to translate QueryPerformanceCounter counts into time,
    // however.  So we'll assume that the clock speed stayed constant, and measure both the
    // QPC counts and cycles of a short loop, to get a conversion factor.
    LARGE_INTEGER lpFrequency;
    if (!QueryPerformanceFrequency(&lpFrequency)) return 0.0;
    // Otherwise...
    LARGE_INTEGER qpcStart;
    uint64_t cycleStart;
    if (!QueryPerformanceCounter(&qpcStart)) return 0.0;
    if (!GetThreadCyclesS(&cycleStart)) return 0.0;
    volatile int sum = 0;
    for (int k = 0; k < SampleLoopSize; k++)
    {
        sum += k;
    }
    LARGE_INTEGER qpcEnd;
    if (!QueryPerformanceCounter(&qpcEnd)) return 0.0;
    uint64_t cycleEnd;
    if (!GetThreadCyclesS(&cycleEnd)) return 0.0;

    double qpcTicks = ((double)qpcEnd.QuadPart) - ((double)qpcStart.QuadPart);
    double secs = (qpcTicks / ((double)lpFrequency.QuadPart));
    double cycles = ((double)cycleEnd) - ((double)cycleStart);
    return cycles / secs;
}

// static
uint64_t CycleTimer::QueryOverhead()
{
    uint64_t tot = 0;
    uint64_t startCycles;
    uint64_t endCycles;
    const int N = 1000;
    bool b = GetThreadCyclesS(&startCycles); assert(b);
    for (int i = 0; i < N; i++)
    {
        b = GetThreadCyclesS(&endCycles); assert(b);
        tot += (endCycles-startCycles);
        startCycles = endCycles;
    }
    return tot/N;
}

// static
void CycleTimer::InterlockedAddU64(uint64_t* loc, uint64_t amount)
{
    volatile int64_t* vloc = (volatile int64_t*)loc;
    uint64_t prev = *vloc;
    for (;;)
    {
        uint64_t next = prev + amount;
        int64_t snext = (int64_t)next;
        int64_t sprev = (int64_t)prev;
        int64_t res = InterlockedCompareExchange64(vloc, snext, sprev);
        if (res == sprev) return;
        else prev = (uint64_t)res;
    }
}

