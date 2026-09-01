// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../RuntimeEvent.h"

#include <assert.h>
#include <errno.h>
#include <new>
#include <sched.h>
#include <stdint.h>
#include <time.h>
#include <unistd.h>

#include <minipal/conditionvariable.h>
#include <minipal/mutex.h>
#include <minipal/time.h>

static constexpr uint32_t Infinite = UINT32_MAX;
static constexpr uint32_t WaitFailed = UINT32_MAX;
static constexpr uint32_t WaitObject0 = 0;
static constexpr uint32_t WaitTimeout = 258;

struct EventData
{
    minipal_condition_variable condition;
    minipal_nonrecursive_mutex mutex;
    bool manualReset;
    bool state;
};

CLREventBase::CLREventBase()
    : m_handle(nullptr)
{
}

void* CLREventBase::CreateEvent(void* eventAttributes, bool manualReset, bool initialState)
{
    (void)eventAttributes;

    EventData* event = new (std::nothrow) EventData;
    if (event == nullptr)
    {
        return nullptr;
    }

    if (!minipal_nonrecursive_mutex_init(&event->mutex))
    {
        delete event;
        return nullptr;
    }

    if (!minipal_condition_variable_init(&event->condition))
    {
        minipal_nonrecursive_mutex_destroy(&event->mutex);
        delete event;
        return nullptr;
    }

    event->manualReset = manualReset;
    event->state = initialState;
    return event;
}

bool CLREventBase::CloseEvent(void* event)
{
    if (event == nullptr)
    {
        return false;
    }

    EventData* eventData = static_cast<EventData*>(event);
    minipal_condition_variable_destroy(&eventData->condition);
    minipal_nonrecursive_mutex_destroy(&eventData->mutex);
    delete eventData;
    return true;
}

bool CLREventBase::Set(void* event)
{
    if (event == nullptr)
    {
        return false;
    }

    EventData* eventData = static_cast<EventData*>(event);
    minipal_nonrecursive_mutex_enter(&eventData->mutex);
    eventData->state = true;
    bool success = eventData->manualReset
        ? minipal_condition_variable_broadcast(&eventData->condition)
        : minipal_condition_variable_signal(&eventData->condition);
    minipal_nonrecursive_mutex_leave(&eventData->mutex);
    return success;
}

bool CLREventBase::Reset(void* event)
{
    if (event == nullptr)
    {
        return false;
    }

    EventData* eventData = static_cast<EventData*>(event);
    minipal_nonrecursive_mutex_enter(&eventData->mutex);
    eventData->state = false;
    minipal_nonrecursive_mutex_leave(&eventData->mutex);
    return true;
}

uint32_t CLREventBase::Wait(void* event, uint32_t milliseconds)
{
    if (event == nullptr)
    {
        return WaitFailed;
    }

    EventData* eventData = static_cast<EventData*>(event);
    minipal_nonrecursive_mutex_enter(&eventData->mutex);

    uint32_t waitStatus = WaitObject0;
    uint32_t remainingMilliseconds = milliseconds;
    uint64_t startTicks = milliseconds == Infinite ? 0 : minipal_lowres_ticks();

    while (!eventData->state)
    {
#if defined(TARGET_WASM) && !defined(FEATURE_MULTITHREADING)
        if (milliseconds != 0)
        {
            assert(!"Cannot block on an event wait in single-threaded mode");
            waitStatus = WaitFailed;
            break;
        }
#endif

        minipal_condition_variable_result result = minipal_condition_variable_wait_nonrecursive(
            &eventData->condition,
            &eventData->mutex,
            remainingMilliseconds);

        if (result == MINIPAL_CONDITION_VARIABLE_TIMED_OUT)
        {
            waitStatus = WaitTimeout;
            break;
        }

        if (result == MINIPAL_CONDITION_VARIABLE_FAILED)
        {
            waitStatus = WaitFailed;
            break;
        }

        if (!eventData->state && milliseconds != Infinite)
        {
            uint64_t elapsedMilliseconds = minipal_lowres_ticks() - startTicks;
            if (elapsedMilliseconds >= milliseconds)
            {
                waitStatus = WaitTimeout;
                break;
            }

            remainingMilliseconds = milliseconds - static_cast<uint32_t>(elapsedMilliseconds);
        }
    }

    if (waitStatus == WaitObject0 && !eventData->manualReset)
    {
        eventData->state = false;
    }

    minipal_nonrecursive_mutex_leave(&eventData->mutex);
    return waitStatus;
}

bool CLREventBase::CreateAutoEventNoThrow(bool initialState)
{
    m_handle = CreateEvent(nullptr, false, initialState);
    return IsValid();
}

bool CLREventBase::CreateManualEventNoThrow(bool initialState)
{
    m_handle = CreateEvent(nullptr, true, initialState);
    return IsValid();
}

bool CLREventBase::CreateOSAutoEventNoThrow(bool initialState)
{
    return CreateAutoEventNoThrow(initialState);
}

bool CLREventBase::CreateOSManualEventNoThrow(bool initialState)
{
    return CreateManualEventNoThrow(initialState);
}

void CLREventBase::CloseEvent()
{
    if (IsValid())
    {
        CloseEvent(m_handle);
        m_handle = nullptr;
    }
}

bool CLREventBase::IsValid() const
{
    return m_handle != nullptr && m_handle != reinterpret_cast<void*>(-1);
}

bool CLREventBase::Set()
{
    return IsValid() && Set(m_handle);
}

bool CLREventBase::Reset()
{
    return IsValid() && Reset(m_handle);
}

uint32_t CLREventBase::Wait(uint32_t milliseconds)
{
    return IsValid() ? Wait(m_handle, milliseconds) : WaitFailed;
}

void* CLREventBase::GetOSEvent()
{
    return m_handle;
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
