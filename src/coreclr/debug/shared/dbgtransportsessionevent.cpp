// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dbgtransportsessionevent.h"

#ifdef HOST_WINDOWS

DbgTransportSessionEvent::DbgTransportSessionEvent()
    : m_event(CreateEventW(nullptr, TRUE, FALSE, nullptr))
{
}

DbgTransportSessionEvent::~DbgTransportSessionEvent()
{
    if (m_event != nullptr)
    {
        CloseHandle(m_event);
    }
}

bool DbgTransportSessionEvent::IsValid() const
{
    return m_event != nullptr;
}

bool DbgTransportSessionEvent::Set()
{
    if (!IsValid())
    {
        return false;
    }

    return SetEvent(m_event) != FALSE;
}

bool DbgTransportSessionEvent::Reset()
{
    if (!IsValid())
    {
        return false;
    }

    return ResetEvent(m_event) != FALSE;
}

DbgTransportSessionEvent::WaitResult DbgTransportSessionEvent::Wait(uint32_t timeoutMilliseconds)
{
    if (!IsValid())
    {
        return WaitResult::Failed;
    }

    DWORD result = WaitForSingleObject(m_event, timeoutMilliseconds);
    if (result == WAIT_OBJECT_0)
    {
        return WaitResult::Signaled;
    }

    return result == WAIT_TIMEOUT
        ? WaitResult::TimedOut
        : WaitResult::Failed;
}

#else // HOST_WINDOWS

#include <assert.h>
#include <errno.h>
#include <time.h>

namespace
{
    constexpr uint64_t NanosecondsPerMillisecond = 1000000;
    constexpr uint64_t NanosecondsPerSecond = 1000000000;

#ifdef HOST_OSX
    void NanosecondsToTimeSpec(uint64_t nanoseconds, timespec* time)
    {
        time->tv_sec = static_cast<time_t>(nanoseconds / NanosecondsPerSecond);
        time->tv_nsec = static_cast<long>(nanoseconds % NanosecondsPerSecond);
    }
#endif // HOST_OSX

#ifndef HOST_OSX
    bool GetDeadline(clockid_t clock, uint32_t timeoutMilliseconds, timespec* deadline)
    {
        if (clock_gettime(clock, deadline) != 0)
        {
            return false;
        }

        uint64_t nanoseconds =
            static_cast<uint64_t>(deadline->tv_nsec) +
            static_cast<uint64_t>(timeoutMilliseconds) * NanosecondsPerMillisecond;
        deadline->tv_sec += static_cast<time_t>(nanoseconds / NanosecondsPerSecond);
        deadline->tv_nsec = static_cast<long>(nanoseconds % NanosecondsPerSecond);
        return true;
    }
#endif // !HOST_OSX
}

DbgTransportSessionEvent::DbgTransportSessionEvent()
    : m_condition{}
    , m_mutex{}
    , m_generation(0)
    , m_conditionInitialized(false)
    , m_mutexInitialized(false)
    , m_signaled(false)
{
    pthread_condattr_t attributes;
    if (pthread_condattr_init(&attributes) != 0)
    {
        return;
    }

#ifdef DBG_TRANSPORT_HAS_PTHREAD_CONDATTR_SETCLOCK
    if (pthread_condattr_setclock(&attributes, CLOCK_MONOTONIC) != 0)
    {
        int error = pthread_condattr_destroy(&attributes);
        assert(error == 0);
        (void)error;
        return;
    }
#endif // DBG_TRANSPORT_HAS_PTHREAD_CONDATTR_SETCLOCK

    if (pthread_mutex_init(&m_mutex, nullptr) != 0)
    {
        int error = pthread_condattr_destroy(&attributes);
        assert(error == 0);
        (void)error;
        return;
    }

    m_mutexInitialized = true;
    if (pthread_cond_init(&m_condition, &attributes) == 0)
    {
        m_conditionInitialized = true;
    }

    int error = pthread_condattr_destroy(&attributes);
    assert(error == 0);
    (void)error;
}

DbgTransportSessionEvent::~DbgTransportSessionEvent()
{
    if (m_conditionInitialized)
    {
        int error = pthread_cond_destroy(&m_condition);
        assert(error == 0);
        (void)error;
    }

    if (m_mutexInitialized)
    {
        int error = pthread_mutex_destroy(&m_mutex);
        assert(error == 0);
        (void)error;
    }
}

bool DbgTransportSessionEvent::IsValid() const
{
    return m_conditionInitialized;
}

bool DbgTransportSessionEvent::Set()
{
    if (!IsValid())
    {
        return false;
    }

    int error = pthread_mutex_lock(&m_mutex);
    if (error != 0)
    {
        return false;
    }

    if (!m_signaled)
    {
        m_signaled = true;
        // PAL unregisters every current manual-reset-event waiter during SetEvent. Recording the
        // transition provides the same guarantee for condition-variable waiters after a racing Reset.
        m_generation++;
        error = pthread_cond_broadcast(&m_condition);
    }
    int unlockError = pthread_mutex_unlock(&m_mutex);
    assert(unlockError == 0);
    return error == 0 && unlockError == 0;
}

bool DbgTransportSessionEvent::Reset()
{
    if (!IsValid())
    {
        return false;
    }

    int error = pthread_mutex_lock(&m_mutex);
    if (error != 0)
    {
        return false;
    }

    m_signaled = false;
    error = pthread_mutex_unlock(&m_mutex);
    assert(error == 0);
    return error == 0;
}

DbgTransportSessionEvent::WaitResult DbgTransportSessionEvent::Wait(uint32_t timeoutMilliseconds)
{
    if (!IsValid())
    {
        return WaitResult::Failed;
    }

    constexpr uint32_t InfiniteTimeout = UINT32_MAX;
    timespec deadline;
#ifdef HOST_OSX
    uint64_t endNanoseconds = 0;
#endif // HOST_OSX
    if (timeoutMilliseconds != InfiniteTimeout)
    {
#ifdef HOST_OSX
        uint64_t timeoutNanoseconds =
            static_cast<uint64_t>(timeoutMilliseconds) * NanosecondsPerMillisecond;
        NanosecondsToTimeSpec(timeoutNanoseconds, &deadline);
        endNanoseconds = clock_gettime_nsec_np(CLOCK_UPTIME_RAW) + timeoutNanoseconds;
#else
#ifdef DBG_TRANSPORT_HAS_PTHREAD_CONDATTR_SETCLOCK
        constexpr clockid_t WaitClock = CLOCK_MONOTONIC;
#else
        constexpr clockid_t WaitClock = CLOCK_REALTIME;
#endif // DBG_TRANSPORT_HAS_PTHREAD_CONDATTR_SETCLOCK
        if (!GetDeadline(WaitClock, timeoutMilliseconds, &deadline))
        {
            return WaitResult::Failed;
        }
#endif // HOST_OSX
    }

    int error = pthread_mutex_lock(&m_mutex);
    if (error != 0)
    {
        return WaitResult::Failed;
    }

    // A generation change means this waiter was present for a Set, regardless of the current reset state.
    uint64_t generation = m_generation;
    while (!m_signaled && generation == m_generation)
    {
        error = timeoutMilliseconds == InfiniteTimeout
            ? pthread_cond_wait(&m_condition, &m_mutex)
#ifdef HOST_OSX
            : pthread_cond_timedwait_relative_np(&m_condition, &m_mutex, &deadline);
#else
            : pthread_cond_timedwait(&m_condition, &m_mutex, &deadline);
#endif // HOST_OSX
#ifdef HOST_OSX
        if (timeoutMilliseconds != InfiniteTimeout &&
            error == 0 &&
            !m_signaled &&
            generation == m_generation)
        {
            uint64_t currentNanoseconds = clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
            if (currentNanoseconds < endNanoseconds)
            {
                NanosecondsToTimeSpec(endNanoseconds - currentNanoseconds, &deadline);
            }
            else
            {
                error = ETIMEDOUT;
            }
        }
#endif // HOST_OSX
        if (error != 0)
        {
            break;
        }
    }

    bool signaled = m_signaled || generation != m_generation;
    int unlockError = pthread_mutex_unlock(&m_mutex);
    assert(unlockError == 0);
    if (unlockError != 0)
    {
        return WaitResult::Failed;
    }

    if (signaled)
    {
        return WaitResult::Signaled;
    }

    return error == ETIMEDOUT
        ? WaitResult::TimedOut
        : WaitResult::Failed;
}

#endif // HOST_WINDOWS
