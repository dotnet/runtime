// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <errno.h>
#include <stdint.h>
#include <stdio.h>
#include <time.h>

#include <minipal/conditionvariable.h>

namespace
{
    enum class TestState
    {
        Opening,
        Open,
        Closed,
    };

    struct WaiterState
    {
        minipal_condition_variable* condition;
        minipal_mutex* mutex;
        TestState* state;
        TestState result;
    };

    struct SignalWaiterState
    {
        minipal_condition_variable* condition;
        minipal_nonrecursive_mutex* mutex;
        int* readyCount;
        int* wakeTokens;
        int* wakeCount;
        bool* waitFailed;
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
        WaiterState* waiter = static_cast<WaiterState*>(argument);
        minipal_mutex_enter(waiter->mutex);
        while (*waiter->state == TestState::Opening)
        {
            minipal_condition_variable_result result =
                minipal_condition_variable_wait(
                    waiter->condition,
                    waiter->mutex,
                    MINIPAL_CONDITION_VARIABLE_INFINITE);
            if (result == MINIPAL_CONDITION_VARIABLE_FAILED)
            {
                break;
            }
        }
        waiter->result = *waiter->state;
        minipal_mutex_leave(waiter->mutex);

#ifdef HOST_WINDOWS
        return 0;
#else
        return nullptr;
#endif // HOST_WINDOWS
    }

#ifdef HOST_WINDOWS
    DWORD WINAPI SignalWaiterThread(void* argument)
#else
    void* SignalWaiterThread(void* argument)
#endif
    {
        SignalWaiterState* waiter = static_cast<SignalWaiterState*>(argument);
        minipal_nonrecursive_mutex_enter(waiter->mutex);
        (*waiter->readyCount)++;
        while (*waiter->wakeTokens == 0)
        {
            minipal_condition_variable_result result =
                minipal_condition_variable_wait_nonrecursive(
                    waiter->condition,
                    waiter->mutex,
                    MINIPAL_CONDITION_VARIABLE_INFINITE);
            if (result == MINIPAL_CONDITION_VARIABLE_FAILED)
            {
                *waiter->waitFailed = true;
                break;
            }
        }
        if (*waiter->wakeTokens != 0)
        {
            (*waiter->wakeTokens)--;
            (*waiter->wakeCount)++;
        }
        minipal_nonrecursive_mutex_leave(waiter->mutex);

#ifdef HOST_WINDOWS
        return 0;
#else
        return nullptr;
#endif // HOST_WINDOWS
    }

    bool TestSignalCondition()
    {
        minipal_nonrecursive_mutex mutex;
        if (!Check(minipal_nonrecursive_mutex_init(&mutex), "create non-recursive mutex"))
        {
            return false;
        }

        minipal_condition_variable condition;
        if (!Check(minipal_condition_variable_init(&condition), "create signal condition"))
        {
            minipal_nonrecursive_mutex_destroy(&mutex);
            return false;
        }

        int readyCount = 0;
        int wakeTokens = 0;
        int wakeCount = 0;
        bool waitFailed = false;
        SignalWaiterState waiters[] =
        {
            { &condition, &mutex, &readyCount, &wakeTokens, &wakeCount, &waitFailed },
            { &condition, &mutex, &readyCount, &wakeTokens, &wakeCount, &waitFailed },
        };

#ifdef HOST_WINDOWS
        HANDLE threads[] =
        {
            CreateThread(nullptr, 0, SignalWaiterThread, &waiters[0], 0, nullptr),
            CreateThread(nullptr, 0, SignalWaiterThread, &waiters[1], 0, nullptr),
        };
        bool threadStarted[] = { threads[0] != nullptr, threads[1] != nullptr };
#else
        pthread_t threads[2];
        bool threadStarted[] =
        {
            pthread_create(&threads[0], nullptr, SignalWaiterThread, &waiters[0]) == 0,
            pthread_create(&threads[1], nullptr, SignalWaiterThread, &waiters[1]) == 0,
        };
#endif // HOST_WINDOWS

        int startedCount = (threadStarted[0] ? 1 : 0) + (threadStarted[1] ? 1 : 0);
        for (int retry = 0; retry < 2000; retry++)
        {
            minipal_nonrecursive_mutex_enter(&mutex);
            int currentReadyCount = readyCount;
            minipal_nonrecursive_mutex_leave(&mutex);
            if (currentReadyCount == startedCount)
            {
                break;
            }
            SleepMilliseconds(1);
        }

        minipal_nonrecursive_mutex_enter(&mutex);
        bool waitersReady = readyCount == 2;
        wakeTokens = 1;
        bool signaled = minipal_condition_variable_signal(&condition);
        minipal_nonrecursive_mutex_leave(&mutex);

        for (int retry = 0; retry < 2000; retry++)
        {
            minipal_nonrecursive_mutex_enter(&mutex);
            int currentWakeCount = wakeCount;
            minipal_nonrecursive_mutex_leave(&mutex);
            if (currentWakeCount == 1)
            {
                break;
            }
            SleepMilliseconds(1);
        }

        minipal_nonrecursive_mutex_enter(&mutex);
        bool oneWaiterWoke = wakeCount == 1;
        wakeTokens += startedCount - wakeCount;
        bool broadcast = minipal_condition_variable_broadcast(&condition);
        minipal_nonrecursive_mutex_leave(&mutex);

        bool success =
            Check(startedCount == 2, "start signal waiters") &&
            Check(waitersReady, "signal waiters are ready") &&
            Check(signaled, "signal one waiter") &&
            Check(oneWaiterWoke, "exactly one waiter satisfies the signaled predicate") &&
            Check(!waitFailed, "condition wait succeeds") &&
            Check(broadcast, "broadcast remaining waiter");

        for (int index = 0; index < 2; index++)
        {
            if (threadStarted[index])
            {
#ifdef HOST_WINDOWS
                success =
                    Check(WaitForSingleObject(threads[index], 5000) == WAIT_OBJECT_0, "join signal waiter") &&
                    success;
                CloseHandle(threads[index]);
#else
                success = Check(pthread_join(threads[index], nullptr) == 0, "join signal waiter") && success;
#endif // HOST_WINDOWS
            }
        }

        minipal_nonrecursive_mutex_enter(&mutex);
        success = Check(wakeCount == startedCount, "all signal waiters wake") && success;
        minipal_nonrecursive_mutex_leave(&mutex);

        minipal_condition_variable_destroy(&condition);
        minipal_nonrecursive_mutex_destroy(&mutex);
        return success;
    }

    bool TestStateCondition()
    {
        minipal_mutex mutex;
        if (!Check(minipal_mutex_init(&mutex), "create state mutex"))
        {
            return false;
        }

        minipal_condition_variable condition;
        if (!Check(minipal_condition_variable_init(&condition), "create state condition"))
        {
            minipal_mutex_destroy(&mutex);
            return false;
        }

        TestState state = TestState::Opening;
        WaiterState waiters[] =
        {
            { &condition, &mutex, &state, TestState::Opening },
            { &condition, &mutex, &state, TestState::Opening },
        };

#ifdef HOST_WINDOWS
        HANDLE threads[] =
        {
            CreateThread(nullptr, 0, WaiterThread, &waiters[0], 0, nullptr),
            CreateThread(nullptr, 0, WaiterThread, &waiters[1], 0, nullptr),
        };
        bool threadsStarted = threads[0] != nullptr && threads[1] != nullptr;
#else
        pthread_t threads[2];
        bool threadStarted[] =
        {
            pthread_create(&threads[0], nullptr, WaiterThread, &waiters[0]) == 0,
            pthread_create(&threads[1], nullptr, WaiterThread, &waiters[1]) == 0,
        };
        bool threadsStarted = threadStarted[0] && threadStarted[1];
#endif // HOST_WINDOWS

        SleepMilliseconds(50);
        minipal_mutex_enter(&mutex);
        state = TestState::Open;
        bool broadcast = minipal_condition_variable_broadcast(&condition);
        minipal_mutex_leave(&mutex);

        bool success =
            Check(threadsStarted, "start state condition waiters") &&
            Check(broadcast, "broadcast open state");

#ifdef HOST_WINDOWS
        for (HANDLE thread : threads)
        {
            if (thread != nullptr)
            {
                success =
                    Check(WaitForSingleObject(thread, 5000) == WAIT_OBJECT_0, "join state condition waiter") &&
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
                    Check(pthread_join(threads[index], nullptr) == 0, "join state condition waiter") &&
                    success;
            }
        }
#endif // HOST_WINDOWS

        success =
            Check(
                waiters[0].result == TestState::Open && waiters[1].result == TestState::Open,
                "all waiters observe open state") &&
            success;

        minipal_mutex_enter(&mutex);
        state = TestState::Opening;
        minipal_condition_variable_result timeout =
            minipal_condition_variable_wait(&condition, &mutex, 50);
        minipal_mutex_leave(&mutex);

        success =         Check(timeout == MINIPAL_CONDITION_VARIABLE_TIMED_OUT, "state condition times out") && success;

        WaiterState closedWaiter = { &condition, &mutex, &state, TestState::Opening };
#ifdef HOST_WINDOWS
        HANDLE closedThread = CreateThread(nullptr, 0, WaiterThread, &closedWaiter, 0, nullptr);
        bool closedThreadStarted = closedThread != nullptr;
#else
        pthread_t closedThread;
        bool closedThreadStarted = pthread_create(&closedThread, nullptr, WaiterThread, &closedWaiter) == 0;
#endif // HOST_WINDOWS

        SleepMilliseconds(50);
        minipal_mutex_enter(&mutex);
        state = TestState::Closed;
        broadcast = minipal_condition_variable_broadcast(&condition);
        minipal_mutex_leave(&mutex);

        success =
            Check(closedThreadStarted, "start closed-state waiter") &&
            Check(broadcast, "broadcast closed state") &&
            success;

        if (closedThreadStarted)
        {
#ifdef HOST_WINDOWS
            success =
                Check(WaitForSingleObject(closedThread, 5000) == WAIT_OBJECT_0, "join closed-state waiter") &&
                success;
            CloseHandle(closedThread);
#else
            success =
                Check(pthread_join(closedThread, nullptr) == 0, "join closed-state waiter") &&
                success;
#endif // HOST_WINDOWS
        }

        success = Check(closedWaiter.result == TestState::Closed, "waiter observes closed state") && success;

        minipal_condition_variable_destroy(&condition);
        minipal_mutex_destroy(&mutex);
        return success;
    }
}

int main()
{
    if (!TestSignalCondition() ||
        !TestStateCondition())
    {
        return 1;
    }

    printf("DbgTransport session state condition tests passed.\n");
    return 0;
}
