// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RUNTIME_EVENT_H__
#define __RUNTIME_EVENT_H__

#include <stdint.h>

void* PAL_CreateEvent(void* eventAttributes, bool manualReset, bool initialState);
bool PAL_CloseEvent(void* event);
bool PAL_SetEvent(void* event);
bool PAL_ResetEvent(void* event);
void PAL_Sleep(uint32_t milliseconds);

// Unix supports a single event and waitAll must be false. Windows forwards to the OS implementation.
uint32_t PAL_WaitForMultipleObjectsEx(
    uint32_t count,
    void* const* events,
    bool waitAll,
    uint32_t milliseconds,
    bool alertable);

#endif // __RUNTIME_EVENT_H__
