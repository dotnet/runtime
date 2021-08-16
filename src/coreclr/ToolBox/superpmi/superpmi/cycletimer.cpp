// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "cycletimer.h"

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
    BOOL retVal = QueryThreadCycleTime(GetCurrentThread(), &start);

    if (retVal == FALSE)
    {
        LogError("CycleTimer::Start unable to QPC. error was 0x%08x", ::GetLastError());
        ::__debugbreak();
    }
}

void CycleTimer::Stop()
{
    BOOL retVal = QueryThreadCycleTime(GetCurrentThread(), &stop);

    if (retVal == FALSE)
    {
        LogError("CycleTimer::Stop unable to QPC. error was 0x%08x", ::GetLastError());
        ::__debugbreak();
    }
}

unsigned __int64 CycleTimer::GetCycles()
{
    return stop - start - overhead;
}

unsigned __int64 CycleTimer::QueryOverhead()
{
    unsigned __int64 tot = 0;
    unsigned __int64 startCycles;
    unsigned __int64 endCycles;
    const int        N = 1000;
    for (int i = 0; i < N; i++)
    {
        QueryThreadCycleTime(GetCurrentThread(), &startCycles);
        QueryThreadCycleTime(GetCurrentThread(), &endCycles);
        tot += (endCycles - startCycles);
    }
    return tot / N;
}
