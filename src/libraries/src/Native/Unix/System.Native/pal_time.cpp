// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_time.h"
#include "pal_utilities.h"

#include <assert.h>
#include <utime.h>
#include <time.h>
#include <sys/time.h>
#if HAVE_MACH_ABSOLUTE_TIME
#include <mach/mach_time.h>
#endif

enum
{
    SecondsToMicroSeconds = 1000000,  // 10^6
    SecondsToNanoSeconds = 1000000000 // 10^9
};

static void ConvertUTimBuf(const UTimBuf& pal, utimbuf& native)
{
    native.actime = static_cast<time_t>(pal.AcTime);
    native.modtime = static_cast<time_t>(pal.ModTime);
}

extern "C" int32_t SystemNative_UTime(const char* path, UTimBuf* times)
{
    assert(times != nullptr);

    utimbuf temp;
    ConvertUTimBuf(*times, temp);

    int32_t result;
    while (CheckInterrupted(result = utime(path, &temp)));
    return result;
}

extern "C" int32_t SystemNative_GetTimestampResolution(uint64_t* resolution)
{
    assert(resolution);

#if HAVE_CLOCK_MONOTONIC
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

#elif HAVE_MACH_ABSOLUTE_TIME
    mach_timebase_info_data_t mtid;
    if (mach_timebase_info(&mtid) == KERN_SUCCESS)
    {
        *resolution = SecondsToNanoSeconds * (static_cast<uint64_t>(mtid.denom) / static_cast<uint64_t>(mtid.numer));
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

extern "C" int32_t SystemNative_GetTimestamp(uint64_t* timestamp)
{
    assert(timestamp);

#if HAVE_CLOCK_MONOTONIC
    struct timespec ts;
    int result = clock_gettime(CLOCK_MONOTONIC, &ts);
    assert(result == 0); // only possible errors are if MONOTONIC isn't supported or &ts is an invalid address
    (void)result; // suppress unused parameter warning in release builds
    *timestamp = (static_cast<uint64_t>(ts.tv_sec) * SecondsToNanoSeconds) + static_cast<uint64_t>(ts.tv_nsec);
    return 1;

#elif HAVE_MACH_ABSOLUTE_TIME
    *timestamp = mach_absolute_time();
    return 1;

#else
    struct timeval tv;
    if (gettimeofday(&tv, NULL) == 0)
    {
        *timestamp = (static_cast<uint64_t>(tv.tv_sec) * SecondsToMicroSeconds) + static_cast<uint64_t>(tv.tv_usec);
        return 1;
    }
    else
    {
        *timestamp = 0;
        return 0;
    }

#endif
}

extern "C" int32_t SystemNative_GetAbsoluteTime(uint64_t* timestamp)
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

extern "C" int32_t SystemNative_GetTimebaseInfo(uint32_t* numer, uint32_t* denom)
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
