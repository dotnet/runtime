// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <errno.h>
#include <stdint.h>
#include <stdio.h>
#include <time.h>

#include "dbgtransportsessionevent.h"

namespace
{
    struct WaiterState
    {
        DbgTransportSessionEvent* event;
        DbgTransportSessionEvent::WaitResult result;
    };

    bool Check(bool condition, const char* message)
    {
        if (!condition)
        {
            fprintf(stderr, "FAILED: %s\n", message);
        }

        return condition;
    }

    void SleepMilliseconds(uint32_t milliseconds)
    {
#ifdef HOST_WINDOWS
        Sleep(milliseconds);
#else
        timespec delay = {};
        delay.tv_sec = milliseconds / 1000;
        delay.tv_nsec = static_cast<long>(milliseconds % 1000) * 1000000;
        while (nanosleep(&delay, &delay) != 0 && errno == EINTR);
#endif // HOST_WINDOWS
    }

#ifdef HOST_WINDOWS
    DWORD WINAPI WaiterThread(void* argument)
#else
    void* WaiterThread(void* argument)
#endif
    {
        WaiterState* state = static_cast<WaiterState*>(argument);
        state->result = state->event->Wait(UINT32_MAX);
#ifdef HOST_WINDOWS
        return 0;
#else
        return nullptr;
#endif // HOST_WINDOWS
    }

    bool TestManualResetEvent()
    {
        DbgTransportSessionEvent event;
        if (!Check(event.IsValid(), "create session event"))
        {
            return false;
        }

        WaiterState states[] =
        {
            { &event, DbgTransportSessionEvent::WaitResult::Failed },
            { &event, DbgTransportSessionEvent::WaitResult::Failed },
        };

#ifdef HOST_WINDOWS
        HANDLE threads[] =
        {
            CreateThread(nullptr, 0, WaiterThread, &states[0], 0, nullptr),
            CreateThread(nullptr, 0, WaiterThread, &states[1], 0, nullptr),
        };
        bool threadsStarted = threads[0] != nullptr && threads[1] != nullptr;
#else
        pthread_t threads[2];
        bool threadStarted[] =
        {
            pthread_create(&threads[0], nullptr, WaiterThread, &states[0]) == 0,
            pthread_create(&threads[1], nullptr, WaiterThread, &states[1]) == 0,
        };
        bool threadsStarted = threadStarted[0] && threadStarted[1];
#endif // HOST_WINDOWS

        SleepMilliseconds(50);
        bool success =
            Check(
                event.Wait(0) == DbgTransportSessionEvent::WaitResult::TimedOut,
                "initial session event is reset") &&
            Check(threadsStarted, "start session event waiters");

        success = Check(event.Set(), "set session event") && success;

#ifdef HOST_WINDOWS
        for (HANDLE thread : threads)
        {
            if (thread != nullptr)
            {
                success =
                    Check(WaitForSingleObject(thread, 5000) == WAIT_OBJECT_0, "join session event waiter") &&
                    success;
                CloseHandle(thread);
            }
        }
#else
        for (int index = 0; index < 2; index++)
        {
            if (threadStarted[index])
            {
                success =
                    Check(pthread_join(threads[index], nullptr) == 0, "join session event waiter") &&
                    success;
            }
        }
#endif // HOST_WINDOWS

        success =
            Check(
                states[0].result == DbgTransportSessionEvent::WaitResult::Signaled &&
                states[1].result == DbgTransportSessionEvent::WaitResult::Signaled,
                "session event releases every waiter") &&
            Check(
                event.Wait(0) == DbgTransportSessionEvent::WaitResult::Signaled,
                "session event remains signaled") &&
            Check(
                event.Wait(0) == DbgTransportSessionEvent::WaitResult::Signaled,
                "session event remains signaled for subsequent waits") &&
            Check(event.Reset(), "reset session event") &&
            Check(
                event.Wait(50) == DbgTransportSessionEvent::WaitResult::TimedOut,
                "reset session event times out") &&
            success;

        return success;
    }
}

int main()
{
    if (!TestManualResetEvent())
    {
        return 1;
    }

    printf("DbgTransportSessionEvent tests passed.\n");
    return 0;
}
