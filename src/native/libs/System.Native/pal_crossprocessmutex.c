// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_errno.h"
#include "pal_crossprocessmutex.h"

#include <limits.h>
#include <sched.h>
#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>
#include <errno.h>
#include <time.h>
#include <sys/time.h>
#include <minipal/thread.h>
#if HAVE_SCHED_GETCPU
#include <sched.h>
#endif
#include <pthread.h>

static int32_t AcquirePThreadMutexWithTimeout(pthread_mutex_t* mutex, int32_t timeoutMilliseconds)
{
    assert(mutex != NULL);

    if (timeoutMilliseconds == -1)
    {
        return pthread_mutex_lock(mutex);
    }
    else if (timeoutMilliseconds == 0)
    {
        return pthread_mutex_trylock(mutex);
    }

    // Calculate the time at which a timeout should occur, and wait. Older versions of OSX don't support clock_gettime with
    // CLOCK_MONOTONIC, so we instead compute the relative timeout duration, and use a relative variant of the timed wait.
    struct timespec timeoutTimeSpec;
    int32_t error = 0;
#if HAVE_CLOCK_GETTIME_NSEC_NP
    timeoutTimeSpec.tv_sec = timeoutMilliseconds / 1000;
    timeoutTimeSpec.tv_nsec = (timeoutMilliseconds % 1000) * 1000 * 1000;

    error = pthread_mutex_reltimedlock_np(mutex, &timeoutTimeSpec);
#elif HAVE_PTHREAD_MUTEX_CLOCKLOCK && HAVE_CLOCK_MONOTONIC
    error = clock_gettime(CLOCK_MONOTONIC, &timeoutTimeSpec);
    assert(error == 0);

    uint64_t nanoseconds = (uint64_t)timeoutMilliseconds * 1000 * 1000 + (uint64_t)timeoutTimeSpec.tv_nsec;

    timeoutTimeSpec.tv_sec += nanoseconds / (1000 * 1000 * 1000);
    timeoutTimeSpec.tv_nsec = nanoseconds % (1000 * 1000 * 1000);

    return pthread_mutex_clocklock(mutex, CLOCK_MONOTONIC, &timeoutTimeSpec);
#else
    struct timeval tv;

    error = gettimeofday(&tv, NULL);
    assert(error == 0);

    timeoutTimeSpec.tv_sec = tv.tv_sec;
    timeoutTimeSpec.tv_nsec = (long)(tv.tv_usec * 1000);

    uint64_t nanoseconds = (uint64_t)timeoutMilliseconds * 1000 * 1000 + (uint64_t)timeoutTimeSpec.tv_nsec;

    timeoutTimeSpec.tv_sec += nanoseconds / (1000 * 1000 * 1000);
    timeoutTimeSpec.tv_nsec = nanoseconds % (1000 * 1000 * 1000);

    return pthread_mutex_timedlock(mutex, &timeoutTimeSpec);
#endif
}

struct LowLevelCrossProcessMutex
{
    pthread_mutex_t Mutex;
    uint32_t OwnerProcessId;
    uint32_t OwnerThreadId;
    uint8_t IsAbandoned;
};

#define INVALID_PROCESS_ID (uint32_t)(-1)
#define INVALID_THREAD_ID (uint32_t)(-1)

int32_t SystemNative_LowLevelCrossProcessMutex_Size(void)
{
    return (int32_t)sizeof(LowLevelCrossProcessMutex);
}

int32_t SystemNative_LowLevelCrossProcessMutex_Init(LowLevelCrossProcessMutex* mutex)
{
    mutex->OwnerProcessId = INVALID_PROCESS_ID;
    mutex->OwnerThreadId = INVALID_THREAD_ID;
    mutex->IsAbandoned = 0;
    pthread_mutexattr_t mutexAttributes;
    int error = pthread_mutexattr_init(&mutexAttributes);
    if (error != 0)
    {
        return ConvertErrorPlatformToPal(error);
    }

    error = pthread_mutexattr_settype(&mutexAttributes, PTHREAD_MUTEX_RECURSIVE);
    assert(error == 0);

    error = pthread_mutexattr_setrobust(&mutexAttributes, PTHREAD_MUTEX_ROBUST);
    assert(error == 0);

    error = pthread_mutexattr_setpshared(&mutexAttributes, PTHREAD_PROCESS_SHARED);
    assert(error == 0);

    error = pthread_mutex_init(&mutex->Mutex, &mutexAttributes);
    return ConvertErrorPlatformToPal(error);
}

int32_t SystemNative_LowLevelCrossProcessMutex_Acquire(LowLevelCrossProcessMutex* mutex, int32_t timeoutMilliseconds)
{
    int32_t result = AcquirePThreadMutexWithTimeout(&mutex->Mutex, timeoutMilliseconds);

    if (result == EOWNERDEAD)
    {
        // The mutex was abandoned by the previous owner.
        // Make it consistent so that it can be used again.
        int setConsistentResult = pthread_mutex_consistent(&mutex->Mutex);
    }

    return ConvertErrorPlatformToPal(result);
}

int32_t SystemNative_LowLevelCrossProcessMutex_Release(LowLevelCrossProcessMutex* mutex)
{
    assert(mutex != NULL);
    return ConvertErrorPlatformToPal(pthread_mutex_unlock(&mutex->Mutex));
}

int32_t SystemNative_LowLevelCrossProcessMutex_Destroy(LowLevelCrossProcessMutex* mutex)
{
    assert(mutex != NULL);
    return ConvertErrorPlatformToPal(pthread_mutex_destroy(&mutex->Mutex));
}

void SystemNative_LowLevelCrossProcessMutex_GetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t* pOwnerProcessId, uint32_t* pOwnerThreadId)
{
    assert(mutex != NULL);
    assert(pOwnerProcessId != NULL);
    assert(pOwnerThreadId != NULL);

    *pOwnerProcessId = mutex->OwnerProcessId;
    *pOwnerThreadId = mutex->OwnerThreadId;
}

void SystemNative_LowLevelCrossProcessMutex_SetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t ownerProcessId, uint32_t ownerThreadId)
{
    assert(mutex != NULL);

    mutex->OwnerProcessId = ownerProcessId;
    mutex->OwnerThreadId = ownerThreadId;
}

uint8_t SystemNative_LowLevelCrossProcessMutex_IsAbandoned(LowLevelCrossProcessMutex* mutex)
{
    assert(mutex != NULL);
    return mutex->IsAbandoned;
}

void SystemNative_LowLevelCrossProcessMutex_SetAbandoned(LowLevelCrossProcessMutex* mutex, uint8_t isAbandoned)
{
    assert(mutex != NULL);
    mutex->IsAbandoned = isAbandoned;
}
