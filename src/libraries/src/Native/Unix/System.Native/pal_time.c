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
#include <sys/resource.h>
#if HAVE_MACH_ABSOLUTE_TIME
#include <mach/mach_time.h>
#endif

enum
{
    SecondsToMicroSeconds = 1000000,   // 10^6
    SecondsToNanoSeconds = 1000000000, // 10^9
    MicroSecondsToNanoSeconds = 1000   // 10^3
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

// Gets the number of "ticks per second" of the underlying monotonic timer.
//
// On most Unix platforms, the methods that query the resolution return a value
// that is "nanoseconds per tick" in which case we need to scale before returning.
uint64_t SystemNative_GetTimestampResolution()
{
#if HAVE_MACH_ABSOLUTE_TIME
    mach_timebase_info_data_t mtid;

    if (mach_timebase_info(&mtid) != KERN_SUCCESS)
    {
        return 0;
    }

    // (numer / denom) gives you the nanoseconds per tick, so the below code
    // computes the number of ticks per second. We explicitly do the multiplication
    // first in order to help minimize the error that is produced by integer division.

    return (SecondsToNanoSeconds * (uint64_t)(mtid.denom)) / (uint64_t)(mtid.numer);
#else
    // clock_gettime() returns a result in terms of nanoseconds rather than a count. This
    // means that we need to either always scale the result by the actual resolution (to
    // get a count) or we need to say the resolution is in terms of nanoseconds. We prefer
    // the latter since it allows the highest throughput and should minimize error propagated
    // to the user.

    return SecondsToNanoSeconds;
#endif
}

uint64_t SystemNative_GetTimestamp()
{
#if HAVE_MACH_ABSOLUTE_TIME
    return mach_absolute_time();
#else
    struct timespec ts;

    int result = clock_gettime(CLOCK_MONOTONIC, &ts);
    assert(result == 0); // only possible errors are if MONOTONIC isn't supported or &ts is an invalid address
    (void)result; // suppress unused parameter warning in release builds

    return ((uint64_t)(ts.tv_sec) * SecondsToNanoSeconds) + (uint64_t)(ts.tv_nsec);
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

#if defined(_ARM_) || defined(_ARM64_)
#define SYSCONF_GET_NUMPROCS _SC_NPROCESSORS_CONF
#else
#define SYSCONF_GET_NUMPROCS _SC_NPROCESSORS_ONLN
#endif

int32_t SystemNative_GetCpuUtilization(ProcessCpuInformation* previousCpuInfo)
{
    static long numProcessors = 0;

    if (numProcessors <= 0)
    {
        numProcessors = sysconf(SYSCONF_GET_NUMPROCS);
        if (numProcessors <= 0)
        {
            return 0;
        }
    }

    uint64_t kernelTime = 0;
    uint64_t userTime = 0;

    struct rusage resUsage;
    if (getrusage(RUSAGE_SELF, &resUsage) == -1)
    {
        assert(false);
        return 0;
    }
    else
    {
        kernelTime =
            ((uint64_t)(resUsage.ru_stime.tv_sec) * SecondsToNanoSeconds) + 
            ((uint64_t)(resUsage.ru_stime.tv_usec) * MicroSecondsToNanoSeconds);
        userTime =
            ((uint64_t)(resUsage.ru_utime.tv_sec) * SecondsToNanoSeconds) +
            ((uint64_t)(resUsage.ru_utime.tv_usec) * MicroSecondsToNanoSeconds);
    }

    uint64_t resolution = SystemNative_GetTimestampResolution();
    uint64_t timestamp = SystemNative_GetTimestamp();

    uint64_t currentTime = timestamp * SecondsToNanoSeconds / resolution;

    uint64_t lastRecordedCurrentTime = previousCpuInfo->lastRecordedCurrentTime;
    uint64_t lastRecordedKernelTime = previousCpuInfo->lastRecordedKernelTime;
    uint64_t lastRecordedUserTime = previousCpuInfo->lastRecordedUserTime;

    uint64_t cpuTotalTime = 0;
    if (currentTime > lastRecordedCurrentTime)
    {
        // cpuTotalTime is based on clock time. Since multiple threads can run in parallel,
        // we need to scale cpuTotalTime cover the same amount of total CPU time.
        // rusage time is already scaled across multiple processors.
        cpuTotalTime = (currentTime - lastRecordedCurrentTime);
        cpuTotalTime *= (uint64_t)numProcessors;
    }

    uint64_t cpuBusyTime = 0;
    if (userTime >= lastRecordedUserTime && kernelTime >= lastRecordedKernelTime)
    {
        cpuBusyTime = (userTime - lastRecordedUserTime) + (kernelTime - lastRecordedKernelTime);
    }

    int32_t cpuUtilization = 0;
    if (cpuTotalTime > 0 && cpuBusyTime > 0)
    {
        cpuUtilization = (int32_t)(cpuBusyTime * 100 / cpuTotalTime);
    }

    assert(cpuUtilization >= 0 && cpuUtilization <= 100);

    previousCpuInfo->lastRecordedCurrentTime = currentTime;
    previousCpuInfo->lastRecordedUserTime = userTime;
    previousCpuInfo->lastRecordedKernelTime = kernelTime;

    return cpuUtilization;
}
