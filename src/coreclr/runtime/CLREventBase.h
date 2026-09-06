// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CLR_EVENT_BASE_H__
#define __CLR_EVENT_BASE_H__

#include <stdint.h>

class CLREventBase
{
public:
    CLREventBase();

    void CreateAutoEvent(bool initialState);
    void CreateManualEvent(bool initialState);
    bool CreateAutoEventNoThrow(bool initialState);
    bool CreateManualEventNoThrow(bool initialState);
#ifdef HOST_WINDOWS
    bool CreateFromOSHandle(void* osHandle);
#endif

    void CloseEvent();
    bool IsValid() const;
    bool Set();
    bool Reset();

    uint32_t Wait(uint32_t milliseconds);
    uint32_t Wait(uint32_t milliseconds, bool alertable);
    uint32_t Wait(uint32_t milliseconds, bool alertable, bool allowReentrantWait);
    uint32_t WaitEx(uint32_t milliseconds, uint32_t mode);

    void* GetOSEvent();

private:
    bool CreateEventNoThrow(bool manualReset, bool initialState);

protected:
    void* m_handle;
};

#endif // __CLR_EVENT_BASE_H__
