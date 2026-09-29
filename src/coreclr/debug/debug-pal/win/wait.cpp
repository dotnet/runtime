// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>

#include "debugwait.h"

namespace
{
    constexpr uint32_t MaxWaitHandles = 64;

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

WaitHandle::WaitHandle(HANDLE handle)
    : m_handle(handle)
{
}

WaitHandle::WaitHandle(const WaitHandle& handle)
    : WaitHandle(DuplicateNativeHandle(handle.m_handle))
{
}

WaitHandle::~WaitHandle()
{
    if (m_handle != nullptr)
    {
        CloseHandle(m_handle);
    }
}

WaitEvent::WaitEvent(bool initialState)
    : WaitHandle(CreateEventW(nullptr, FALSE, initialState, nullptr))
{
}

WaitEvent::WaitEvent(HANDLE handle)
    : WaitHandle(DuplicateNativeHandle(handle))
{
}

bool WaitEvent::Set()
{
    return SetEvent(GetRawHandle()) != FALSE;
}

bool WaitEvent::Reset()
{
    return ResetEvent(GetRawHandle()) != FALSE;
}

WaitLatch::WaitLatch()
    : WaitHandle(CreateEventW(nullptr, TRUE, FALSE, nullptr))
{
}

bool WaitLatch::Set()
{
    return SetEvent(GetRawHandle()) != FALSE;
}

NativeHandle::NativeHandle(HANDLE handle)
    : WaitHandle(DuplicateNativeHandle(handle))
{
}

int32_t WaitHandle::Wait(
    const WaitHandle* const* handles,
    uint32_t count,
    uint32_t timeout)
{
    if (handles == nullptr || count == 0 || count > MaxWaitHandles)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return Failed;
    }

    HANDLE nativeHandles[MaxWaitHandles];
    for (uint32_t index = 0; index < count; index++)
    {
        if (handles[index] == nullptr || !handles[index]->IsValid())
        {
            SetLastError(ERROR_INVALID_HANDLE);
            return Failed;
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
        return Timeout;
    }

    return Failed;
}
