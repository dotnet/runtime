// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "logging.h"
#include "simpletimer.h"

SimpleTimer::SimpleTimer()
{
    start.QuadPart = 0;
    stop.QuadPart  = 0;

    BOOL retVal = ::QueryPerformanceFrequency(&proc_freq);
    if (retVal == FALSE)
    {
        LogDebug("SimpleTimer::SimpleTimer unable to QPF. error was 0x%08x", ::GetLastError());
        ::__debugbreak();
    }
}

SimpleTimer::~SimpleTimer()
{
}

void SimpleTimer::Start()
{
    BOOL retVal = ::QueryPerformanceCounter(&start);
    if (retVal == FALSE)
    {
        LogDebug("SimpleTimer::Start unable to QPC. error was 0x%08x", ::GetLastError());
        ::__debugbreak();
    }
}

void SimpleTimer::Stop()
{
    BOOL retVal = ::QueryPerformanceCounter(&stop);
    if (retVal == FALSE)
    {
        LogDebug("SimpleTimer::Stop unable to QPC. error was 0x%08x", ::GetLastError());
        ::__debugbreak();
    }
}

double SimpleTimer::GetMilliseconds()
{
    return GetSeconds() * 1000.0;
}

double SimpleTimer::GetSeconds()
{
    return ((stop.QuadPart - start.QuadPart) / (double)proc_freq.QuadPart);
}
