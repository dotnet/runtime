// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "yieldprocessornormalized.h"


#include "finalizerthread.h"

enum class NormalizationState : UINT8
{
    Uninitialized,
    Initialized,
    Failed
};

static const int NsPerYieldMeasurementCount = 8;
static const unsigned int MeasurementPeriodMs = 4000;

static const unsigned int NsPerS = 1000 * 1000 * 1000;

static NormalizationState s_normalizationState = NormalizationState::Uninitialized;
static unsigned int s_previousNormalizationTimeMs;

static UINT64 s_performanceCounterTicksPerS;
static double s_nsPerYieldMeasurements[NsPerYieldMeasurementCount];
static int s_nextMeasurementIndex;
static double s_establishedNsPerYield = YieldProcessorNormalization::TargetNsPerNormalizedYield;

static unsigned int DetermineMeasureDurationUs()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(s_normalizationState != NormalizationState::Failed);

    // On some systems, querying the high performance counter has relatively significant overhead. Increase the measure duration
    // if the overhead seems high relative to the measure duration.
    unsigned int measureDurationUs = 1;
    LARGE_INTEGER li;
    QueryPerformanceCounter(&li);
    UINT64 startTicks = li.QuadPart;
    QueryPerformanceCounter(&li);
    UINT64 elapsedTicks = li.QuadPart - startTicks;
    if (elapsedTicks >= s_performanceCounterTicksPerS * measureDurationUs * (1000 / 4) / NsPerS) // elapsed >= 1/4 of the measure duration
    {
        measureDurationUs *= 4;
    }
    return measureDurationUs;
}

static double MeasureNsPerYield(unsigned int measureDurationUs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(s_normalizationState != NormalizationState::Failed);

    int yieldCount = (int)(measureDurationUs * 1000 / s_establishedNsPerYield) + 1;
    UINT64 ticksPerS = s_performanceCounterTicksPerS;
    UINT64 measureDurationTicks = ticksPerS * measureDurationUs / (1000 * 1000);

    LARGE_INTEGER li;
    QueryPerformanceCounter(&li);
    UINT64 startTicks = li.QuadPart;

    for (int i = 0; i < yieldCount; ++i)
    {
        System_YieldProcessor();
    }

    QueryPerformanceCounter(&li);
    UINT64 elapsedTicks = li.QuadPart - startTicks;
    while (elapsedTicks < measureDurationTicks)
    {
        int nextYieldCount =
            Max(4,
                elapsedTicks == 0
                    ? yieldCount / 4
                    : (int)(yieldCount * (measureDurationTicks - elapsedTicks) / (double)elapsedTicks) + 1);
        for (int i = 0; i < nextYieldCount; ++i)
        {
            System_YieldProcessor();
        }

        QueryPerformanceCounter(&li);
        elapsedTicks = li.QuadPart - startTicks;
        yieldCount += nextYieldCount;
    }

    // Limit the minimum to a reasonable value considering that on some systems a yield may be implemented as a no-op
    const double MinNsPerYield = 0.1;

    // Measured values higher than this don't affect values calculated for normalization, and it's very unlikely for a yield to
    // really take this long. Limit the maximum to keep the recorded values reasonable.
    const double MaxNsPerYield = YieldProcessorNormalization::TargetMaxNsPerSpinIteration / 1.5 + 1;

    return Max(MinNsPerYield, Min((double)elapsedTicks * NsPerS / ((double)yieldCount * ticksPerS), MaxNsPerYield));
}

void YieldProcessorNormalization::PerformMeasurement()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(s_isMeasurementScheduled);

    double latestNsPerYield;
    if (s_normalizationState == NormalizationState::Initialized)
    {
        if (GetTickCount() - s_previousNormalizationTimeMs < MeasurementPeriodMs)
        {
            return;
        }

        int nextMeasurementIndex = s_nextMeasurementIndex;
        latestNsPerYield = MeasureNsPerYield(DetermineMeasureDurationUs());
        AtomicStore(&s_nsPerYieldMeasurements[nextMeasurementIndex], latestNsPerYield);
        if (++nextMeasurementIndex >= NsPerYieldMeasurementCount)
        {
            nextMeasurementIndex = 0;
        }
        s_nextMeasurementIndex = nextMeasurementIndex;
    }
    else if (s_normalizationState == NormalizationState::Uninitialized)
    {
        LARGE_INTEGER li;
        if (!QueryPerformanceFrequency(&li) || li.QuadPart < 1000 * 1000)
        {
            // High precision clock not available or clock resolution is too low, resort to defaults
            s_normalizationState = NormalizationState::Failed;
            return;
        }
        s_performanceCounterTicksPerS = li.QuadPart;

        unsigned int measureDurationUs = DetermineMeasureDurationUs();
        for (int i = 0; i < NsPerYieldMeasurementCount; ++i)
        {
            latestNsPerYield = MeasureNsPerYield(measureDurationUs);
            AtomicStore(&s_nsPerYieldMeasurements[i], latestNsPerYield);
            if (i == 0 || latestNsPerYield < s_establishedNsPerYield)
            {
                AtomicStore(&s_establishedNsPerYield, latestNsPerYield);
            }

            if (i < NsPerYieldMeasurementCount - 1)
            {
                FireEtwYieldProcessorMeasurement(GetClrInstanceId(), latestNsPerYield, s_establishedNsPerYield);
            }
        }
    }
    else
    {
        _ASSERTE(s_normalizationState == NormalizationState::Failed);
        return;
    }

    double establishedNsPerYield = s_nsPerYieldMeasurements[0];
    for (int i = 1; i < NsPerYieldMeasurementCount; ++i)
    {
        double nsPerYield = s_nsPerYieldMeasurements[i];
        if (nsPerYield < establishedNsPerYield)
        {
            establishedNsPerYield = nsPerYield;
        }
    }
    if (establishedNsPerYield != s_establishedNsPerYield)
    {
        AtomicStore(&s_establishedNsPerYield, establishedNsPerYield);
    }

    FireEtwYieldProcessorMeasurement(GetClrInstanceId(), latestNsPerYield, s_establishedNsPerYield);

    // Calculate the number of yields required to span the duration of a normalized yield
    unsigned int yieldsPerNormalizedYield = Max(1u, (unsigned int)(TargetNsPerNormalizedYield / establishedNsPerYield + 0.5));
    _ASSERTE(yieldsPerNormalizedYield <= MaxYieldsPerNormalizedYield);
    s_yieldsPerNormalizedYield = yieldsPerNormalizedYield;

    // Calculate the maximum number of yields that would be optimal for a late spin iteration. Typically, we would not want to
    // spend excessive amounts of time (thousands of cycles) doing only YieldProcessor, as SwitchToThread/Sleep would do a
    // better job of allowing other work to run.
    s_optimalMaxNormalizedYieldsPerSpinIteration =
        Max(1u, (unsigned int)(TargetMaxNsPerSpinIteration / (yieldsPerNormalizedYield * establishedNsPerYield) + 0.5));
    _ASSERTE(s_optimalMaxNormalizedYieldsPerSpinIteration <= MaxOptimalMaxNormalizedYieldsPerSpinIteration);

    GCHeapUtilities::GetGCHeap()->SetYieldProcessorScalingFactor((float)yieldsPerNormalizedYield);

    s_previousNormalizationTimeMs = GetTickCount();
    s_normalizationState = NormalizationState::Initialized;
    s_isMeasurementScheduled = false;
}


void YieldProcessorNormalization::ScheduleMeasurementIfNecessary()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    NormalizationState normalizationState = VolatileLoadWithoutBarrier(&s_normalizationState);
    if (normalizationState == NormalizationState::Initialized)
    {
        if (GetTickCount() - s_previousNormalizationTimeMs < MeasurementPeriodMs)
        {
            return;
        }
    }
    else if (normalizationState == NormalizationState::Uninitialized)
    {
    }
    else
    {
        _ASSERTE(normalizationState == NormalizationState::Failed);
        return;
    }

    // !g_fEEStarted is required for FinalizerThread::EnableFinalization() below
    if (s_isMeasurementScheduled || !g_fEEStarted)
    {
        return;
    }

    s_isMeasurementScheduled = true;
    FinalizerThread::EnableFinalization();
}


void YieldProcessorNormalization::FireMeasurementEvents()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!EventEnabledYieldProcessorMeasurement())
    {
        return;
    }

    // This function may be called at any time to fire events about recorded measurements. There is no synchronization for the
    // recorded information, so try to enumerate the array with some care.
    double establishedNsPerYield = AtomicLoad(&s_establishedNsPerYield);
    int nextIndex = VolatileLoadWithoutBarrier(&s_nextMeasurementIndex);
    for (int i = 0; i < NsPerYieldMeasurementCount; ++i)
    {
        double nsPerYield = AtomicLoad(&s_nsPerYieldMeasurements[nextIndex]);
        if (nsPerYield != 0) // the array may not be fully initialized yet
        {
            FireEtwYieldProcessorMeasurement(GetClrInstanceId(), nsPerYield, establishedNsPerYield);
        }

        if (++nextIndex >= NsPerYieldMeasurementCount)
        {
            nextIndex = 0;
        }
    }
}

double YieldProcessorNormalization::AtomicLoad(double *valueRef)
{
    WRAPPER_NO_CONTRACT;

#ifdef TARGET_64BIT
    return VolatileLoadWithoutBarrier(valueRef);
#else
    return InterlockedCompareExchangeT(valueRef, 0.0, 0.0);
#endif
}

void YieldProcessorNormalization::AtomicStore(double *valueRef, double value)
{
    WRAPPER_NO_CONTRACT;

#ifdef TARGET_64BIT
    *valueRef = value;
#else
    InterlockedExchangeT(valueRef, value);
#endif
}

