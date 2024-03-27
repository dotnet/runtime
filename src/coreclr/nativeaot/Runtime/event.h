// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENT_H__
#define __EVENT_H__

class CLREventStatic
{
public:
    bool CreateManualEventNoThrow(bool bInitialState);
    bool CreateAutoEventNoThrow(bool bInitialState);
    bool CreateOSManualEventNoThrow(bool bInitialState);
    bool CreateOSAutoEventNoThrow(bool bInitialState);
    void CloseEvent();
    bool IsValid() const;
    bool Set();
    bool Reset();
    uint32_t Wait(uint32_t dwMilliseconds, bool bAlertable, bool bAllowReentrantWait = false);
    HANDLE GetOSEvent();

private:
    HANDLE  m_hEvent;
    bool    m_fInitialized;
};

#endif // __EVENT_H__
