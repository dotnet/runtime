// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#if HAVE_CLOCK_GETTIME_NSEC_NP
#include <time.h>
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
    while (CheckInterrupted(result = utimensat(AT_FDCWD, path, updatedTimes, AT_SYMLINK_NOFOLLOW)));
#else
    struct timeval updatedTimes[2];
    updatedTimes[0].tv_sec = (long)times[0].tv_sec;
    updatedTimes[0].tv_usec = (int)times[0].tv_nsec / 1000;

    updatedTimes[1].tv_sec = (long)times[1].tv_sec;
    updatedTimes[1].tv_usec = (int)times[1].tv_nsec / 1000;
    while (CheckInterrupted(result =
#if HAVE_LUTIMES
        lutimes
#else
        utimes
#endif
        (path, updatedTimes)));
#endif

    return result;
}

uint64_t SystemNative_GetTimestamp()
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    return clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
#else
    struct timespec ts;

    int result = clock_gettime(CLOCK_MONOTONIC, &ts);
    assert(result == 0); // only possible errors are if MONOTONIC isn't supported or &ts is an invalid address
    (void)result; // suppress unused parameter warning in release builds

    return ((uint64_t)(ts.tv_sec) * SecondsToNanoSeconds) + (uint64_t)(ts.tv_nsec);
#endif
}

int32_t SystemNative_GetCpuUtilization(ProcessCpuInformation* previousCpuInfo)
{
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

    uint64_t currentTime = SystemNative_GetTimestamp();

    uint64_t lastRecordedCurrentTime = previousCpuInfo->lastRecordedCurrentTime;
    uint64_t lastRecordedKernelTime = previousCpuInfo->lastRecordedKernelTime;
    uint64_t lastRecordedUserTime = previousCpuInfo->lastRecordedUserTime;

    uint64_t cpuTotalTime = 0;
    if (currentTime > lastRecordedCurrentTime)
    {
        cpuTotalTime = (currentTime - lastRecordedCurrentTime);
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

    previousCpuInfo->lastRecordedCurrentTime = currentTime;
    previousCpuInfo->lastRecordedUserTime = userTime;
    previousCpuInfo->lastRecordedKernelTime = kernelTime;

    return cpuUtilization;
}
