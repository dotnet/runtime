// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include "cycletimer.h"
#include "winbase.h"
#include "winwrap.h"
#include "assert.h"
#include "utilcode.h"

bool CycleTimer::GetThreadCyclesS(unsigned __int64* cycles)
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
    unsigned __int64 cycleStart;
    if (!QueryPerformanceCounter(&qpcStart)) return 0.0;
    if (!GetThreadCyclesS(&cycleStart)) return 0.0;
    volatile int sum = 0;
    for (int k = 0; k < SampleLoopSize; k++)
    {
        sum += k;
    }
    LARGE_INTEGER qpcEnd;
    if (!QueryPerformanceCounter(&qpcEnd)) return 0.0;
    unsigned __int64 cycleEnd;
    if (!GetThreadCyclesS(&cycleEnd)) return 0.0;

    double qpcTicks = ((double)qpcEnd.QuadPart) - ((double)qpcStart.QuadPart);
    double secs = (qpcTicks / ((double)lpFrequency.QuadPart));
    double cycles = ((double)cycleEnd) - ((double)cycleStart);
    return cycles / secs;
}

// static
unsigned __int64 CycleTimer::QueryOverhead()
{
    unsigned __int64 tot = 0;
    unsigned __int64 startCycles;
    unsigned __int64 endCycles;
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
void CycleTimer::InterlockedAddU64(unsigned __int64* loc, unsigned __int64 amount)
{
    volatile __int64* vloc = (volatile __int64*)loc;
    unsigned __int64 prev = *vloc;
    for (;;)
    {
        unsigned __int64 next = prev + amount;
        __int64 snext = (__int64)next;
        __int64 sprev = (__int64)prev;
        __int64 res = InterlockedCompareExchange64(vloc, snext, sprev);
        if (res == sprev) return;
        else prev = (unsigned __int64)res;
    }
}

