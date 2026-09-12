// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../CLREventBase.h"

#include <windows.h>

CLREventBase::CLREventBase()
    : m_handle(nullptr)
{
}

bool CLREventBase::CreateEventNoThrow(bool manualReset, bool initialState)
{
    m_handle = CreateEventW(nullptr, manualReset, initialState, nullptr);
    return IsValid();
}

bool CLREventBase::CreateAutoEventNoThrow(bool initialState)
{
    return CreateEventNoThrow(false, initialState);
}

bool CLREventBase::CreateManualEventNoThrow(bool initialState)
{
    return CreateEventNoThrow(true, initialState);
}

bool CLREventBase::CreateFromOSHandle(void* osHandle)
{
    if (IsValid())
    {
        return false;
    }

    DWORD handleFlags;
    if (!GetHandleInformation(osHandle, &handleFlags))
    {
        return false;
    }

    return DuplicateHandle(
        GetCurrentProcess(),
        osHandle,
        GetCurrentProcess(),
        &m_handle,
        0,
        (handleFlags & HANDLE_FLAG_INHERIT) != 0,
        DUPLICATE_SAME_ACCESS) != FALSE;
}

void CLREventBase::CloseEvent()
{
    if (IsValid())
    {
        CloseHandle(m_handle);
        m_handle = nullptr;
    }
}

bool CLREventBase::IsValid() const
{
    return m_handle != nullptr && m_handle != INVALID_HANDLE_VALUE;
}

bool CLREventBase::Set()
{
    return IsValid() && SetEvent(m_handle) != FALSE;
}

bool CLREventBase::Reset()
{
    return IsValid() && ResetEvent(m_handle) != FALSE;
}

uint32_t CLREventBase::Wait(uint32_t milliseconds)
{
    return Wait(milliseconds, false);
}

uint32_t CLREventBase::Wait(uint32_t milliseconds, bool alertable)
{
    return IsValid() ? WaitForMultipleObjectsEx(1, &m_handle, false, milliseconds, alertable) : WAIT_FAILED;
}

void* CLREventBase::GetOSEvent()
{
    return m_handle;
}
