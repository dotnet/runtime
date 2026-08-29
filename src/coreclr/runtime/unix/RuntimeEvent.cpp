// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../RuntimeEvent.h"

#include <assert.h>
#include <errno.h>
#include <new>
#include <pthread.h>
#include <sched.h>
#include <stdint.h>
#include <time.h>
#include <unistd.h>

#include "minipalconfig.h"

static constexpr uint32_t Infinite = UINT32_MAX;
static constexpr uint32_t MillisecondsToNanoseconds = 1000000;
static constexpr uint32_t NanosecondsPerSecond = 1000000000;
static constexpr uint32_t WaitFailed = UINT32_MAX;
static constexpr uint32_t WaitObject0 = 0;
static constexpr uint32_t WaitTimeout = 258;

static void AddMilliseconds(timespec* time, uint32_t milliseconds)
{
    uint64_t nanoseconds = time->tv_nsec + static_cast<uint64_t>(milliseconds) * MillisecondsToNanoseconds;
    if (nanoseconds >= NanosecondsPerSecond)
    {
        time->tv_sec += nanoseconds / NanosecondsPerSecond;
        nanoseconds %= NanosecondsPerSecond;
    }

    time->tv_nsec = nanoseconds;
}

static void NanosecondsToTimeSpec(uint64_t nanoseconds, timespec* time)
{
    time->tv_sec = nanoseconds / NanosecondsPerSecond;
    time->tv_nsec = nanoseconds % NanosecondsPerSecond;
}

class RuntimeEvent
{
    pthread_cond_t m_condition;
    pthread_mutex_t m_mutex;
    bool m_manualReset;
    bool m_state;
    bool m_isValid;

public:
    RuntimeEvent(bool manualReset, bool initialState)
        : m_manualReset(manualReset),
          m_state(initialState),
          m_isValid(false)
    {
    }

    bool Initialize()
    {
        pthread_condattr_t attributes;
        int status = pthread_condattr_init(&attributes);
        if (status != 0)
        {
            return false;
        }

        bool success = false;

#if HAVE_PTHREAD_CONDATTR_SETCLOCK && !HAVE_CLOCK_GETTIME_NSEC_NP
        status = pthread_condattr_setclock(&attributes, CLOCK_MONOTONIC);
        if (status != 0)
        {
            goto Exit;
        }
#endif // HAVE_PTHREAD_CONDATTR_SETCLOCK && !HAVE_CLOCK_GETTIME_NSEC_NP

        status = pthread_mutex_init(&m_mutex, nullptr);
        if (status != 0)
        {
            goto Exit;
        }

        status = pthread_cond_init(&m_condition, &attributes);
        if (status != 0)
        {
            status = pthread_mutex_destroy(&m_mutex);
            assert(status == 0);
            goto Exit;
        }

        m_isValid = true;
        success = true;

    Exit:
        status = pthread_condattr_destroy(&attributes);
        assert(status == 0);
        return success;
    }

    bool Destroy()
    {
        if (!m_isValid)
        {
            return true;
        }

        int conditionStatus = pthread_cond_destroy(&m_condition);
        assert(conditionStatus == 0);

        int mutexStatus = pthread_mutex_destroy(&m_mutex);
        assert(mutexStatus == 0);

        m_isValid = false;
        return conditionStatus == 0 && mutexStatus == 0;
    }

    uint32_t Wait(uint32_t milliseconds)
    {
        timespec endTime;
#if HAVE_CLOCK_GETTIME_NSEC_NP
        uint64_t endNanoseconds = 0;
        if (milliseconds != Infinite)
        {
            uint64_t nanoseconds = static_cast<uint64_t>(milliseconds) * MillisecondsToNanoseconds;
            NanosecondsToTimeSpec(nanoseconds, &endTime);
            endNanoseconds = clock_gettime_nsec_np(CLOCK_UPTIME_RAW) + nanoseconds;
        }
#elif HAVE_PTHREAD_CONDATTR_SETCLOCK
        if (milliseconds != Infinite)
        {
            clock_gettime(CLOCK_MONOTONIC, &endTime);
            AddMilliseconds(&endTime, milliseconds);
        }
#else
#error "Don't know how to perform timed wait on this platform"
#endif

        int status = pthread_mutex_lock(&m_mutex);
        if (status != 0)
        {
            return WaitFailed;
        }

        while (!m_state)
        {
            if (milliseconds == Infinite)
            {
                status = pthread_cond_wait(&m_condition, &m_mutex);
            }
            else
            {
#if HAVE_CLOCK_GETTIME_NSEC_NP
                status = pthread_cond_timedwait_relative_np(&m_condition, &m_mutex, &endTime);
                if (status == 0 && !m_state)
                {
                    uint64_t currentNanoseconds = clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
                    if (currentNanoseconds < endNanoseconds)
                    {
                        NanosecondsToTimeSpec(endNanoseconds - currentNanoseconds, &endTime);
                    }
                    else
                    {
                        status = ETIMEDOUT;
                    }
                }
#else // HAVE_CLOCK_GETTIME_NSEC_NP
                status = pthread_cond_timedwait(&m_condition, &m_mutex, &endTime);
#endif // HAVE_CLOCK_GETTIME_NSEC_NP
            }

            if (status != 0)
            {
                break;
            }
        }

        if (status == 0 && !m_manualReset)
        {
            m_state = false;
        }

        int unlockStatus = pthread_mutex_unlock(&m_mutex);
        if (unlockStatus != 0)
        {
            return WaitFailed;
        }

        if (status == 0)
        {
            return WaitObject0;
        }

        return status == ETIMEDOUT ? WaitTimeout : WaitFailed;
    }

    bool Set()
    {
        int status = pthread_mutex_lock(&m_mutex);
        if (status != 0)
        {
            return false;
        }

        m_state = true;
        status = pthread_cond_broadcast(&m_condition);
        int unlockStatus = pthread_mutex_unlock(&m_mutex);
        return status == 0 && unlockStatus == 0;
    }

    bool Reset()
    {
        int status = pthread_mutex_lock(&m_mutex);
        if (status != 0)
        {
            return false;
        }

        m_state = false;
        return pthread_mutex_unlock(&m_mutex) == 0;
    }
};

void* PAL_CreateEvent(void* eventAttributes, bool manualReset, bool initialState)
{
    (void)eventAttributes;

    RuntimeEvent* event = new (std::nothrow) RuntimeEvent(manualReset, initialState);
    if (event == nullptr)
    {
        return nullptr;
    }

    if (!event->Initialize())
    {
        delete event;
        return nullptr;
    }

    return event;
}

bool PAL_CloseEvent(void* event)
{
    if (event == nullptr)
    {
        return false;
    }

    RuntimeEvent* runtimeEvent = static_cast<RuntimeEvent*>(event);
    bool success = runtimeEvent->Destroy();
    delete runtimeEvent;
    return success;
}

bool PAL_SetEvent(void* event)
{
    if (event == nullptr)
    {
        return false;
    }

    return static_cast<RuntimeEvent*>(event)->Set();
}

bool PAL_ResetEvent(void* event)
{
    if (event == nullptr)
    {
        return false;
    }

    return static_cast<RuntimeEvent*>(event)->Reset();
}

static void SleepMicroseconds(uint64_t microseconds)
{
    timespec requested;
    requested.tv_sec = microseconds / 1000000;
    requested.tv_nsec = (microseconds % 1000000) * 1000;

    timespec remaining;
    while (nanosleep(&requested, &remaining) != 0 && errno == EINTR)
    {
        requested = remaining;
    }
}

void PAL_Sleep(uint32_t milliseconds)
{
#if defined(TARGET_WASM) && !defined(FEATURE_MULTITHREADING)
    (void)milliseconds;
    return;
#endif

    if (milliseconds == 0)
    {
        sched_yield();
        return;
    }

    if (milliseconds == Infinite)
    {
        while (true)
        {
            usleep(999000);
        }
    }

    SleepMicroseconds(static_cast<uint64_t>(milliseconds) * 1000);
}

uint32_t PAL_WaitForMultipleObjectsEx(
    uint32_t count,
    void* const* events,
    bool waitAll,
    uint32_t milliseconds,
    bool alertable)
{
    (void)alertable;

    assert(count == 1);
    assert(!waitAll);

    if (count != 1 || events == nullptr || events[0] == nullptr || waitAll)
    {
        return WaitFailed;
    }

#if defined(TARGET_WASM) && !defined(FEATURE_MULTITHREADING)
    if (milliseconds != 0)
    {
        assert(!"Cannot block on an event wait in single-threaded mode");
        return WaitFailed;
    }
#endif

    return static_cast<RuntimeEvent*>(events[0])->Wait(milliseconds);
}
