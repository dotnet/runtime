// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RUNTIME_EVENT_H__
#define __RUNTIME_EVENT_H__

#include <stdint.h>

class CLREventBase
{
public:
    CLREventBase();

    void CreateAutoEvent(bool initialState);
    void CreateManualEvent(bool initialState);
    bool CreateAutoEventNoThrow(bool initialState);
    bool CreateManualEventNoThrow(bool initialState);
    bool CreateOSAutoEventNoThrow(bool initialState);
    bool CreateOSManualEventNoThrow(bool initialState);

    void CloseEvent();
    bool IsValid() const;
    bool Set();
    bool Reset();

    uint32_t Wait(uint32_t milliseconds);
    uint32_t Wait(uint32_t milliseconds, bool alertable, bool allowReentrantWait = false);
    uint32_t WaitEx(uint32_t milliseconds, uint32_t mode);

    void* GetOSEvent();

    static void* CreateEvent(void* eventAttributes, bool manualReset, bool initialState);
    static bool CloseEvent(void* event);
    static bool Set(void* event);
    static bool Reset(void* event);
#ifdef HOST_WINDOWS
    static uint32_t Wait(void* event, uint32_t milliseconds, bool alertable = false);
#else
    static uint32_t Wait(void* event, uint32_t milliseconds);
#endif

protected:
    void* m_handle;
};

void PAL_Sleep(uint32_t milliseconds);

#endif // __RUNTIME_EVENT_H__
