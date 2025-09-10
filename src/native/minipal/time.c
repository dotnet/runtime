// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <minipal/time.h>
#include "minipalconfig.h"

#if HOST_WINDOWS

#include <Windows.h>

int64_t minipal_hires_ticks()
{
    LARGE_INTEGER ts;
    BOOL ret;
    ret = QueryPerformanceCounter(&ts);
    assert(ret); // The function is documented to never fail on Windows XP+.
    return ts.QuadPart;
}

int64_t minipal_hires_tick_frequency()
{
    LARGE_INTEGER ts;
    BOOL ret;
    ret = QueryPerformanceFrequency(&ts);
    assert(ret); // The function is documented to never fail on Windows XP+.
    return ts.QuadPart;
}

int64_t minipal_lowres_ticks()
{
    return GetTickCount64();
}

uint64_t minipal_get_system_time()
{
    FILETIME filetime;
    GetSystemTimeAsFileTime(&filetime);
    return ((uint64_t)filetime.dwHighDateTime << 32) | filetime.dwLowDateTime;
}

#else // HOST_WINDOWS

#include "minipalconfig.h"

#include <time.h>
#include <sys/time.h>
#include <errno.h>

inline static void YieldProcessor(void);

inline static void YieldProcessor(void)
{
#if defined(HOST_X86) || defined(HOST_AMD64)
    __asm__ __volatile__(
        "rep\n"
        "nop");
#elif defined(HOST_ARM)
    __asm__ __volatile__( "yield");
#elif defined(HOST_ARM64)
    __asm__ __volatile__(
        "dmb ishst\n"
        "yield"
        );
#elif defined(HOST_LOONGARCH64)
    __asm__ volatile( "dbar 0;  \n");
#elif defined(HOST_RISCV64)
    // TODO-RISCV64-CQ: When Zihintpause is supported, replace with `pause` instruction.
    __asm__ __volatile__(".word 0x0100000f");
#else
    return;
#endif
}

#define tccSecondsToNanoSeconds 1000000000      // 10^9
#define tccSecondsToMilliSeconds 1000           // 10^3
#define tccMilliSecondsToNanoSeconds 1000000    // 10^6
#define tccSecondsTo100NS 10000000              // 10^7
int64_t minipal_hires_tick_frequency(void)
{
    return tccSecondsToNanoSeconds;
}

int64_t minipal_hires_ticks(void)
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    return (int64_t)clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
#elif HAVE_CLOCK_MONOTONIC
    struct timespec ts;
    int result = clock_gettime(CLOCK_MONOTONIC, &ts);
    if (result != 0)
    {
        assert(!"clock_gettime(CLOCK_MONOTONIC) failed");
    }

    return ((int64_t)(ts.tv_sec) * (int64_t)(tccSecondsToNanoSeconds)) + (int64_t)(ts.tv_nsec);
#else
    #error "minipal_hires_ticks requires clock_gettime_nsec_np or clock_gettime to be supported."
#endif
}

int64_t minipal_lowres_ticks(void)
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    return  (int64_t)clock_gettime_nsec_np(CLOCK_UPTIME_RAW) / (int64_t)(tccMilliSecondsToNanoSeconds);
#elif HAVE_CLOCK_MONOTONIC
    struct timespec ts;

    // emscripten exposes CLOCK_MONOTONIC_COARSE but doesn't implement it
#if HAVE_CLOCK_MONOTONIC_COARSE && !defined(__EMSCRIPTEN__)
    // CLOCK_MONOTONIC_COARSE has enough precision for GetTickCount but
    // doesn't have the same overhead as CLOCK_MONOTONIC. This allows
    // overall higher throughput. See dotnet/coreclr#2257 for more details.

    const clockid_t clockType = CLOCK_MONOTONIC_COARSE;
#else
    const clockid_t clockType = CLOCK_MONOTONIC;
#endif

    int result = clock_gettime(clockType, &ts);
    if (result != 0)
    {
#if HAVE_CLOCK_MONOTONIC_COARSE && !defined(__EMSCRIPTEN__)
        assert(!"clock_gettime(CLOCK_MONOTONIC_COARSE) failed");
#else
        assert(!"clock_gettime(CLOCK_MONOTONIC) failed");
#endif
    }

    return ((int64_t)(ts.tv_sec) * (int64_t)(tccSecondsToMilliSeconds)) + ((int64_t)(ts.tv_nsec) / (int64_t)(tccMilliSecondsToNanoSeconds));
#else
    #error "minipal_lowres_ticks requires clock_gettime_nsec_np or clock_gettime to be supported."
#endif
}

uint64_t minipal_get_system_time(void)
{
    struct timespec ts;
    if (clock_gettime(CLOCK_REALTIME, &ts) != 0)
    {
        assert(!"clock_gettime(CLOCK_REALTIME) failed");
    }

    const int64_t SECS_BETWEEN_1601_AND_1970_EPOCHS = 11644473600LL;
    return ((int64_t)(ts.tv_sec) + SECS_BETWEEN_1601_AND_1970_EPOCHS) * tccSecondsTo100NS + (ts.tv_nsec / 100);
}

#endif // HOST_WINDOWS

void minipal_microdelay(uint32_t usecs, uint32_t* usecsSinceYield)
{
#if HOST_WINDOWS
    if (usecs > 1000)
    {
        SleepEx(usecs / 1000, FALSE);
        if (usecsSinceYield)
        {
            *usecsSinceYield = 0;
        }

        return;
    }
#else
    if (usecs > 10)
    {
        struct timespec requested;
        requested.tv_sec = usecs / 1000;
        requested.tv_nsec = (usecs - requested.tv_sec * 1000) * 1000;

        struct timespec remaining;
        while (nanosleep(&requested, &remaining) == EINTR)
        {
            requested = remaining;
        }

        if (usecsSinceYield)
        {
            *usecsSinceYield = 0;
        }

        return;
    }
#endif

    int64_t startTicks = minipal_hires_ticks();
    int64_t ticksPerSecond = minipal_hires_tick_frequency();
    int64_t endTicks = startTicks + (usecs * ticksPerSecond) / 1000000;

    // start with 1 nop/pause and then double up until we hit the limit
    // this way we should not overshoot by more than 2x.
    for (int i = 0; i < 30; i++)
    {
        for (int j = 0; j < (1 << i); j++)
        {
            YieldProcessor();
        }

        int64_t currentTicks = minipal_hires_ticks();
        if (currentTicks > endTicks)
        {
            break;
        }
    }

    if (usecsSinceYield)
    {
        *usecsSinceYield += usecs;
    }
}
