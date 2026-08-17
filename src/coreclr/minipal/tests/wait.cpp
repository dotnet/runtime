// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <errno.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <new>

#ifdef HOST_WINDOWS
#include <windows.h>
#else
#include <pthread.h>
#include <signal.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <time.h>
#include <unistd.h>
#endif

#include "minipal-wait.h"

#if defined(MINIPAL_WAIT_TESTS)
extern "C" void minipal_wait_test_pause_process_watchers(bool pause);
extern "C" void minipal_wait_test_pause_exited_process_watchers(bool pause);
extern "C" int32_t minipal_wait_test_get_process_watcher_count();
extern "C" int32_t minipal_wait_test_get_paused_process_watcher_count();
extern "C" int32_t minipal_wait_test_get_paused_exited_process_watcher_count();
extern "C" int32_t minipal_wait_test_get_process_watcher_signal_count();
extern "C" void minipal_wait_test_reset_process_watcher_signal_count();
#endif // MINIPAL_WAIT_TESTS

namespace
{
    struct WaitThreadState
    {
        minipal_event handle;
        uint32_t timeout;
        int32_t result;

        WaitThreadState(const minipal_event& waitHandle, uint32_t waitTimeout)
            : handle(waitHandle)
            , timeout(waitTimeout)
            , result(MINIPAL_WAIT_FAILED)
        {
        }
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
#endif
    }

    uint64_t GetTickMilliseconds()
    {
#ifdef HOST_WINDOWS
        return GetTickCount64();
#else
        timespec currentTime;
        if (clock_gettime(CLOCK_MONOTONIC, &currentTime) != 0)
        {
            return 0;
        }

        return static_cast<uint64_t>(currentTime.tv_sec) * 1000 +
            static_cast<uint64_t>(currentTime.tv_nsec) / 1000000;
#endif
    }

#if defined(MINIPAL_WAIT_TESTS)
    struct ProcessWaitReleaseState
    {
        minipal_process_wait* process;
        int32_t released;
    };

    bool WaitForValue(int32_t (*getValue)(), int32_t expected, uint32_t timeout)
    {
        uint64_t start = GetTickMilliseconds();
        do
        {
            if (getValue() == expected)
            {
                return true;
            }

            SleepMilliseconds(1);
        } while (GetTickMilliseconds() - start < timeout);

        return getValue() == expected;
    }

    void* ReleaseProcessWait(void* argument)
    {
        ProcessWaitReleaseState* state = static_cast<ProcessWaitReleaseState*>(argument);
        delete state->process;
        __atomic_store_n(&state->released, 1, __ATOMIC_RELEASE);
        return nullptr;
    }
#endif // MINIPAL_WAIT_TESTS

#ifdef HOST_WINDOWS
    DWORD WINAPI WaitThread(void* argument)
#else
    void* WaitThread(void* argument)
#endif
    {
        WaitThreadState* state = static_cast<WaitThreadState*>(argument);
        state->result = minipal_wait_handle::Wait(state->handle, state->timeout);
#ifdef HOST_WINDOWS
        return 0;
#else
        return nullptr;
#endif
    }

    bool TestAutoResetEvent()
    {
        minipal_event event(false);
        minipal_event initiallySignaledEvent(true);
        bool success =
            Check(event.IsValid() && initiallySignaledEvent.IsValid(), "create auto-reset events") &&
            Check(
                minipal_wait_handle::Wait(initiallySignaledEvent, 0) == 0,
                "initially signaled auto-reset event is acquired") &&
            Check(
                minipal_wait_handle::Wait(initiallySignaledEvent, 0) == MINIPAL_WAIT_TIMEOUT,
                "initially signaled auto-reset event is consumed") &&
            Check(
                minipal_wait_handle::Wait(event, 0) == MINIPAL_WAIT_TIMEOUT,
                "unsignaled auto-reset event times out") &&
            Check(event.Set(), "set auto-reset event before reset") &&
            Check(event.Reset(), "reset auto-reset event") &&
            Check(
                minipal_wait_handle::Wait(event, 0) == MINIPAL_WAIT_TIMEOUT,
                "reset auto-reset event times out") &&
            Check(event.Set(), "set auto-reset event") &&
            Check(event.Set(), "coalesce repeated auto-reset set") &&
            Check(
                minipal_wait_handle::Wait(event, 0) == 0,
                "signaled auto-reset event is acquired") &&
            Check(
                minipal_wait_handle::Wait(event, 0) == MINIPAL_WAIT_TIMEOUT,
                "auto-reset event is consumed");

        return success;
    }

    bool TestLowestIndexPriority()
    {
        minipal_event events[3] =
        {
            minipal_event(false),
            minipal_event(false),
            minipal_event(false),
        };
        const minipal_wait_handle* waitSet[] = { &events[0], &events[1], &events[2] };

        bool success =
            Check(events[0].IsValid() && events[1].IsValid() && events[2].IsValid(), "create priority events") &&
            Check(events[2].Set(), "set high-index event") &&
            Check(events[0].Set(), "set low-index event") &&
            Check(
                minipal_wait_handle::Wait(waitSet, 3, 0) == 0,
                "lowest signaled index wins") &&
            Check(
                minipal_wait_handle::Wait(waitSet, 3, 0) == 2,
                "higher index remains signaled");

        return success;
    }

    bool TestTimeout()
    {
        minipal_event event(false);
        if (!Check(event.IsValid(), "create timeout event"))
        {
            return false;
        }

        uint64_t start = GetTickMilliseconds();
        int32_t result = minipal_wait_handle::Wait(event, 100);
        uint64_t elapsed = GetTickMilliseconds() - start;

        bool success =
            Check(result == MINIPAL_WAIT_TIMEOUT, "finite wait times out") &&
            Check(elapsed >= 75, "finite wait observes requested timeout") &&
            Check(elapsed < 2000, "finite wait does not overrun");

        return success;
    }

    bool TestAutoResetReleasesSingleWaiter()
    {
        minipal_event event(false);
        WaitThreadState states[2] =
        {
            { event, 2000 },
            { event, 2000 },
        };

#ifdef HOST_WINDOWS
        HANDLE threads[2] =
        {
            CreateThread(nullptr, 0, WaitThread, &states[0], 0, nullptr),
            CreateThread(nullptr, 0, WaitThread, &states[1], 0, nullptr),
        };
        bool threadsStarted = threads[0] != nullptr && threads[1] != nullptr;
#else
        pthread_t threads[2];
        bool threadStarted[2] =
        {
            pthread_create(&threads[0], nullptr, WaitThread, &states[0]) == 0,
            pthread_create(&threads[1], nullptr, WaitThread, &states[1]) == 0,
        };
        bool threadsStarted = threadStarted[0] && threadStarted[1];
#endif

        SleepMilliseconds(50);
        bool success =
            Check(event.IsValid() && states[0].handle.IsValid() && states[1].handle.IsValid(),
                "create shared auto-reset event") &&
            Check(threadsStarted, "start auto-reset waiters") &&
            Check(event.Set(), "signal one auto-reset waiter");

#ifdef HOST_WINDOWS
        for (HANDLE thread : threads)
        {
            if (thread != nullptr)
            {
                success =
                    Check(WaitForSingleObject(thread, 5000) == WAIT_OBJECT_0, "join auto-reset waiter") &&
                    success;
                CloseHandle(thread);
            }
        }
#else
        for (int index = 0; index < 2; index++)
        {
            if (threadStarted[index])
            {
                success = Check(pthread_join(threads[index], nullptr) == 0, "join auto-reset waiter") && success;
            }
        }
#endif

        int acquiredCount = (states[0].result == 0 ? 1 : 0) + (states[1].result == 0 ? 1 : 0);
        int timeoutCount =
            (states[0].result == MINIPAL_WAIT_TIMEOUT ? 1 : 0) +
            (states[1].result == MINIPAL_WAIT_TIMEOUT ? 1 : 0);
        success =
            Check(acquiredCount == 1, "exactly one waiter acquires auto-reset event") &&
            Check(timeoutCount == 1, "other auto-reset waiter times out") &&
            success;

        return success;
    }

    bool TestDuplicateLifetime()
    {
        minipal_event* duplicate;
        {
            minipal_event event(false);
            duplicate = new (std::nothrow) minipal_event(event);
        }

        bool success =
            Check(duplicate != nullptr && duplicate->IsValid(), "duplicate event") &&
            Check(duplicate->Set(), "set duplicate after original release") &&
            Check(
                minipal_wait_handle::Wait(*duplicate, 0) == 0,
                "duplicate keeps event alive");

        delete duplicate;
        return success;
    }

    bool TestInvalidWaitSet()
    {
        minipal_event event(false);
        minipal_process_wait invalidProcess(static_cast<uint32_t>(0));
        const minipal_wait_handle* handles[] = { &event, nullptr };

        bool success =
            Check(event.IsValid(), "create invalid-wait event") &&
            Check(!invalidProcess.IsValid(), "reject invalid process identifier") &&
            Check(
                minipal_wait_handle::Wait(invalidProcess, 0) == MINIPAL_WAIT_FAILED,
                "reject invalid single wait handle") &&
            Check(
                minipal_wait_handle::Wait(handles, 2, 0) == MINIPAL_WAIT_FAILED,
                "reject null wait-set entry") &&
            Check(
                minipal_wait_handle::Wait(nullptr, 1, 0) == MINIPAL_WAIT_FAILED,
                "reject null wait set") &&
            Check(
                minipal_wait_handle::Wait(handles, 0, 0) == MINIPAL_WAIT_FAILED,
                "reject empty wait set") &&
            Check(
                minipal_wait_handle::Wait(handles, MINIPAL_MAX_WAIT_OBJECTS + 1, 0) ==
                    MINIPAL_WAIT_FAILED,
                "reject oversized wait set");

        return success;
    }

    bool TestConcurrentWait()
    {
        minipal_event event(false);
        WaitThreadState state(event, MINIPAL_WAIT_INFINITE);

#ifdef HOST_WINDOWS
        HANDLE thread = CreateThread(nullptr, 0, WaitThread, &state, 0, nullptr);
        bool threadStarted = thread != nullptr;
#else
        pthread_t thread;
        bool threadStarted = pthread_create(&thread, nullptr, WaitThread, &state) == 0;
#endif

        SleepMilliseconds(50);
        bool success =
            Check(event.IsValid() && state.handle.IsValid(), "create concurrent event") &&
            Check(threadStarted, "start waiting thread") &&
            Check(event.Set(), "signal waiting thread");

        if (threadStarted)
        {
#ifdef HOST_WINDOWS
            success =
                Check(WaitForSingleObject(thread, 5000) == WAIT_OBJECT_0, "join waiting thread") &&
                success;
            CloseHandle(thread);
#else
            success = Check(pthread_join(thread, nullptr) == 0, "join waiting thread") && success;
#endif
        }

        success = Check(state.result == 0, "waiting thread acquires event") && success;
        return success;
    }

#ifndef HOST_WINDOWS
    void InterruptSignalHandler(int)
    {
    }

    bool TestInterruptedWait()
    {
        struct sigaction action = {};
        action.sa_handler = InterruptSignalHandler;
        sigemptyset(&action.sa_mask);
        if (!Check(sigaction(SIGUSR1, &action, nullptr) == 0, "install interrupt handler"))
        {
            return false;
        }

        minipal_event event(false);
        WaitThreadState state(event, MINIPAL_WAIT_INFINITE);
        pthread_t thread;
        bool threadStarted = pthread_create(&thread, nullptr, WaitThread, &state) == 0;

        SleepMilliseconds(50);
        bool success =
            Check(event.IsValid() && state.handle.IsValid(), "create interrupted event") &&
            Check(threadStarted, "start interrupted waiting thread");

        if (threadStarted)
        {
            success = Check(pthread_kill(thread, SIGUSR1) == 0, "interrupt waiting thread") && success;
            SleepMilliseconds(50);
            success = Check(event.Set(), "signal interrupted wait") && success;
            success = Check(pthread_join(thread, nullptr) == 0, "join interrupted waiting thread") && success;
        }

        success = Check(state.result == 0, "interrupted wait retries") && success;
        return success;
    }
#endif

#if defined(MINIPAL_WAIT_TESTS)
    bool TestAbandonedProcessWatcher()
    {
        pid_t child = fork();
        if (!Check(child >= 0, "fork abandoned process-watcher child"))
        {
            return false;
        }

        if (child == 0)
        {
            SleepMilliseconds(30000);
            _exit(0);
        }

        minipal_wait_test_pause_process_watchers(true);

        minipal_process_wait* process =
            new (std::nothrow) minipal_process_wait(static_cast<uint32_t>(child));

        bool watcherPaused =
            process != nullptr &&
            process->IsValid() &&
            WaitForValue(minipal_wait_test_get_paused_process_watcher_count, 1, 2000);
        bool success =
            Check(process != nullptr && process->IsValid(), "create abandoned process watcher") &&
            Check(watcherPaused, "pause process watcher");

        minipal_process_wait* duplicate = watcherPaused
            ? new (std::nothrow) minipal_process_wait(*process)
            : nullptr;
        success =
            Check(duplicate != nullptr && duplicate->IsValid(), "duplicate process watcher") &&
            success;

        if (duplicate != nullptr)
        {
            delete process;
            process = duplicate;
            success =
                Check(
                    minipal_wait_test_get_process_watcher_count() == 1,
                    "process watcher remains while public reference exists") &&
                success;
        }

        ProcessWaitReleaseState state = { process, 0 };
        pthread_t releaseThread;
        bool releaseThreadStarted =
            watcherPaused &&
            pthread_create(&releaseThread, nullptr, ReleaseProcessWait, &state) == 0;
        success = Check(releaseThreadStarted, "start process-wait release thread") && success;

        bool releasedWithoutWaiting = false;
        if (releaseThreadStarted)
        {
            uint64_t start = GetTickMilliseconds();
            do
            {
                releasedWithoutWaiting = __atomic_load_n(&state.released, __ATOMIC_ACQUIRE) != 0;
                if (!releasedWithoutWaiting)
                {
                    SleepMilliseconds(1);
                }
            } while (!releasedWithoutWaiting && GetTickMilliseconds() - start < 2000);
        }

        success =
            Check(releasedWithoutWaiting, "final public release does not join process watcher") &&
            success;

        minipal_wait_test_pause_process_watchers(false);

        if (releaseThreadStarted)
        {
            success = Check(pthread_join(releaseThread, nullptr) == 0, "join process-wait release thread") && success;
        }
        else
        {
            delete state.process;
        }

        success =
            Check(
                WaitForValue(minipal_wait_test_get_process_watcher_count, 0, 2000),
                "abandoned process watcher exits") &&
            success;

        int status;
        pid_t waitResult = waitpid(child, &status, WNOHANG);
        success = Check(waitResult == 0, "abandoned process remains running") && success;
        if (waitResult == 0)
        {
            success = Check(kill(child, SIGKILL) == 0, "terminate abandoned process-watcher child") && success;
        }

        do
        {
            waitResult = waitpid(child, &status, 0);
        } while (waitResult < 0 && errno == EINTR);

        success =
            Check(waitResult == child || (waitResult < 0 && errno == ECHILD), "collect abandoned child process") &&
            success;
        return success;
    }

    bool TestAbandonedExitedProcessWatcher()
    {
        pid_t child = fork();
        if (!Check(child >= 0, "fork exited process-watcher child"))
        {
            return false;
        }

        if (child == 0)
        {
            SleepMilliseconds(100);
            _exit(0);
        }

        minipal_wait_test_reset_process_watcher_signal_count();
        minipal_wait_test_pause_exited_process_watchers(true);

        ProcessWaitReleaseState state =
        {
            new (std::nothrow) minipal_process_wait(static_cast<uint32_t>(child)),
            0,
        };

        bool watcherPaused =
            state.process != nullptr &&
            state.process->IsValid() &&
            WaitForValue(minipal_wait_test_get_paused_exited_process_watcher_count, 1, 2000);
        bool success =
            Check(state.process != nullptr && state.process->IsValid(), "create exited process watcher") &&
            Check(watcherPaused, "pause exited process watcher before signaling");

        pthread_t releaseThread;
        bool releaseThreadStarted =
            watcherPaused &&
            pthread_create(&releaseThread, nullptr, ReleaseProcessWait, &state) == 0;
        success = Check(releaseThreadStarted, "start exited process-wait release thread") && success;

        bool releasedWithoutWaiting = false;
        if (releaseThreadStarted)
        {
            uint64_t start = GetTickMilliseconds();
            do
            {
                releasedWithoutWaiting = __atomic_load_n(&state.released, __ATOMIC_ACQUIRE) != 0;
                if (!releasedWithoutWaiting)
                {
                    SleepMilliseconds(1);
                }
            } while (!releasedWithoutWaiting && GetTickMilliseconds() - start < 2000);
        }

        success =
            Check(releasedWithoutWaiting, "final public release does not join exited process watcher") &&
            success;

        minipal_wait_test_pause_exited_process_watchers(false);

        if (releaseThreadStarted)
        {
            success = Check(pthread_join(releaseThread, nullptr) == 0, "join exited process-wait release thread") && success;
        }
        else
        {
            delete state.process;
        }

        success =
            Check(
                WaitForValue(minipal_wait_test_get_process_watcher_count, 0, 2000),
                "abandoned exited process watcher exits") &&
            Check(
                minipal_wait_test_get_process_watcher_signal_count() == 0,
                "abandoned exited process watcher does not signal") &&
            success;

        int status;
        pid_t waitResult;
        do
        {
            waitResult = waitpid(child, &status, 0);
        } while (waitResult < 0 && errno == EINTR);

        success =
            Check(waitResult == child || (waitResult < 0 && errno == ECHILD), "collect exited child process") &&
            success;
        return success;
    }
#endif // MINIPAL_WAIT_TESTS

    bool TestProcessExit(const char* executablePath)
    {
#ifdef HOST_WINDOWS
        char commandLine[MAX_PATH + 32];
        snprintf(commandLine, sizeof(commandLine), "\"%s\" --child", executablePath);

        STARTUPINFOA startupInfo = {};
        startupInfo.cb = sizeof(startupInfo);
        PROCESS_INFORMATION processInfo = {};
        if (!Check(
                CreateProcessA(
                    executablePath,
                    commandLine,
                    nullptr,
                    nullptr,
                    FALSE,
                    0,
                    nullptr,
                    nullptr,
                    &startupInfo,
                    &processInfo) != FALSE,
                "create child process"))
        {
            return false;
        }

        minipal_process_wait process(processInfo.dwProcessId);
        minipal_event event(false);
        const minipal_wait_handle* handles[] = { &event, &process };
        int32_t result = !process.IsValid() || !event.IsValid()
            ? MINIPAL_WAIT_FAILED
            : minipal_wait_handle::Wait(handles, 2, 5000);
        bool success =
            Check(process.IsValid() && event.IsValid(), "create process waitables") &&
            Check(result == 1, "observe process exit in wait set") &&
            Check(event.Set(), "set event after process exit") &&
            Check(
                minipal_wait_handle::Wait(handles, 2, 0) == 0,
                "event has priority over exited process") &&
            Check(
                minipal_wait_handle::Wait(process, 0) == 0,
                "process exit remains signaled");

        CloseHandle(processInfo.hThread);
        CloseHandle(processInfo.hProcess);
        return success;
#else
        (void)executablePath;
        pid_t child = fork();
        if (!Check(child >= 0, "fork child process"))
        {
            return false;
        }

        if (child == 0)
        {
            SleepMilliseconds(100);
            _exit(0);
        }

        minipal_process_wait process(static_cast<uint32_t>(child));
        minipal_event event(false);
        const minipal_wait_handle* handles[] = { &event, &process };
        int32_t result = !process.IsValid() || !event.IsValid()
            ? MINIPAL_WAIT_FAILED
            : minipal_wait_handle::Wait(handles, 2, 5000);
        bool success =
            Check(process.IsValid() && event.IsValid(), "create process waitables") &&
            Check(result == 1, "observe process exit in wait set") &&
            Check(event.Set(), "set event after process exit") &&
            Check(
                minipal_wait_handle::Wait(handles, 2, 0) == 0,
                "event has priority over exited process") &&
            Check(
                minipal_wait_handle::Wait(process, 0) == 0,
                "process exit remains signaled");

        int status;
        pid_t waitResult;
        do
        {
            waitResult = waitpid(child, &status, 0);
        } while (waitResult < 0 && errno == EINTR);
        success =
            Check(waitResult == child || (waitResult < 0 && errno == ECHILD), "collect child process") &&
            success;
        return success;
#endif
    }
}

int main(int argc, char** argv)
{
    if (argc == 2 && strcmp(argv[1], "--child") == 0)
    {
        SleepMilliseconds(100);
        return 0;
    }

    bool success =
        TestAutoResetEvent() &&
        TestLowestIndexPriority() &&
        TestTimeout() &&
        TestDuplicateLifetime() &&
        TestInvalidWaitSet() &&
        TestConcurrentWait() &&
        TestAutoResetReleasesSingleWaiter() &&
#ifndef HOST_WINDOWS
        TestInterruptedWait() &&
#endif
        TestProcessExit(argv[0]);

#if defined(MINIPAL_WAIT_TESTS)
    success = TestAbandonedProcessWatcher() && success;
    success = TestAbandonedExitedProcessWatcher() && success;
#endif // MINIPAL_WAIT_TESTS

    if (success)
    {
        printf("All minipal wait tests passed.\n");
        return 0;
    }

    return 1;
}
