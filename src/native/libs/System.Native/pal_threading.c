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
#if HAVE_SCHED_GETCPU
#include <sched.h>
#endif

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

LowLevelMonitor* SystemNative_LowLevelMonitor_Create(void)
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

int32_t SystemNative_CreateThread(uintptr_t stackSize, void *(*startAddress)(void*), void *parameter)
{
    bool result = false;
    pthread_attr_t attrs;

    int error = pthread_attr_init(&attrs);
    if (error != 0)
    {
        // Do not call pthread_attr_destroy
        return false;
    }

    error = pthread_attr_setdetachstate(&attrs, PTHREAD_CREATE_DETACHED);
    assert(error == 0);

#ifdef HOST_OSX
    // Match Windows stack size
    if (stackSize == 0)
    {
        stackSize = 4096 * 1024;
    }
#endif

    if (stackSize > 0)
    {
        if (stackSize < (uintptr_t)PTHREAD_STACK_MIN)
        {
            stackSize = (uintptr_t)PTHREAD_STACK_MIN;
        }

        error = pthread_attr_setstacksize(&attrs, stackSize);
        if (error != 0) goto CreateThreadExit;
    }

    pthread_t threadId;
    error = pthread_create(&threadId, &attrs, startAddress, parameter);
    if (error != 0) goto CreateThreadExit;

    result = true;

CreateThreadExit:
    error = pthread_attr_destroy(&attrs);
    assert(error == 0);

    return result;
}

int32_t SystemNative_SchedGetCpu(void)
{
#if HAVE_SCHED_GETCPU
    return sched_getcpu();
#else
    return -1;
#endif
}

__attribute__((noreturn))
void SystemNative_Exit(int32_t exitCode)
{
    exit(exitCode);
}

__attribute__((noreturn))
void SystemNative_Abort(void)
{
    abort();
}

// Gets a non-truncated OS thread ID that is also suitable for diagnostics, for platforms that offer a 64-bit ID
uint64_t SystemNative_GetUInt64OSThreadId(void)
{
#ifdef __APPLE__
    uint64_t threadId;
    int result = pthread_threadid_np(pthread_self(), &threadId);
    assert(result == 0);
    return threadId;
#else
    assert(false);
    return 0;
#endif
}

#if defined(__linux__)
#include <sys/syscall.h>
#include <unistd.h>
#elif defined(__FreeBSD__)
#include <pthread_np.h>
#elif defined(__NetBSD__)
#include <lwp.h>
#endif

// Tries to get a non-truncated OS thread ID that is also suitable for diagnostics, for platforms that offer a 32-bit ID.
// Returns (uint32_t)-1 when the implementation does not know how to get the OS thread ID.
uint32_t SystemNative_TryGetUInt32OSThreadId(void)
{
    const uint32_t InvalidId = (uint32_t)-1;

#if defined(__linux__)
    assert(sizeof(pid_t) == sizeof(uint32_t));
    uint32_t threadId = (uint32_t)syscall(SYS_gettid);
    assert(threadId != InvalidId);
    return threadId;
#elif defined(__FreeBSD__)
    uint32_t threadId = (uint32_t)pthread_getthreadid_np();
    assert(threadId != InvalidId);
    return threadId;
#elif defined(__NetBSD__)
    assert(sizeof(lwpid_t) == sizeof(uint32_t));
    uint32_t threadId = (uint32_t)_lwp_self();
    assert(threadId != InvalidId);
    return threadId;
#else
    return InvalidId;
#endif
}
