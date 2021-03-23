// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

static Volatile<bool> s_isYieldProcessorNormalizedInitialized = false;
static CrstStatic s_initializeYieldProcessorNormalizedCrst;

void InitializeYieldProcessorNormalizedCrst()
{
    WRAPPER_NO_CONTRACT;
    s_initializeYieldProcessorNormalizedCrst.Init(CrstLeafLock);
}

static void InitializeYieldProcessorNormalized()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    CrstHolder lock(&s_initializeYieldProcessorNormalizedCrst);

    if (s_isYieldProcessorNormalizedInitialized)
    {
        return;
    }

    // Intel pre-Skylake processor: measured typically 14-17 cycles per yield
    // Intel post-Skylake processor: measured typically 125-150 cycles per yield
    const int MeasureDurationMs = 10;
    const int NsPerSecond = 1000 * 1000 * 1000;

    LARGE_INTEGER li;
    if (!QueryPerformanceFrequency(&li) || (ULONGLONG)li.QuadPart < 1000 / MeasureDurationMs)
    {
        // High precision clock not available or clock resolution is too low, resort to defaults
        s_isYieldProcessorNormalizedInitialized = true;
        return;
    }
    ULONGLONG ticksPerSecond = li.QuadPart;

    // Measure the nanosecond delay per yield
    ULONGLONG measureDurationTicks = ticksPerSecond / (1000 / MeasureDurationMs);
    unsigned int yieldCount = 0;
    QueryPerformanceCounter(&li);
    ULONGLONG startTicks = li.QuadPart;
    ULONGLONG elapsedTicks;
    do
    {
        // On some systems, querying the high performance counter has relatively significant overhead. Do enough yields to mask
        // the timing overhead. Assuming one yield has a delay of MinNsPerNormalizedYield, 1000 yields would have a delay in the
        // low microsecond range.
        for (int i = 0; i < 1000; ++i)
        {
            System_YieldProcessor();
        }
        yieldCount += 1000;

        QueryPerformanceCounter(&li);
        ULONGLONG nowTicks = li.QuadPart;
        elapsedTicks = nowTicks - startTicks;
    } while (elapsedTicks < measureDurationTicks);
    double nsPerYield = (double)elapsedTicks * NsPerSecond / ((double)yieldCount * ticksPerSecond);
    if (nsPerYield < 1)
    {
        nsPerYield = 1;
    }

    // Calculate the number of yields required to span the duration of a normalized yield. Since nsPerYield is at least 1, this
    // value is naturally limited to MinNsPerNormalizedYield.
    int yieldsPerNormalizedYield = (int)(MinNsPerNormalizedYield / nsPerYield + 0.5);
    if (yieldsPerNormalizedYield < 1)
    {
        yieldsPerNormalizedYield = 1;
    }
    _ASSERTE(yieldsPerNormalizedYield <= (int)MinNsPerNormalizedYield);

    // Calculate the maximum number of yields that would be optimal for a late spin iteration. Typically, we would not want to
    // spend excessive amounts of time (thousands of cycles) doing only YieldProcessor, as SwitchToThread/Sleep would do a
    // better job of allowing other work to run.
    int optimalMaxNormalizedYieldsPerSpinIteration =
        (int)(NsPerOptimalMaxSpinIterationDuration / (yieldsPerNormalizedYield * nsPerYield) + 0.5);
    if (optimalMaxNormalizedYieldsPerSpinIteration < 1)
    {
        optimalMaxNormalizedYieldsPerSpinIteration = 1;
    }

    g_yieldsPerNormalizedYield = yieldsPerNormalizedYield;
    g_optimalMaxNormalizedYieldsPerSpinIteration = optimalMaxNormalizedYieldsPerSpinIteration;
    s_isYieldProcessorNormalizedInitialized = true;

    GCHeapUtilities::GetGCHeap()->SetYieldProcessorScalingFactor((float)yieldsPerNormalizedYield);
}

void EnsureYieldProcessorNormalizedInitialized()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (!s_isYieldProcessorNormalizedInitialized)
    {
        InitializeYieldProcessorNormalized();
    }
}
