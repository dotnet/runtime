// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_threading.h"

#include <limits.h>
#include <sched.h>
#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>

#include <pthread.h>

#include <time.h>
#include <sys/time.h>

#if HAVE_MACH_ABSOLUTE_TIME
#include <mach/mach_time.h>
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

    error = pthread_cond_init(&monitor->Condition, NULL);
    if (error != 0)
    {
        error = pthread_mutex_destroy(&monitor->Mutex);
        assert(error == 0);
        free(monitor);
        return NULL;
    }

#ifdef DEBUG
    monitor->IsLocked = false;
#endif

    return monitor;
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

uint32_t SystemNative_LowLevelMonitor_TimedWait(LowLevelMonitor* monitor, int32_t timeoutMilliseconds)
{
    assert(monitor != NULL);
    assert(timeoutMilliseconds >= -1);

    if (timeoutMilliseconds < 0)
    {
        SystemNative_LowLevelMonitor_Wait(monitor);
        return true;
    }

    SetIsLocked(monitor, false);

    struct timespec ts;
    int error;

#if HAVE_MACH_ABSOLUTE_TIME
    memset (&ts, 0, sizeof(struct timespec));
    ts.tv_sec = timeoutMilliseconds / 1000;
    ts.tv_nsec = (timeoutMilliseconds % 1000) * 1000 * 1000;

    error = pthread_cond_timedwait_relative_np(&monitor->Condition, &monitor->Mutex, &ts);
#else
    error = clock_gettime(CLOCK_MONOTONIC, &ts);
    assert(error == 0);

    ts.tv_sec += timeoutMilliseconds / 1000;
    ts.tv_nsec += (timeoutMilliseconds % 1000) * 1000 * 1000;
    if (ts.tv_nsec >= 1000 * 1000 * 1000) {
        ts.tv_nsec -= 1000 * 1000 * 1000;
        ts.tv_sec ++;
    }

    error = pthread_cond_timedwait(&monitor->Condition, &monitor->Mutex, &ts);
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
