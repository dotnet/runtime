// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../RuntimeEvent.h"

#include <windows.h>

void* PAL_CreateEvent(void* eventAttributes, bool manualReset, bool initialState)
{
    return CreateEventW(
        static_cast<LPSECURITY_ATTRIBUTES>(eventAttributes),
        manualReset,
        initialState,
        nullptr);
}

bool PAL_CloseEvent(void* event)
{
    return CloseHandle(event) != FALSE;
}

bool PAL_SetEvent(void* event)
{
    return SetEvent(event) != FALSE;
}

bool PAL_ResetEvent(void* event)
{
    return ResetEvent(event) != FALSE;
}

void PAL_Sleep(uint32_t milliseconds)
{
    Sleep(milliseconds);
}

uint32_t PAL_WaitForMultipleObjectsEx(
    uint32_t count,
    void* const* events,
    bool waitAll,
    uint32_t milliseconds,
    bool alertable)
{
    return WaitForMultipleObjectsEx(count, events, waitAll, milliseconds, alertable);
}
