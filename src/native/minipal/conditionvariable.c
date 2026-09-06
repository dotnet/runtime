// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(__APPLE__) && !defined(_DARWIN_C_SOURCE)
// pthread_cond_timedwait_relative_np is hidden by _XOPEN_SOURCE unless
// _DARWIN_C_SOURCE is defined before <pthread.h>.
#define _DARWIN_C_SOURCE
#endif

#include <assert.h>
#include <errno.h>
#include <time.h>

#include <minipal/conditionvariable.h>

#include "minipalconfig.h"

#define NANOSECONDS_PER_MILLISECOND 1000000
#define NANOSECONDS_PER_SECOND 1000000000

#if !defined(HOST_WINDOWS) && !HAVE_CLOCK_GETTIME_NSEC_NP
static bool GetDeadline(clockid_t clock, uint32_t timeoutMilliseconds, struct timespec* deadline)
{
    if (clock_gettime(clock, deadline) != 0)
    {
        return false;
    }

    uint64_t nanoseconds =
        (uint64_t)deadline->tv_nsec +
        (uint64_t)timeoutMilliseconds * NANOSECONDS_PER_MILLISECOND;
    deadline->tv_sec += (time_t)(nanoseconds / NANOSECONDS_PER_SECOND);
    deadline->tv_nsec = (long)(nanoseconds % NANOSECONDS_PER_SECOND);
    return true;
}
#endif // !HOST_WINDOWS && !HAVE_CLOCK_GETTIME_NSEC_NP

bool minipal_condition_variable_init(minipal_condition_variable* condition)
{
    assert(condition != NULL);
#ifdef HOST_WINDOWS
    InitializeConditionVariable(&condition->_impl);
    return true;
#else
    pthread_condattr_t attributes;
    if (pthread_condattr_init(&attributes) != 0)
    {
        return false;
    }

#if HAVE_PTHREAD_CONDATTR_SETCLOCK
    if (pthread_condattr_setclock(&attributes, CLOCK_MONOTONIC) != 0)
    {
        int error = pthread_condattr_destroy(&attributes);
        assert(error == 0);
        (void)error;
        return false;
    }
#endif // HAVE_PTHREAD_CONDATTR_SETCLOCK

    bool success = pthread_cond_init(&condition->_impl, &attributes) == 0;
    int error = pthread_condattr_destroy(&attributes);
    assert(error == 0);
    (void)error;
    return success;
#endif // HOST_WINDOWS
}

void minipal_condition_variable_destroy(minipal_condition_variable* condition)
{
    assert(condition != NULL);
#ifndef HOST_WINDOWS
    int error = pthread_cond_destroy(&condition->_impl);
    assert(error == 0);
    (void)error;
#else
    (void)condition;
#endif // !HOST_WINDOWS
}

bool minipal_condition_variable_broadcast(minipal_condition_variable* condition)
{
    assert(condition != NULL);
#ifdef HOST_WINDOWS
    WakeAllConditionVariable(&condition->_impl);
    return true;
#else
    return pthread_cond_broadcast(&condition->_impl) == 0;
#endif // HOST_WINDOWS
}

bool minipal_condition_variable_signal(minipal_condition_variable* condition)
{
    assert(condition != NULL);
#ifdef HOST_WINDOWS
    WakeConditionVariable(&condition->_impl);
    return true;
#else
    return pthread_cond_signal(&condition->_impl) == 0;
#endif // HOST_WINDOWS
}

#ifndef HOST_WINDOWS
static minipal_condition_variable_result minipal_condition_variable_wait_pthread(
    minipal_condition_variable* condition,
    pthread_mutex_t* mutex,
    uint32_t timeoutMilliseconds)
{
    int error;
    if (timeoutMilliseconds == MINIPAL_CONDITION_VARIABLE_INFINITE)
    {
        error = pthread_cond_wait(&condition->_impl, mutex);
    }
    else
    {
        struct timespec timeout;
#if HAVE_CLOCK_GETTIME_NSEC_NP
        uint64_t nanoseconds = (uint64_t)timeoutMilliseconds * NANOSECONDS_PER_MILLISECOND;
        timeout.tv_sec = (time_t)(nanoseconds / NANOSECONDS_PER_SECOND);
        timeout.tv_nsec = (long)(nanoseconds % NANOSECONDS_PER_SECOND);
        error = pthread_cond_timedwait_relative_np(&condition->_impl, mutex, &timeout);
#else
#if HAVE_PTHREAD_CONDATTR_SETCLOCK
        const clockid_t waitClock = CLOCK_MONOTONIC;
#else
        const clockid_t waitClock = CLOCK_REALTIME;
#endif // HAVE_PTHREAD_CONDATTR_SETCLOCK
        if (!GetDeadline(waitClock, timeoutMilliseconds, &timeout))
        {
            return MINIPAL_CONDITION_VARIABLE_FAILED;
        }
        error = pthread_cond_timedwait(&condition->_impl, mutex, &timeout);
#endif // HAVE_CLOCK_GETTIME_NSEC_NP
    }

    if (error == 0)
    {
        return MINIPAL_CONDITION_VARIABLE_SIGNALED;
    }

    return error == ETIMEDOUT
        ? MINIPAL_CONDITION_VARIABLE_TIMED_OUT
        : MINIPAL_CONDITION_VARIABLE_FAILED;
}
#endif // !HOST_WINDOWS

minipal_condition_variable_result minipal_condition_variable_wait(
    minipal_condition_variable* condition,
    minipal_mutex* mutex,
    uint32_t timeoutMilliseconds)
{
    assert(condition != NULL);
    assert(mutex != NULL);

#ifdef HOST_WINDOWS
    if (SleepConditionVariableCS(&condition->_impl, &mutex->_impl, timeoutMilliseconds) != FALSE)
    {
        return MINIPAL_CONDITION_VARIABLE_SIGNALED;
    }

    return GetLastError() == ERROR_TIMEOUT
        ? MINIPAL_CONDITION_VARIABLE_TIMED_OUT
        : MINIPAL_CONDITION_VARIABLE_FAILED;
#else
    return minipal_condition_variable_wait_pthread(condition, &mutex->_impl, timeoutMilliseconds);
#endif // HOST_WINDOWS
}

minipal_condition_variable_result minipal_condition_variable_wait_nonrecursive(
    minipal_condition_variable* condition,
    minipal_nonrecursive_mutex* mutex,
    uint32_t timeoutMilliseconds)
{
    assert(condition != NULL);
    assert(mutex != NULL);

#ifdef HOST_WINDOWS
    if (SleepConditionVariableSRW(&condition->_impl, &mutex->_impl, timeoutMilliseconds, 0) != FALSE)
    {
        return MINIPAL_CONDITION_VARIABLE_SIGNALED;
    }

    return GetLastError() == ERROR_TIMEOUT
        ? MINIPAL_CONDITION_VARIABLE_TIMED_OUT
        : MINIPAL_CONDITION_VARIABLE_FAILED;
#else
    return minipal_condition_variable_wait_pthread(condition, &mutex->_impl, timeoutMilliseconds);
#endif // HOST_WINDOWS
}
