// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "logging.h"
#include "simpletimer.h"
#include "minipal/time.h"

SimpleTimer::SimpleTimer()
{
    start = 0;
    stop  = 0;

    proc_freq = minipal_hires_tick_frequency();
}

SimpleTimer::~SimpleTimer()
{
}

void SimpleTimer::Start()
{
    start = minipal_hires_ticks();
}

void SimpleTimer::Stop()
{
    stop = minipal_hires_ticks();
}

double SimpleTimer::GetMilliseconds()
{
    return GetSeconds() * 1000.0;
}

double SimpleTimer::GetSeconds()
{
    return ((stop - start) / (double)proc_freq);
}
