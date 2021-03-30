// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_threading.h"

#include <limits.h>
#include <sched.h>
#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>
#include <errno.h>
#include <time.h>
#include <sys/time.h>

#if defined(TARGET_OSX)
// So we can use the declaration of pthread_cond_timedwait_relative_np
#undef _XOPEN_SOURCE
#endif
#include <pthread.h>
#if defined(TARGET_OSX)
#define _XOPEN_SOURCE
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// LowLevelMonitor - Represents a non-recursive mutex and condition

struct LowLevelMonitor
{
    pthread_mutex_t Mutex;
    pthread_cond_t Condition;
#ifdef DEBUG
    bool IsLocked;
#endif
};

static void SetIsLocked(LowLevelMonitor* monitor, bool isLocked)
{
#ifdef DEBUG
    assert(monitor->IsLocked != isLocked);
    monitor->IsLocked = isLocked;
#else
    (void)monitor; // unused in release build
    (void)isLocked; // unused in release build
#endif
}

LowLevelMonitor* SystemNative_LowLevelMonitor_Create()
{
    LowLevelMonitor* monitor = (LowLevelMonitor *)malloc(sizeof(LowLevelMonitor));
    if (monitor == NULL)
    {
        return NULL;
    }

    int error;

    error = pthread_mutex_init(&monitor->Mutex, NULL);
    if (error != 0)
    {
        free(monitor);
        return NULL;
    }

#if HAVE_PTHREAD_CONDATTR_SETCLOCK && HAVE_CLOCK_MONOTONIC
    pthread_condattr_t conditionAttributes;
    error = pthread_condattr_init(&conditionAttributes);
    if (error != 0)
    {
        goto mutex_destroy;
    }

    error = pthread_condattr_setclock(&conditionAttributes, CLOCK_MONOTONIC);
    if (error != 0)
    {
        error = pthread_condattr_destroy(&conditionAttributes);
        assert(error == 0);
        goto mutex_destroy;
    }

    error = pthread_cond_init(&monitor->Condition, &conditionAttributes);

    int condAttrDestroyError = pthread_condattr_destroy(&conditionAttributes);
    assert(condAttrDestroyError == 0);
    (void)condAttrDestroyError; // unused in release build
#else
    error = pthread_cond_init(&monitor->Condition, NULL);
#endif
    if (error != 0)
    {
        goto mutex_destroy;
    }

#ifdef DEBUG
    monitor->IsLocked = false;
#endif

    return monitor;

mutex_destroy:
    error = pthread_mutex_destroy(&monitor->Mutex);
    assert(error == 0);
    free(monitor);
    return NULL;
}

void SystemNative_LowLevelMonitor_Destroy(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);

    int error;

    error = pthread_cond_destroy(&monitor->Condition);
    assert(error == 0);

    error = pthread_mutex_destroy(&monitor->Mutex);
    assert(error == 0);

    (void)error; // unused in release build

    free(monitor);
}

void SystemNative_LowLevelMonitor_Acquire(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);

    int error = pthread_mutex_lock(&monitor->Mutex);
    assert(error == 0);
    (void)error; // unused in release build

    SetIsLocked(monitor, true);
}

void SystemNative_LowLevelMonitor_Release(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);

    SetIsLocked(monitor, false);

    int error = pthread_mutex_unlock(&monitor->Mutex);
    assert(error == 0);
    (void)error; // unused in release build
}

void SystemNative_LowLevelMonitor_Wait(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);

    SetIsLocked(monitor, false);

    int error = pthread_cond_wait(&monitor->Condition, &monitor->Mutex);
    assert(error == 0);
    (void)error; // unused in release build

    SetIsLocked(monitor, true);
}

int32_t SystemNative_LowLevelMonitor_TimedWait(LowLevelMonitor *monitor, int32_t timeoutMilliseconds)
{
    assert(timeoutMilliseconds >= 0);

    SetIsLocked(monitor, false);

    int error;

    // Calculate the time at which a timeout should occur, and wait. Older versions of OSX don't support clock_gettime with
    // CLOCK_MONOTONIC, so we instead compute the relative timeout duration, and use a relative variant of the timed wait.
    struct timespec timeoutTimeSpec;
#if HAVE_CLOCK_GETTIME_NSEC_NP
    timeoutTimeSpec.tv_sec = timeoutMilliseconds / 1000;
    timeoutTimeSpec.tv_nsec = (timeoutMilliseconds % 1000) * 1000 * 1000;

    error = pthread_cond_timedwait_relative_np(&monitor->Condition, &monitor->Mutex, &timeoutTimeSpec);
#else
#if HAVE_PTHREAD_CONDATTR_SETCLOCK && HAVE_CLOCK_MONOTONIC
    error = clock_gettime(CLOCK_MONOTONIC, &timeoutTimeSpec);
    assert(error == 0);
#else
    struct timeval tv;

    error = gettimeofday(&tv, NULL);
    assert(error == 0);

    timeoutTimeSpec.tv_sec = tv.tv_sec;
    timeoutTimeSpec.tv_nsec = tv.tv_usec * 1000;
#endif
    uint64_t nanoseconds = (uint64_t)timeoutMilliseconds * 1000 * 1000 + (uint64_t)timeoutTimeSpec.tv_nsec;
    timeoutTimeSpec.tv_sec += nanoseconds / (1000 * 1000 * 1000);
    timeoutTimeSpec.tv_nsec = nanoseconds % (1000 * 1000 * 1000);

    error = pthread_cond_timedwait(&monitor->Condition, &monitor->Mutex, &timeoutTimeSpec);
#endif
    assert(error == 0 || error == ETIMEDOUT);

    SetIsLocked(monitor, true);

    return error == 0;
}

void SystemNative_LowLevelMonitor_Signal_Release(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);

    int error;

    error = pthread_cond_signal(&monitor->Condition);
    assert(error == 0);

    SetIsLocked(monitor, false);

    error = pthread_mutex_unlock(&monitor->Mutex);
    assert(error == 0);

    (void)error; // unused in release build
}
