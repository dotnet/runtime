// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
PALEXPORT int32_t SystemNative_UTimensat(const char* path, TimeSpec* times);

/**
 * Sets the last access and last modified time of a file
 *
 * Returns 0 on success; otherwise, returns -1 and errno is set.
 */
PALEXPORT int32_t SystemNative_FUTimens(intptr_t fd, TimeSpec* times);

/**
 * Gets a high-resolution timestamp that can be used for time-interval measurements.
 */
PALEXPORT uint64_t SystemNative_GetTimestamp(void);

/**
 * The main purpose of this function is to compute the overall CPU utilization
 * for the CLR thread pool to regulate the number of worker threads.
 * Since there is no consistent API on Unix to get the CPU utilization
 * from a user process, getrusage and gettimeofday are used to
 * compute the current process's CPU utilization instead. The CPU utilization
 * returned is sum of utilization across all processors, e.g. this function will
 * return 200 when two cores are running at 100%.
 */
PALEXPORT int32_t SystemNative_GetCpuUtilization(ProcessCpuInformation* previousCpuInfo);
