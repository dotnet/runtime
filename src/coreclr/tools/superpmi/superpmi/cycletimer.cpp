// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "cycletimer.h"

#include <minipal/time.h>

CycleTimer::CycleTimer()
{
    start    = 0;
    stop     = 0;
    overhead = QueryOverhead();
}

CycleTimer::~CycleTimer()
{
}

void CycleTimer::Start()
{
    if (!minipal_get_thread_cycles(&start))
    {
        LogError("CycleTimer::Start unable to QPC. error was 0x%08x", ::GetLastError());
        DEBUG_BREAK;
    }
}

void CycleTimer::Stop()
{
    if (!minipal_get_thread_cycles(&stop))
    {
        LogError("CycleTimer::Stop unable to QPC. error was 0x%08x", ::GetLastError());
        DEBUG_BREAK;
    }
}

uint64_t CycleTimer::GetCycles()
{
    return stop - start - overhead;
}

uint64_t CycleTimer::QueryOverhead()
{
    uint64_t tot = 0;
    uint64_t startCycles;
    uint64_t endCycles;
    const int        N = 1000;
    for (int i = 0; i < N; i++)
    {
        minipal_get_thread_cycles(&startCycles);
        minipal_get_thread_cycles(&endCycles);
        tot += (endCycles - startCycles);
    }
    return tot / N;
}
