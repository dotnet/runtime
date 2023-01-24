// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//


#ifndef __Synch_h__
#define __Synch_h__

enum WaitMode
{
    WaitMode_None =0x0,
    WaitMode_Alertable = 0x1,         // Can be waken by APC.  May pumping message.
    WaitMode_IgnoreSyncCtx = 0x2,     // Dispatch to synchronization context if existed.
    WaitMode_InDeadlock = 0x4,        // The wait can be terminated by host's deadlock detection
};


struct PendingSync;

class CLREventBase
{
public:
    CLREventBase()
    {
        LIMITED_METHOD_CONTRACT;
        m_handle = INVALID_HANDLE_VALUE;
        m_dwFlags = 0;
    }

    // Create an Event that is host aware
    void CreateAutoEvent(BOOL bInitialState);
    void CreateManualEvent(BOOL bInitialState);

    // Non-throwing variants of the functions above
    BOOL CreateAutoEventNoThrow(BOOL bInitialState);
    BOOL CreateManualEventNoThrow(BOOL bInitialState);

    void CreateMonitorEvent(SIZE_T Cookie); // robust against initialization races - for exclusive use by AwareLock


    // Create an Event that is not host aware
    void CreateOSAutoEvent (BOOL bInitialState);
    void CreateOSManualEvent (BOOL bInitialState);

    // Non-throwing variants of the functions above
    BOOL CreateOSAutoEventNoThrow (BOOL bInitialState);
    BOOL CreateOSManualEventNoThrow (BOOL bInitialState);

    void CloseEvent();

    BOOL IsValid() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_handle != INVALID_HANDLE_VALUE;
    }

    BOOL IsMonitorEventAllocated()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwFlags & CLREVENT_FLAGS_MONITOREVENT_ALLOCATED;
    }

#ifndef DACCESS_COMPILE
    HANDLE GetHandleUNHOSTED() {
        LIMITED_METHOD_CONTRACT;
        return m_handle;
    }
#endif // DACCESS_COMPILE

    BOOL Set();
    void SetMonitorEvent(); // robust against races - for exclusive use by AwareLock
    BOOL Reset();
    DWORD Wait(DWORD dwMilliseconds, BOOL bAlertable, PendingSync *syncState=NULL);
    DWORD WaitEx(DWORD dwMilliseconds, WaitMode mode, PendingSync *syncState=NULL);

protected:
    HANDLE m_handle;

private:
    enum
    {
        CLREVENT_FLAGS_AUTO_EVENT = 0x0001,
        CLREVENT_FLAGS_OS_EVENT = 0x0002,
        CLREVENT_FLAGS_IN_DEADLOCK_DETECTION = 0x0004,

        CLREVENT_FLAGS_MONITOREVENT_ALLOCATED = 0x0008,
        CLREVENT_FLAGS_MONITOREVENT_SIGNALLED = 0x0010,

        CLREVENT_FLAGS_STATIC = 0x0020,

        // Several bits unused;
    };

    Volatile<DWORD> m_dwFlags;

    BOOL IsAutoEvent() { LIMITED_METHOD_CONTRACT; return m_dwFlags & CLREVENT_FLAGS_AUTO_EVENT; }
    void SetAutoEvent ()
    {
        LIMITED_METHOD_CONTRACT;
        // cannot use `|=' operator on `Volatile<DWORD>'
        m_dwFlags = m_dwFlags | CLREVENT_FLAGS_AUTO_EVENT;
    }
    BOOL IsOSEvent() { LIMITED_METHOD_CONTRACT; return m_dwFlags & CLREVENT_FLAGS_OS_EVENT; }
    void SetOSEvent ()
    {
        LIMITED_METHOD_CONTRACT;
        // cannot use `|=' operator on `Volatile<DWORD>'
        m_dwFlags = m_dwFlags | CLREVENT_FLAGS_OS_EVENT;
    }
    BOOL IsInDeadlockDetection() { LIMITED_METHOD_CONTRACT; return m_dwFlags & CLREVENT_FLAGS_IN_DEADLOCK_DETECTION; }
    void SetInDeadlockDetection ()
    {
        LIMITED_METHOD_CONTRACT;
        // cannot use `|=' operator on `Volatile<DWORD>'
        m_dwFlags = m_dwFlags | CLREVENT_FLAGS_IN_DEADLOCK_DETECTION;
    }
};


class CLREvent : public CLREventBase
{
public:

#ifndef DACCESS_COMPILE
    ~CLREvent()
    {
        WRAPPER_NO_CONTRACT;

        CloseEvent();
    }
#endif
};


// CLREventStatic
//   Same as CLREvent, but intended to be used for global variables.
//   Instances may leak their handle, because of the order in which
//   global destructors are run.  Note that you can still explicitly
//   call CloseHandle, which will indeed not leak the handle.
class CLREventStatic : public CLREventBase
{
};

BOOL CLREventWaitWithTry(CLREventBase *pEvent, DWORD timeout, BOOL fAlertable, DWORD *pStatus);
#endif
