// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../CLREventBase.h"

#include <assert.h>
#include <new>
#include <stdint.h>

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

bool CLREventBase::CreateEventNoThrow(bool manualReset, bool initialState)
{
    EventData* event = new (std::nothrow) EventData;
    if (event == nullptr)
    {
        return false;
    }

    if (!minipal_nonrecursive_mutex_init(&event->mutex))
    {
        delete event;
        return false;
    }

    if (!minipal_condition_variable_init(&event->condition))
    {
        minipal_nonrecursive_mutex_destroy(&event->mutex);
        delete event;
        return false;
    }

    event->manualReset = manualReset;
    event->state = initialState;
    m_handle = event;
    return true;
}

uint32_t CLREventBase::Wait(uint32_t milliseconds, bool alertable)
{
    (void)alertable;

    if (!IsValid())
    {
        return WaitFailed;
    }

    EventData* eventData = static_cast<EventData*>(m_handle);
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
    return CreateEventNoThrow(false, initialState);
}

bool CLREventBase::CreateManualEventNoThrow(bool initialState)
{
    return CreateEventNoThrow(true, initialState);
}

void CLREventBase::CloseEvent()
{
    if (IsValid())
    {
        EventData* eventData = static_cast<EventData*>(m_handle);
        minipal_condition_variable_destroy(&eventData->condition);
        minipal_nonrecursive_mutex_destroy(&eventData->mutex);
        delete eventData;
        m_handle = nullptr;
    }
}

bool CLREventBase::IsValid() const
{
    return m_handle != nullptr && m_handle != reinterpret_cast<void*>(-1);
}

bool CLREventBase::Set()
{
    if (!IsValid())
    {
        return false;
    }

    EventData* eventData = static_cast<EventData*>(m_handle);
    minipal_nonrecursive_mutex_enter(&eventData->mutex);
    eventData->state = true;
    bool success = eventData->manualReset
        ? minipal_condition_variable_broadcast(&eventData->condition)
        : minipal_condition_variable_signal(&eventData->condition);
    minipal_nonrecursive_mutex_leave(&eventData->mutex);
    return success;
}

bool CLREventBase::Reset()
{
    if (!IsValid())
    {
        return false;
    }

    EventData* eventData = static_cast<EventData*>(m_handle);
    minipal_nonrecursive_mutex_enter(&eventData->mutex);
    eventData->state = false;
    minipal_nonrecursive_mutex_leave(&eventData->mutex);
    return true;
}

uint32_t CLREventBase::Wait(uint32_t milliseconds)
{
    return Wait(milliseconds, false);
}

void* CLREventBase::GetOSEvent()
{
    return m_handle;
}
