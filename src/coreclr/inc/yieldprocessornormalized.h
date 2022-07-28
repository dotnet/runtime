// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// Undefine YieldProcessor to encourage using the normalized versions below instead. System_YieldProcessor() can be used where
// the intention is to use the system-default implementation of YieldProcessor().
#define HAS_SYSTEM_YIELDPROCESSOR
FORCEINLINE void System_YieldProcessor() { YieldProcessor(); }
#ifdef YieldProcessor
#undef YieldProcessor
#endif
#define YieldProcessor Dont_Use_YieldProcessor

#define DISABLE_COPY(T) \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

#define DISABLE_CONSTRUCT_COPY(T) \
    T() = delete; \
    DISABLE_COPY(T)

class YieldProcessorNormalization
{
public:
    static const unsigned int TargetNsPerNormalizedYield = 37;
    static const unsigned int TargetMaxNsPerSpinIteration = 272;

    // These are maximums for the computed values for normalization based their calculation
    static const unsigned int MaxYieldsPerNormalizedYield = TargetNsPerNormalizedYield * 10;
    static const unsigned int MaxOptimalMaxNormalizedYieldsPerSpinIteration =
        TargetMaxNsPerSpinIteration * 3 / (TargetNsPerNormalizedYield * 2) + 1;

private:
    static bool s_isMeasurementScheduled;

    static unsigned int s_yieldsPerNormalizedYield;
    static unsigned int s_optimalMaxNormalizedYieldsPerSpinIteration;

public:
    static bool IsMeasurementScheduled()
    {
        return s_isMeasurementScheduled;
    }

    static void PerformMeasurement();

private:
    static void ScheduleMeasurementIfNecessary();

public:
    static unsigned int GetOptimalMaxNormalizedYieldsPerSpinIteration()
    {
        return s_optimalMaxNormalizedYieldsPerSpinIteration;
    }

    static void FireMeasurementEvents();

private:
    static double AtomicLoad(double *valueRef);
    static void AtomicStore(double *valueRef, double value);

    DISABLE_CONSTRUCT_COPY(YieldProcessorNormalization);

    friend class YieldProcessorNormalizationInfo;
    friend void YieldProcessorNormalizedForPreSkylakeCount(unsigned int);
};

class YieldProcessorNormalizationInfo
{
private:
    unsigned int yieldsPerNormalizedYield;
    unsigned int optimalMaxNormalizedYieldsPerSpinIteration;
    unsigned int optimalMaxYieldsPerSpinIteration;

public:
    YieldProcessorNormalizationInfo()
        : yieldsPerNormalizedYield(YieldProcessorNormalization::s_yieldsPerNormalizedYield),
        optimalMaxNormalizedYieldsPerSpinIteration(YieldProcessorNormalization::s_optimalMaxNormalizedYieldsPerSpinIteration),
        optimalMaxYieldsPerSpinIteration(yieldsPerNormalizedYield * optimalMaxNormalizedYieldsPerSpinIteration)
    {
        YieldProcessorNormalization::ScheduleMeasurementIfNecessary();
    }

    DISABLE_COPY(YieldProcessorNormalizationInfo);

    friend void YieldProcessorNormalized(const YieldProcessorNormalizationInfo &);
    friend void YieldProcessorNormalized(const YieldProcessorNormalizationInfo &, unsigned int);
    friend void YieldProcessorNormalizedForPreSkylakeCount(const YieldProcessorNormalizationInfo &, unsigned int);
    friend void YieldProcessorWithBackOffNormalized(const YieldProcessorNormalizationInfo &, unsigned int);
};

// See YieldProcessorNormalized() for preliminary info. Typical usage:
//     if (!condition)
//     {
//         YieldProcessorNormalizationInfo normalizationInfo;
//         do
//         {
//             YieldProcessorNormalized(normalizationInfo);
//         } while (!condition);
//     }
FORCEINLINE void YieldProcessorNormalized(const YieldProcessorNormalizationInfo &normalizationInfo)
{
    unsigned int n = normalizationInfo.yieldsPerNormalizedYield;
    _ASSERTE(n != 0);
    do
    {
        System_YieldProcessor();
    } while (--n != 0);
}

// Delays execution of the current thread for a short duration. Unlike YieldProcessor(), an effort is made to normalize the
// delay across processors. The actual delay may be meaningful in several ways, including but not limited to the following:
//   - The delay should be long enough that a tiny spin-wait like the following has a decent likelihood of observing a new value
//     for the condition (when changed by a different thread) on each iteration, otherwise it may unnecessary increase CPU usage
//     and decrease scalability of the operation.
//         while(!condition)
//         {
//             YieldProcessorNormalized();
//         }
//   - The delay should be short enough that a tiny spin-wait like above would not miss multiple cross-thread changes to the
//     condition, otherwise it may unnecessarily increase latency of the operation
//   - In reasonably short spin-waits, the actual delay may not matter much. In unreasonably long spin-waits that progress in
//     yield count per iteration for each failed check of the condition, the progression can significantly magnify the second
//     issue above on later iterations.
//   - This function and variants are intended to provide a decent balance between the above issues, as ideal solutions to each
//     issue have trade-offs between them. If latency of the operation is far more important in the scenario, consider using
//     System_YieldProcessor() instead, which would issue a delay that is typically <= the delay issued by this method.
FORCEINLINE void YieldProcessorNormalized()
{
    YieldProcessorNormalized(YieldProcessorNormalizationInfo());
}

// See YieldProcessorNormalized(count) for preliminary info. Typical usage:
//     if (!moreExpensiveCondition)
//     {
//         YieldProcessorNormalizationInfo normalizationInfo;
//         do
//         {
//             YieldProcessorNormalized(normalizationInfo, 2);
//         } while (!moreExpensiveCondition);
//     }
FORCEINLINE void YieldProcessorNormalized(const YieldProcessorNormalizationInfo &normalizationInfo, unsigned int count)
{
    _ASSERTE(count != 0);

    if (sizeof(SIZE_T) <= sizeof(unsigned int))
    {
        // On platforms with a small SIZE_T, prevent overflow on the multiply below
        const unsigned int MaxCount = UINT_MAX / YieldProcessorNormalization::MaxYieldsPerNormalizedYield;
        if (count > MaxCount)
        {
            count = MaxCount;
        }
    }

    SIZE_T n = (SIZE_T)count * normalizationInfo.yieldsPerNormalizedYield;
    _ASSERTE(n != 0);
    do
    {
        System_YieldProcessor();
    } while (--n != 0);
}

// See YieldProcessorNormalized() for preliminary info. This function repeats the delay 'count' times. This overload is
// preferred over the single-count overload when multiple yields are desired per spin-wait iteration. Typical usage:
//     while(!moreExpensiveCondition)
//     {
//         YieldProcessorNormalized(2);
//     }
FORCEINLINE void YieldProcessorNormalized(unsigned int count)
{
    YieldProcessorNormalized(YieldProcessorNormalizationInfo(), count);
}

// Please DO NOT use this function in new code! See YieldProcessorNormalizedForPreSkylakeCount(preSkylakeCount) for preliminary
// info. Typical usage:
//     if (!condition)
//     {
//         YieldProcessorNormalizationInfo normalizationInfo;
//         do
//         {
//             YieldProcessorNormalizedForPreSkylakeCount(normalizationInfo, 100);
//         } while (!condition);
//     }
FORCEINLINE void YieldProcessorNormalizedForPreSkylakeCount(
    const YieldProcessorNormalizationInfo &normalizationInfo,
    unsigned int preSkylakeCount)
{
    _ASSERTE(preSkylakeCount != 0);

    if (sizeof(SIZE_T) <= sizeof(unsigned int))
    {
        // On platforms with a small SIZE_T, prevent overflow on the multiply below
        const unsigned int MaxCount = UINT_MAX / YieldProcessorNormalization::MaxYieldsPerNormalizedYield;
        if (preSkylakeCount > MaxCount)
        {
            preSkylakeCount = MaxCount;
        }
    }

    const unsigned int PreSkylakeCountToSkylakeCountDivisor = 8;
    SIZE_T n = (SIZE_T)preSkylakeCount * normalizationInfo.yieldsPerNormalizedYield / PreSkylakeCountToSkylakeCountDivisor;
    if (n == 0)
    {
        n = 1;
    }
    do
    {
        System_YieldProcessor();
    } while (--n != 0);
}

// Please DO NOT use this function in new code! This function is to be used for old spin-wait loops that have not been retuned
// for recent processors, and especially where the yield count may be unreasonably high. The function scales the yield count in
// an attempt to normalize the total delay across processors, to approximately the total delay that would be issued on a
// pre-Skylake processor. New code should be tuned with YieldProcessorNormalized() or variants instead. Typical usage:
//     while(!condition)
//     {
//         YieldProcessorNormalizedForPreSkylakeCount(100);
//     }
FORCEINLINE void YieldProcessorNormalizedForPreSkylakeCount(unsigned int preSkylakeCount)
{
    // This function does not forward to the one above because it is used by some code under utilcode, where
    // YieldProcessorNormalizationInfo cannot be used since normalization does not happen in some of its consumers. So this
    // version uses the fields in YieldProcessorNormalization directly.

    _ASSERTE(preSkylakeCount != 0);

    if (sizeof(SIZE_T) <= sizeof(unsigned int))
    {
        // On platforms with a small SIZE_T, prevent overflow on the multiply below
        const unsigned int MaxCount = UINT_MAX / YieldProcessorNormalization::MaxYieldsPerNormalizedYield;
        if (preSkylakeCount > MaxCount)
        {
            preSkylakeCount = MaxCount;
        }
    }

    const unsigned int PreSkylakeCountToSkylakeCountDivisor = 8;
    SIZE_T n =
        (SIZE_T)preSkylakeCount *
        YieldProcessorNormalization::s_yieldsPerNormalizedYield /
        PreSkylakeCountToSkylakeCountDivisor;
    if (n == 0)
    {
        n = 1;
    }
    do
    {
        System_YieldProcessor();
    } while (--n != 0);
}

// See YieldProcessorNormalized() for preliminary info. This function is to be used when there is a decent possibility that the
// condition would not be satisfied within a short duration. The current implementation increases the delay per spin-wait
// iteration exponentially up to a limit. Typical usage:
//     if (!conditionThatMayNotBeSatisfiedSoon)
//     {
//         YieldProcessorNormalizationInfo normalizationInfo;
//         do
//         {
//             YieldProcessorWithBackOffNormalized(normalizationInfo); // maybe Sleep(0) occasionally
//         } while (!conditionThatMayNotBeSatisfiedSoon);
//     }
FORCEINLINE void YieldProcessorWithBackOffNormalized(
    const YieldProcessorNormalizationInfo &normalizationInfo,
    unsigned int spinIteration)
{
    // This shift value should be adjusted based on the asserted conditions below
    const UINT8 MaxShift = 3;
    static_assert_no_msg(
        ((unsigned int)1 << MaxShift) <= YieldProcessorNormalization::MaxOptimalMaxNormalizedYieldsPerSpinIteration);
    static_assert_no_msg(
        ((unsigned int)1 << (MaxShift + 1)) > YieldProcessorNormalization::MaxOptimalMaxNormalizedYieldsPerSpinIteration);

    unsigned int n;
    if (spinIteration <= MaxShift &&
        ((unsigned int)1 << spinIteration) < normalizationInfo.optimalMaxNormalizedYieldsPerSpinIteration)
    {
        n = ((unsigned int)1 << spinIteration) * normalizationInfo.yieldsPerNormalizedYield;
    }
    else
    {
        n = normalizationInfo.optimalMaxYieldsPerSpinIteration;
    }
    _ASSERTE(n != 0);
    do
    {
        System_YieldProcessor();
    } while (--n != 0);
}

#undef DISABLE_CONSTRUCT_COPY
#undef DISABLE_COPY
