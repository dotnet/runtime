// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DBGTRANSPORTSESSIONEVENT_H__
#define __DBGTRANSPORTSESSIONEVENT_H__

#include <stdint.h>

#ifdef HOST_WINDOWS
#include <windows.h>
#else
#include <pthread.h>
#endif // HOST_WINDOWS

class DbgTransportSessionEvent final
{
public:
    enum class WaitResult
    {
        Signaled,
        TimedOut,
        Failed,
    };

    DbgTransportSessionEvent();
    ~DbgTransportSessionEvent();

    bool IsValid() const;
    bool Set();
    bool Reset();
    WaitResult Wait(uint32_t timeoutMilliseconds);

    DbgTransportSessionEvent(const DbgTransportSessionEvent&) = delete;
    DbgTransportSessionEvent& operator=(const DbgTransportSessionEvent&) = delete;

private:
#ifdef HOST_WINDOWS
    HANDLE m_event;
#else
    pthread_cond_t m_condition;
    pthread_mutex_t m_mutex;
    // Allows waiters released by Set to complete even if Reset runs before they reacquire the mutex.
    uint64_t m_generation;
    bool m_conditionInitialized;
    bool m_mutexInitialized;
    bool m_signaled;
#endif // HOST_WINDOWS
};

#endif // __DBGTRANSPORTSESSIONEVENT_H__
