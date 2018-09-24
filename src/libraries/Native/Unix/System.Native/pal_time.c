// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_time.h"
#include "pal_utilities.h"

#include <assert.h>
#include <utime.h>
#include <time.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/time.h>
#if HAVE_MACH_ABSOLUTE_TIME
#include <mach/mach_time.h>
#endif

enum
{
    SecondsToMicroSeconds = 1000000,  // 10^6
    SecondsToNanoSeconds = 1000000000 // 10^9
};

int32_t SystemNative_UTimensat(const char* path, TimeSpec* times)
{
    int32_t result;
#if HAVE_UTIMENSAT
    struct timespec updatedTimes[2];
    updatedTimes[0].tv_sec = (time_t)times[0].tv_sec;
    updatedTimes[0].tv_nsec = (long)times[0].tv_nsec;

    updatedTimes[1].tv_sec = (time_t)times[1].tv_sec;
    updatedTimes[1].tv_nsec = (long)times[1].tv_nsec;    
    while (CheckInterrupted(result = utimensat(AT_FDCWD, path, updatedTimes, 0)));
#else
    struct timeval updatedTimes[2];
    updatedTimes[0].tv_sec = (long)times[0].tv_sec;
    updatedTimes[0].tv_usec = (int)times[0].tv_nsec / 1000;
    
    updatedTimes[1].tv_sec = (long)times[1].tv_sec;
    updatedTimes[1].tv_usec = (int)times[1].tv_nsec / 1000;
    while (CheckInterrupted(result = utimes(path, updatedTimes)));
#endif

    return result;
}

int32_t SystemNative_GetTimestampResolution(uint64_t* resolution)
{
    assert(resolution);

#if HAVE_MACH_ABSOLUTE_TIME
    mach_timebase_info_data_t mtid;
    if (mach_timebase_info(&mtid) == KERN_SUCCESS)
    {
        *resolution = SecondsToNanoSeconds * ((uint64_t)(mtid.denom) / (uint64_t)(mtid.numer));
        return 1;
    }
    else
    {
        *resolution = 0;
        return 0;
    }

#elif HAVE_CLOCK_MONOTONIC
    // Make sure we can call clock_gettime with MONOTONIC.  Stopwatch invokes
    // GetTimestampResolution as the very first thing, and by calling this here
    // to verify we can successfully, we don't have to branch in GetTimestamp.
    struct timespec ts;
    if (clock_gettime(CLOCK_MONOTONIC, &ts) == 0) 
    {
        *resolution = SecondsToNanoSeconds;
        return 1;
    }
    else
    {
        *resolution = 0;
        return 0;
    }

#else /* gettimeofday */
    *resolution = SecondsToMicroSeconds;
    return 1;

#endif
}

int32_t SystemNative_GetTimestamp(uint64_t* timestamp)
{
    assert(timestamp);

#if HAVE_MACH_ABSOLUTE_TIME
    *timestamp = mach_absolute_time();
    return 1;

#elif HAVE_CLOCK_MONOTONIC
    struct timespec ts;
    int result = clock_gettime(CLOCK_MONOTONIC, &ts);
    assert(result == 0); // only possible errors are if MONOTONIC isn't supported or &ts is an invalid address
    (void)result; // suppress unused parameter warning in release builds
    *timestamp = ((uint64_t)(ts.tv_sec) * SecondsToNanoSeconds) + (uint64_t)(ts.tv_nsec);
    return 1;

#else
    struct timeval tv;
    if (gettimeofday(&tv, NULL) == 0)
    {
        *timestamp = ((uint64_t)(tv.tv_sec) * SecondsToMicroSeconds) + (uint64_t)(tv.tv_usec);
        return 1;
    }
    else
    {
        *timestamp = 0;
        return 0;
    }

#endif
}

int32_t SystemNative_GetAbsoluteTime(uint64_t* timestamp)
{
    assert(timestamp);

#if  HAVE_MACH_ABSOLUTE_TIME
    *timestamp = mach_absolute_time();
    return 1;

#else
    *timestamp = 0;
    return 0;
#endif
}

int32_t SystemNative_GetTimebaseInfo(uint32_t* numer, uint32_t* denom)
{
#if  HAVE_MACH_TIMEBASE_INFO
    mach_timebase_info_data_t timebase;
    kern_return_t ret = mach_timebase_info(&timebase);
    assert(ret == KERN_SUCCESS);

    if (ret == KERN_SUCCESS)
    {
        *numer = timebase.numer;
        *denom = timebase.denom;
    }
    else
#endif
    {
        *numer = 1;
        *denom = 1;
    }
    return 1;
}
