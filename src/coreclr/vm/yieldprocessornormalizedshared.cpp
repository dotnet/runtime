// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef FEATURE_NATIVEAOT
#include "finalizerthread.h"
#endif

enum class NormalizationState : uint8_t
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

static uint64_t s_performanceCounterTicksPerS;
static double s_nsPerYieldMeasurements[NsPerYieldMeasurementCount];
static int s_nextMeasurementIndex;
static double s_establishedNsPerYield = YieldProcessorNormalization::TargetNsPerNormalizedYield;

static LARGE_INTEGER li;

void RhEnableFinalization();

inline unsigned int GetTickCountPortable()
{
#ifdef FEATURE_NATIVEAOT
    return (unsigned int)PalGetTickCount64();
#else
    return GetTickCount();
#endif
}

static uint64_t GetPerformanceCounter()
{
#ifdef FEATURE_NATIVEAOT
    return PalQueryPerformanceCounter();
#else
    QueryPerformanceCounter(&li);
    return li.QuadPart;
#endif
}

static unsigned int DetermineMeasureDurationUs()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
#ifndef FEATURE_NATIVEAOT
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    _ASSERTE(s_normalizationState != NormalizationState::Failed);

    // On some systems, querying the high performance counter has relatively significant overhead. Increase the measure duration
    // if the overhead seems high relative to the measure duration.
    unsigned int measureDurationUs = 1;
    uint64_t startTicks = GetPerformanceCounter();
    uint64_t elapsedTicks = GetPerformanceCounter() - startTicks;
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
#ifndef FEATURE_NATIVEAOT
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    _ASSERTE(s_normalizationState != NormalizationState::Failed);

    int yieldCount = (int)(measureDurationUs * 1000 / s_establishedNsPerYield) + 1;
    uint64_t ticksPerS = s_performanceCounterTicksPerS;
    uint64_t measureDurationTicks = ticksPerS * measureDurationUs / (1000 * 1000);

    uint64_t startTicks = GetPerformanceCounter();

    for (int i = 0; i < yieldCount; ++i)
    {
        System_YieldProcessor();
    }

    uint64_t elapsedTicks = GetPerformanceCounter() - startTicks;
    while (elapsedTicks < measureDurationTicks)
    {
        int nextYieldCount =
            max(4,
                elapsedTicks == 0
                    ? yieldCount / 4
                    : (int)(yieldCount * (measureDurationTicks - elapsedTicks) / (double)elapsedTicks) + 1);
        for (int i = 0; i < nextYieldCount; ++i)
        {
            System_YieldProcessor();
        }

        elapsedTicks = GetPerformanceCounter() - startTicks;
        yieldCount += nextYieldCount;
    }

    // Limit the minimum to a reasonable value considering that on some systems a yield may be implemented as a no-op
    const double MinNsPerYield = 0.1;

    // Measured values higher than this don't affect values calculated for normalization, and it's very unlikely for a yield to
    // really take this long. Limit the maximum to keep the recorded values reasonable.
    const double MaxNsPerYield = YieldProcessorNormalization::TargetMaxNsPerSpinIteration / 1.5 + 1;

    return max(MinNsPerYield, min((double)elapsedTicks * NsPerS / ((double)yieldCount * ticksPerS), MaxNsPerYield));
}

void YieldProcessorNormalization::PerformMeasurement()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
#ifndef FEATURE_NATIVEAOT
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    _ASSERTE(s_isMeasurementScheduled);

    double latestNsPerYield;
    if (s_normalizationState == NormalizationState::Initialized)
    {
        if (GetTickCountPortable() - s_previousNormalizationTimeMs < MeasurementPeriodMs)
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
#ifdef FEATURE_NATIVEAOT
        if ((s_performanceCounterTicksPerS = PalQueryPerformanceFrequency()) < 1000 * 1000)
#else
        if (!QueryPerformanceFrequency(&li) || li.QuadPart < 1000 * 1000)
#endif
        {
            // High precision clock not available or clock resolution is too low, resort to defaults
            s_normalizationState = NormalizationState::Failed;
            return;
        }

#ifndef FEATURE_NATIVEAOT
        s_performanceCounterTicksPerS = li.QuadPart;
#endif

        unsigned int measureDurationUs = DetermineMeasureDurationUs();
        for (int i = 0; i < NsPerYieldMeasurementCount; ++i)
        {
            latestNsPerYield = MeasureNsPerYield(measureDurationUs);
            AtomicStore(&s_nsPerYieldMeasurements[i], latestNsPerYield);
            if (i == 0 || latestNsPerYield < s_establishedNsPerYield)
            {
                AtomicStore(&s_establishedNsPerYield, latestNsPerYield);
            }
#ifndef FEATURE_NATIVEAOT
            if (i < NsPerYieldMeasurementCount - 1)
            {
                FireEtwYieldProcessorMeasurement(GetClrInstanceId(), latestNsPerYield, s_establishedNsPerYield);
            }
#endif
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
    unsigned int yieldsPerNormalizedYield = max(1u, (unsigned int)(TargetNsPerNormalizedYield / establishedNsPerYield + 0.5));
    _ASSERTE(yieldsPerNormalizedYield <= MaxYieldsPerNormalizedYield);
    s_yieldsPerNormalizedYield = yieldsPerNormalizedYield;

    // Calculate the maximum number of yields that would be optimal for a late spin iteration. Typically, we would not want to
    // spend excessive amounts of time (thousands of cycles) doing only YieldProcessor, as SwitchToThread/Sleep would do a
    // better job of allowing other work to run.
    s_optimalMaxNormalizedYieldsPerSpinIteration =
        max(1u, (unsigned int)(TargetMaxNsPerSpinIteration / (yieldsPerNormalizedYield * establishedNsPerYield) + 0.5));
    _ASSERTE(s_optimalMaxNormalizedYieldsPerSpinIteration <= MaxOptimalMaxNormalizedYieldsPerSpinIteration);

    GCHeapUtilities::GetGCHeap()->SetYieldProcessorScalingFactor((float)yieldsPerNormalizedYield);

    s_previousNormalizationTimeMs = GetTickCountPortable();
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
        if (GetTickCountPortable() - s_previousNormalizationTimeMs < MeasurementPeriodMs)
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

#ifdef FEATURE_NATIVEAOT
    if (s_isMeasurementScheduled)
#else
    // !g_fEEStarted is required for FinalizerThread::EnableFinalization() below
    if (s_isMeasurementScheduled || !g_fEEStarted)
#endif
    {
        return;
    }

    s_isMeasurementScheduled = true;
#ifdef FEATURE_NATIVEAOT
    RhEnableFinalization();
#else
    FinalizerThread::EnableFinalization();
#endif
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
#ifdef FEATURE_NATIVEAOT
    static_assert(sizeof(int64_t) == sizeof(double), "");
    int64_t intRes = PalInterlockedCompareExchange64((int64_t*)valueRef, 0, 0);
    return *(double*)(int64_t*)(&intRes);
#else
    return InterlockedCompareExchangeT(valueRef, 0.0, 0.0);
#endif
#endif
}

void YieldProcessorNormalization::AtomicStore(double *valueRef, double value)
{
    WRAPPER_NO_CONTRACT;

#ifdef TARGET_64BIT
    *valueRef = value;
#else
#ifdef FEATURE_NATIVEAOT
    static_assert(sizeof(int64_t) == sizeof(double), "");
    PalInterlockedExchange64((int64_t *)valueRef, *(int64_t *)(double*)&value);
#else
    InterlockedExchangeT(valueRef, value);
#endif
#endif
}

