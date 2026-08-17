// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>

#include "minipal-wait.h"

namespace
{
    HANDLE DuplicateNativeHandle(HANDLE handle)
    {
        if (handle == nullptr)
        {
            SetLastError(ERROR_INVALID_HANDLE);
            return nullptr;
        }

        HANDLE duplicate = nullptr;
        if (!DuplicateHandle(
                GetCurrentProcess(),
                handle,
                GetCurrentProcess(),
                &duplicate,
                0,
                FALSE,
                DUPLICATE_SAME_ACCESS))
        {
            return nullptr;
        }

        return duplicate;
    }
}

minipal_wait_handle::minipal_wait_handle(HANDLE handle)
    : m_handle(handle)
{
}

minipal_wait_handle::minipal_wait_handle(const minipal_wait_handle& handle)
    : minipal_wait_handle(DuplicateNativeHandle(handle.m_handle))
{
}

minipal_wait_handle::~minipal_wait_handle()
{
    if (m_handle != nullptr)
    {
        CloseHandle(m_handle);
    }
}

minipal_event::minipal_event(bool initialState)
    : minipal_wait_handle(CreateEventW(nullptr, FALSE, initialState, nullptr))
{
}

minipal_event::minipal_event(HANDLE handle)
    : minipal_wait_handle(DuplicateNativeHandle(handle))
{
}

bool minipal_event::Set()
{
    return SetEvent(GetRawHandle()) != FALSE;
}

bool minipal_event::Reset()
{
    return ResetEvent(GetRawHandle()) != FALSE;
}

minipal_process_wait::minipal_process_wait(uint32_t processId)
    : minipal_wait_handle(OpenProcess(SYNCHRONIZE, FALSE, processId))
{
}

minipal_process_wait::minipal_process_wait(HANDLE handle)
    : minipal_wait_handle(DuplicateNativeHandle(handle))
{
}

int32_t minipal_wait_handle::Wait(
    const minipal_wait_handle* const* handles,
    uint32_t count,
    uint32_t timeout)
{
    if (handles == nullptr || count == 0 || count > MINIPAL_MAX_WAIT_OBJECTS)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return MINIPAL_WAIT_FAILED;
    }

    HANDLE nativeHandles[MINIPAL_MAX_WAIT_OBJECTS];
    for (uint32_t index = 0; index < count; index++)
    {
        if (handles[index] == nullptr || !handles[index]->IsValid())
        {
            SetLastError(ERROR_INVALID_HANDLE);
            return MINIPAL_WAIT_FAILED;
        }

        nativeHandles[index] = handles[index]->m_handle;
    }

    DWORD result = WaitForMultipleObjectsEx(
        count,
        nativeHandles,
        FALSE,
        timeout,
        FALSE);

    if (result >= WAIT_OBJECT_0 && result < WAIT_OBJECT_0 + count)
    {
        return static_cast<int32_t>(result - WAIT_OBJECT_0);
    }

    if (result == WAIT_TIMEOUT)
    {
        return MINIPAL_WAIT_TIMEOUT;
    }

    return MINIPAL_WAIT_FAILED;
}
