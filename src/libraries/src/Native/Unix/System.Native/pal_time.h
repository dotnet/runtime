// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

typedef struct TimeSpec
{
    int64_t tv_sec; // seconds
    int64_t tv_nsec; // nanoseconds
} TimeSpec;

typedef struct ProcessCpuInformation
{
    uint64_t lastRecordedCurrentTime;
    uint64_t lastRecordedKernelTime;
    uint64_t lastRecordedUserTime;
} ProcessCpuInformation;


/**
 * Sets the last access and last modified time of a file
 *
 * Returns 0 on success; otherwise, returns -1 and errno is set.
 */
DLLEXPORT int32_t SystemNative_UTimensat(const char* path, TimeSpec* times);

/**
 * Gets the resolution of the timestamp, in counts per second.
 */
DLLEXPORT uint64_t SystemNative_GetTimestampResolution(void);

/**
 * Gets a high-resolution timestamp that can be used for time-interval measurements.
 */
DLLEXPORT uint64_t SystemNative_GetTimestamp(void);

DLLEXPORT int32_t SystemNative_GetAbsoluteTime(uint64_t* timestamp);

DLLEXPORT int32_t SystemNative_GetTimebaseInfo(uint32_t* numer, uint32_t* denom);

/**
 * The main purpose of this function is to compute the overall CPU utilization
 * for the CLR thread pool to regulate the number of worker threads.
 * Since there is no consistent API on Unix to get the CPU utilization
 * from a user process, getrusage and gettimeofday are used to
 * compute the current process's CPU utilization instead.
 */
DLLEXPORT int32_t SystemNative_GetCpuUtilization(ProcessCpuInformation* previousCpuInfo);
