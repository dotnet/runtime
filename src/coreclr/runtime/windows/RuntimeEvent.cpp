// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../RuntimeEvent.h"

#include <windows.h>

CLREventBase::CLREventBase()
    : m_handle(nullptr)
{
}

void* CLREventBase::CreateEvent(void* eventAttributes, bool manualReset, bool initialState)
{
    return CreateEventW(
        static_cast<LPSECURITY_ATTRIBUTES>(eventAttributes),
        manualReset,
        initialState,
        nullptr);
}

bool CLREventBase::CloseEvent(void* event)
{
    return CloseHandle(event) != FALSE;
}

bool CLREventBase::Set(void* event)
{
    return SetEvent(event) != FALSE;
}

bool CLREventBase::Reset(void* event)
{
    return ResetEvent(event) != FALSE;
}

uint32_t CLREventBase::Wait(void* event, uint32_t milliseconds, bool alertable)
{
    return WaitForMultipleObjectsEx(1, &event, false, milliseconds, alertable);
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
    return m_handle != nullptr && m_handle != INVALID_HANDLE_VALUE;
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
    return IsValid() ? Wait(m_handle, milliseconds) : WAIT_FAILED;
}

void* CLREventBase::GetOSEvent()
{
    return m_handle;
}

void PAL_Sleep(uint32_t milliseconds)
{
    Sleep(milliseconds);
}
