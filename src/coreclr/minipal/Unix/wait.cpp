// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <errno.h>
#include <fcntl.h>
#include <limits.h>
#include <new>
#include <poll.h>
#include <pthread.h>
#include <signal.h>
#include <stdint.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <time.h>
#include <unistd.h>

#if defined(TARGET_LINUX)
#include <sys/syscall.h>
#endif // TARGET_LINUX

#if defined(TARGET_APPLE) || defined(TARGET_FREEBSD) || defined(TARGET_NETBSD) || defined(TARGET_OPENBSD)
#include <sys/event.h>
#define MINIPAL_WAIT_USES_KQUEUE 1
#else
#define MINIPAL_WAIT_USES_KQUEUE 0
#endif // kqueue platforms

#include <minipal/mutex.h>

#include "minipal-wait.h"

namespace
{
#if defined(MINIPAL_WAIT_TESTS)
    int32_t s_processWatcherCount;
    int32_t s_pauseExitedProcessWatchers;
    int32_t s_pausedProcessWatcherCount;
    int32_t s_pausedExitedProcessWatcherCount;
    int32_t s_pauseProcessWatchers;
    int32_t s_processWatcherSignalCount;
#endif // MINIPAL_WAIT_TESTS

    enum class WaitableKind
    {
        Event,
        ProcessFileDescriptor,
        ProcessKqueue,
        ProcessPipe,
    };

    enum class AcquireResult
    {
        Failed,
        NotReady,
        Ready,
    };

    struct Waitable
    {
        int32_t refCount;
        int32_t publicRefCount;
        WaitableKind kind;
        minipal_mutex mutex;
        bool mutexInitialized;
        bool manualReset;
        bool signaled;
        int readFileDescriptor;
        int writeFileDescriptor;
        int processFileDescriptor;
        int errorCode;
        pid_t processId;
    };

    void CloseFileDescriptor(int fileDescriptor)
    {
        if (fileDescriptor >= 0)
        {
            close(fileDescriptor);
        }
    }

    bool SetCloseOnExec(int fileDescriptor)
    {
        int descriptorFlags;
        do
        {
            descriptorFlags = fcntl(fileDescriptor, F_GETFD);
        } while (descriptorFlags < 0 && errno == EINTR);

        if (descriptorFlags < 0)
        {
            return false;
        }

        int result;
        do
        {
            result = fcntl(fileDescriptor, F_SETFD, descriptorFlags | FD_CLOEXEC);
        } while (result < 0 && errno == EINTR);

        return result == 0;
    }

    bool SetPipeFlags(int fileDescriptor)
    {
        int statusFlags;
        do
        {
            statusFlags = fcntl(fileDescriptor, F_GETFL);
        } while (statusFlags < 0 && errno == EINTR);

        if (statusFlags < 0)
        {
            return false;
        }

        int result;
        do
        {
            result = fcntl(fileDescriptor, F_SETFL, statusFlags | O_NONBLOCK);
        } while (result < 0 && errno == EINTR);

        return result == 0 && SetCloseOnExec(fileDescriptor);
    }

    bool CreatePipe(int fileDescriptors[2])
    {
#if HAVE_PIPE2
        int result;
        do
        {
            result = pipe2(fileDescriptors, O_CLOEXEC | O_NONBLOCK);
        } while (result < 0 && errno == EINTR);

        if (result == 0)
        {
            return true;
        }

        if (errno != ENOSYS && errno != EINVAL)
        {
            return false;
        }
#else
        int result;
#endif // HAVE_PIPE2
        do
        {
            result = pipe(fileDescriptors);
        } while (result < 0 && errno == EINTR);

        if (result < 0)
        {
            return false;
        }

        if (!SetPipeFlags(fileDescriptors[0]) ||
            !SetPipeFlags(fileDescriptors[1]))
        {
            CloseFileDescriptor(fileDescriptors[0]);
            CloseFileDescriptor(fileDescriptors[1]);
            return false;
        }

        return true;
    }

    Waitable* AllocateWaitable(WaitableKind kind)
    {
        Waitable* waitable = new (std::nothrow) Waitable{};
        if (waitable == nullptr)
        {
            return nullptr;
        }

        waitable->refCount = 1;
        waitable->publicRefCount = 1;
        waitable->kind = kind;
        waitable->readFileDescriptor = -1;
        waitable->writeFileDescriptor = -1;
        waitable->processFileDescriptor = -1;

        if (!minipal_mutex_init(&waitable->mutex))
        {
            delete waitable;
            return nullptr;
        }

        waitable->mutexInitialized = true;
        return waitable;
    }

    void DestroyWaitable(Waitable* waitable)
    {
        assert(__atomic_load_n(&waitable->refCount, __ATOMIC_RELAXED) == 0);
        assert(__atomic_load_n(&waitable->publicRefCount, __ATOMIC_RELAXED) == 0);

        CloseFileDescriptor(waitable->readFileDescriptor);
        CloseFileDescriptor(waitable->writeFileDescriptor);
        CloseFileDescriptor(waitable->processFileDescriptor);

        if (waitable->mutexInitialized)
        {
            minipal_mutex_destroy(&waitable->mutex);
        }

        delete waitable;
    }

    void AddReference(Waitable* waitable)
    {
        __atomic_add_fetch(&waitable->refCount, 1, __ATOMIC_RELAXED);
    }

    void ReleaseReference(Waitable* waitable)
    {
        if (__atomic_sub_fetch(&waitable->refCount, 1, __ATOMIC_ACQ_REL) == 0)
        {
            DestroyWaitable(waitable);
        }
    }

    bool WriteByte(int fileDescriptor, bool allowFullPipe)
    {
        uint8_t value = 1;
        ssize_t result;
        do
        {
            result = write(fileDescriptor, &value, sizeof(value));
        } while (result < 0 && errno == EINTR);

        return result == sizeof(value) || (allowFullPipe && result < 0 && errno == EAGAIN);
    }

    void ReleasePublicReference(Waitable* waitable)
    {
        {
            minipal::MutexHolder lock(waitable->mutex);
            int32_t publicRefCount =
                __atomic_sub_fetch(&waitable->publicRefCount, 1, __ATOMIC_ACQ_REL);
            assert(publicRefCount >= 0);
        }

        ReleaseReference(waitable);
    }

    bool ReadByte(int fileDescriptor)
    {
        uint8_t value;
        ssize_t result;
        do
        {
            result = read(fileDescriptor, &value, sizeof(value));
        } while (result < 0 && errno == EINTR);

        return result == sizeof(value);
    }

    bool SignalPipeLocked(Waitable* waitable)
    {
        if (waitable->signaled)
        {
            return true;
        }

        if (!WriteByte(waitable->writeFileDescriptor, false))
        {
            return false;
        }

        waitable->signaled = true;
        return true;
    }

    bool SignalPipe(Waitable* waitable)
    {
        minipal::MutexHolder lock(waitable->mutex);
        return SignalPipeLocked(waitable);
    }

    bool ResetPipe(Waitable* waitable)
    {
        minipal::MutexHolder lock(waitable->mutex);
        if (!waitable->signaled)
        {
            return true;
        }

        uint8_t buffer[16];
        ssize_t result;
        do
        {
            result = read(waitable->readFileDescriptor, buffer, sizeof(buffer));
        } while (result > 0 || (result < 0 && errno == EINTR));

        if (result < 0 && errno != EAGAIN)
        {
            return false;
        }

        waitable->signaled = false;
        return true;
    }

    AcquireResult TryAcquirePipe(Waitable* waitable)
    {
        minipal::MutexHolder lock(waitable->mutex);
        if (waitable->errorCode != 0)
        {
            errno = waitable->errorCode;
            return AcquireResult::Failed;
        }

        if (!waitable->signaled)
        {
            return AcquireResult::NotReady;
        }

        if (!waitable->manualReset)
        {
            if (!ReadByte(waitable->readFileDescriptor))
            {
                return AcquireResult::Failed;
            }

            waitable->signaled = false;
        }

        return AcquireResult::Ready;
    }

    void ReapChild(pid_t processId)
    {
        int status;
        pid_t result;
        do
        {
            result = waitpid(processId, &status, WNOHANG);
        } while (result < 0 && errno == EINTR);
    }

    AcquireResult TryAcquireProcessFileDescriptor(Waitable* waitable)
    {
        minipal::MutexHolder lock(waitable->mutex);
        if (waitable->signaled)
        {
            return AcquireResult::Ready;
        }

        pollfd descriptor = {};
        descriptor.fd = waitable->processFileDescriptor;
        descriptor.events = POLLIN;

        int result;
        do
        {
            result = poll(&descriptor, 1, 0);
        } while (result < 0 && errno == EINTR);

        if (result < 0 || (descriptor.revents & POLLNVAL) != 0)
        {
            return AcquireResult::Failed;
        }

        if (result == 0)
        {
            return AcquireResult::NotReady;
        }

        ReapChild(waitable->processId);
        waitable->signaled = true;
        return AcquireResult::Ready;
    }

#if MINIPAL_WAIT_USES_KQUEUE
    AcquireResult TryAcquireProcessKqueue(Waitable* waitable)
    {
        minipal::MutexHolder lock(waitable->mutex);
        if (waitable->signaled)
        {
            return AcquireResult::Ready;
        }

        struct kevent processEvent;
        timespec timeout = {};
        int result;
        do
        {
            result = kevent(
                waitable->processFileDescriptor,
                nullptr,
                0,
                &processEvent,
                1,
                &timeout);
        } while (result < 0 && errno == EINTR);

        if (result < 0)
        {
            return AcquireResult::Failed;
        }

        if (result == 0)
        {
            return AcquireResult::NotReady;
        }

        ReapChild(waitable->processId);
        waitable->signaled = true;
        return AcquireResult::Ready;
    }

    void MarkExitedProcesses(
        Waitable* const* waitables,
        uint32_t count,
        const struct kevent* events,
        int eventCount)
    {
        for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
        {
            if (events[eventIndex].filter != EVFILT_PROC)
            {
                continue;
            }

            for (uint32_t handleIndex = 0; handleIndex < count; handleIndex++)
            {
                Waitable* waitable = waitables[handleIndex];
                if (waitable->kind == WaitableKind::ProcessKqueue &&
                    waitable->processId == static_cast<pid_t>(events[eventIndex].ident))
                {
                    minipal::MutexHolder lock(waitable->mutex);
                    ReapChild(waitable->processId);
                    waitable->signaled = true;
                }
            }
        }
    }
#endif // MINIPAL_WAIT_USES_KQUEUE

    AcquireResult TryAcquire(Waitable* waitable)
    {
        switch (waitable->kind)
        {
            case WaitableKind::Event:
            case WaitableKind::ProcessPipe:
                return TryAcquirePipe(waitable);

            case WaitableKind::ProcessFileDescriptor:
                return TryAcquireProcessFileDescriptor(waitable);

#if MINIPAL_WAIT_USES_KQUEUE
            case WaitableKind::ProcessKqueue:
                return TryAcquireProcessKqueue(waitable);
#endif // MINIPAL_WAIT_USES_KQUEUE

            default:
                return AcquireResult::Failed;
        }
    }

    int32_t TryAcquireAny(Waitable* const* waitables, uint32_t count)
    {
        for (uint32_t index = 0; index < count; index++)
        {
            AcquireResult result = TryAcquire(waitables[index]);
            if (result == AcquireResult::Failed)
            {
                return MINIPAL_WAIT_FAILED;
            }

            if (result == AcquireResult::Ready)
            {
                return static_cast<int32_t>(index);
            }
        }

        return MINIPAL_WAIT_TIMEOUT;
    }

    bool GetMonotonicTime(uint64_t* nanoseconds)
    {
        timespec currentTime;
        if (clock_gettime(CLOCK_MONOTONIC, &currentTime) != 0)
        {
            return false;
        }

        *nanoseconds =
            static_cast<uint64_t>(currentTime.tv_sec) * 1000000000 +
            static_cast<uint64_t>(currentTime.tv_nsec);
        return true;
    }

    bool GetRemainingNanoseconds(uint64_t deadline, uint64_t* remaining)
    {
        uint64_t currentTime;
        if (!GetMonotonicTime(&currentTime))
        {
            return false;
        }

        *remaining = currentTime >= deadline ? 0 : deadline - currentTime;
        return true;
    }

    int GetPollTimeout(uint64_t remainingNanoseconds)
    {
        uint64_t remainingMilliseconds = (remainingNanoseconds + 999999) / 1000000;
        return remainingMilliseconds > INT_MAX
            ? INT_MAX
            : static_cast<int>(remainingMilliseconds);
    }

    bool HasProcessExited(pid_t processId)
    {
        int status;
        pid_t result;
        do
        {
            result = waitpid(processId, &status, WNOHANG);
        } while (result < 0 && errno == EINTR);

        if (result == processId)
        {
            return true;
        }

        if (result == 0)
        {
            return false;
        }

        if (result < 0 && errno == ECHILD)
        {
            // No identity-stable primitive is available on this fallback path, so PID reuse can race this probe.
            int killResult;
            do
            {
                killResult = kill(processId, 0);
            } while (killResult < 0 && errno == EINTR);

            return killResult < 0 && errno == ESRCH;
        }

        return false;
    }

    void SignalProcessWatcherResult(Waitable* waitable, int errorCode)
    {
        minipal::MutexHolder lock(waitable->mutex);
        if (__atomic_load_n(&waitable->publicRefCount, __ATOMIC_ACQUIRE) == 0)
        {
            return;
        }

        waitable->errorCode = errorCode;
        if (SignalPipeLocked(waitable))
        {
#if defined(MINIPAL_WAIT_TESTS)
            __atomic_add_fetch(&s_processWatcherSignalCount, 1, __ATOMIC_ACQ_REL);
#endif // MINIPAL_WAIT_TESTS
        }
    }

    void* ProcessWatcher(void* argument)
    {
        Waitable* waitable = static_cast<Waitable*>(argument);
#if defined(MINIPAL_WAIT_TESTS)
        __atomic_add_fetch(&s_processWatcherCount, 1, __ATOMIC_ACQ_REL);
        if (__atomic_load_n(&s_pauseProcessWatchers, __ATOMIC_ACQUIRE) != 0)
        {
            __atomic_add_fetch(&s_pausedProcessWatcherCount, 1, __ATOMIC_ACQ_REL);
            while (__atomic_load_n(&s_pauseProcessWatchers, __ATOMIC_ACQUIRE) != 0)
            {
                poll(nullptr, 0, 1);
            }
            __atomic_sub_fetch(&s_pausedProcessWatcherCount, 1, __ATOMIC_ACQ_REL);
        }
#endif // MINIPAL_WAIT_TESTS

        bool exited = false;
        int errorCode = 0;

        while (__atomic_load_n(&waitable->publicRefCount, __ATOMIC_ACQUIRE) != 0)
        {
            exited = HasProcessExited(waitable->processId);
            if (exited)
            {
                break;
            }

            int result;
            do
            {
                result = poll(nullptr, 0, 250);
            } while (result < 0 && errno == EINTR);

            if (result < 0)
            {
                errorCode = errno;
                break;
            }
        }

#if defined(MINIPAL_WAIT_TESTS)
        if (exited && __atomic_load_n(&s_pauseExitedProcessWatchers, __ATOMIC_ACQUIRE) != 0)
        {
            __atomic_add_fetch(&s_pausedExitedProcessWatcherCount, 1, __ATOMIC_ACQ_REL);
            while (__atomic_load_n(&s_pauseExitedProcessWatchers, __ATOMIC_ACQUIRE) != 0)
            {
                poll(nullptr, 0, 1);
            }
            __atomic_sub_fetch(&s_pausedExitedProcessWatcherCount, 1, __ATOMIC_ACQ_REL);
        }
#endif // MINIPAL_WAIT_TESTS

        if (exited || errorCode != 0)
        {
            SignalProcessWatcherResult(waitable, errorCode);
        }

#if defined(MINIPAL_WAIT_TESTS)
        __atomic_sub_fetch(&s_processWatcherCount, 1, __ATOMIC_ACQ_REL);
#endif // MINIPAL_WAIT_TESTS
        ReleaseReference(waitable);
        return nullptr;
    }

    bool StartProcessWatcher(Waitable* waitable)
    {
        pthread_attr_t attributes;
        if (pthread_attr_init(&attributes) != 0)
        {
            return false;
        }

        int result = pthread_attr_setdetachstate(&attributes, PTHREAD_CREATE_DETACHED);
        if (result == 0)
        {
            pthread_t watcherThread;
            AddReference(waitable);
            result = pthread_create(
                &watcherThread,
                &attributes,
                ProcessWatcher,
                waitable);

            if (result != 0)
            {
                ReleaseReference(waitable);
            }
        }

        int destroyResult = pthread_attr_destroy(&attributes);
        assert(destroyResult == 0);
        (void)destroyResult;
        return result == 0;
    }

    Waitable* CreatePipeWaitable(WaitableKind kind, bool manualReset, bool initialState)
    {
        Waitable* waitable = AllocateWaitable(kind);
        if (waitable == nullptr)
        {
            return nullptr;
        }

        int eventPipe[2];
        if (!CreatePipe(eventPipe))
        {
            ReleasePublicReference(waitable);
            return nullptr;
        }

        waitable->readFileDescriptor = eventPipe[0];
        waitable->writeFileDescriptor = eventPipe[1];
        waitable->manualReset = manualReset;

        if (initialState && !SignalPipe(waitable))
        {
            ReleasePublicReference(waitable);
            return nullptr;
        }

        return waitable;
    }

    Waitable* CreateSignaledProcessWaitable(pid_t processId)
    {
        Waitable* waitable = CreatePipeWaitable(WaitableKind::ProcessPipe, true, true);
        if (waitable != nullptr)
        {
            waitable->processId = processId;
            ReapChild(processId);
        }

        return waitable;
    }

    Waitable* CreateProcessWatcherWaitable(pid_t processId)
    {
        Waitable* waitable = CreatePipeWaitable(WaitableKind::ProcessPipe, true, false);
        if (waitable == nullptr)
        {
            return nullptr;
        }

        waitable->processId = processId;
        if (!StartProcessWatcher(waitable))
        {
            ReleasePublicReference(waitable);
            return nullptr;
        }

        return waitable;
    }

#if MINIPAL_WAIT_USES_KQUEUE
    Waitable* CreateKqueueProcessWaitable(pid_t processId)
    {
        int processQueue = kqueue();
        if (processQueue < 0)
        {
            return nullptr;
        }

        if (!SetCloseOnExec(processQueue))
        {
            CloseFileDescriptor(processQueue);
            return nullptr;
        }

        struct kevent change;
        EV_SET(
            &change,
            processId,
            EVFILT_PROC,
            EV_ADD | EV_ENABLE | EV_ONESHOT,
            NOTE_EXIT,
            0,
            nullptr);

        if (kevent(processQueue, &change, 1, nullptr, 0, nullptr) < 0)
        {
            int error = errno;
            CloseFileDescriptor(processQueue);
            if (error == ESRCH)
            {
                return CreateSignaledProcessWaitable(processId);
            }

            errno = error;
            return nullptr;
        }

        Waitable* waitable = AllocateWaitable(WaitableKind::ProcessKqueue);
        if (waitable == nullptr)
        {
            CloseFileDescriptor(processQueue);
            return nullptr;
        }

        waitable->processId = processId;
        waitable->processFileDescriptor = processQueue;
        waitable->manualReset = true;
        return waitable;
    }
#endif // MINIPAL_WAIT_USES_KQUEUE

    int32_t WaitWithPoll(
        Waitable* const* waitables,
        uint32_t count,
        uint32_t timeout,
        uint64_t deadline)
    {
        pollfd descriptors[MINIPAL_MAX_WAIT_OBJECTS] = {};
        for (uint32_t index = 0; index < count; index++)
        {
            Waitable* waitable = waitables[index];
            switch (waitable->kind)
            {
                case WaitableKind::Event:
                case WaitableKind::ProcessPipe:
                    descriptors[index].fd = waitable->readFileDescriptor;
                    break;

                case WaitableKind::ProcessFileDescriptor:
                    descriptors[index].fd = waitable->processFileDescriptor;
                    break;

                default:
                    errno = EINVAL;
                    return MINIPAL_WAIT_FAILED;
            }

            descriptors[index].events = POLLIN;
        }

        while (true)
        {
            int32_t acquired = TryAcquireAny(waitables, count);
            if (acquired != MINIPAL_WAIT_TIMEOUT || timeout == 0)
            {
                return acquired;
            }

            int pollTimeout = -1;
            if (timeout != MINIPAL_WAIT_INFINITE)
            {
                uint64_t remaining;
                if (!GetRemainingNanoseconds(deadline, &remaining))
                {
                    return MINIPAL_WAIT_FAILED;
                }

                if (remaining == 0)
                {
                    return MINIPAL_WAIT_TIMEOUT;
                }

                pollTimeout = GetPollTimeout(remaining);
            }

            int result = poll(descriptors, count, pollTimeout);
            if (result == 0)
            {
                return MINIPAL_WAIT_TIMEOUT;
            }

            if (result < 0)
            {
                if (errno == EINTR)
                {
                    continue;
                }

                return MINIPAL_WAIT_FAILED;
            }

            for (uint32_t index = 0; index < count; index++)
            {
                if ((descriptors[index].revents & POLLNVAL) != 0)
                {
                    errno = EBADF;
                    return MINIPAL_WAIT_FAILED;
                }
            }
        }
    }

#if MINIPAL_WAIT_USES_KQUEUE
    int32_t WaitWithKqueue(
        Waitable* const* waitables,
        uint32_t count,
        uint32_t timeout,
        uint64_t deadline)
    {
        while (true)
        {
            int32_t acquired = TryAcquireAny(waitables, count);
            if (acquired != MINIPAL_WAIT_TIMEOUT || timeout == 0)
            {
                return acquired;
            }

            int waitQueue = kqueue();
            if (waitQueue < 0)
            {
                return MINIPAL_WAIT_FAILED;
            }

            if (!SetCloseOnExec(waitQueue))
            {
                CloseFileDescriptor(waitQueue);
                return MINIPAL_WAIT_FAILED;
            }

            bool registrationFailed = false;
            for (uint32_t index = 0; index < count; index++)
            {
                Waitable* waitable = waitables[index];
                struct kevent change;
                if (waitable->kind == WaitableKind::ProcessKqueue)
                {
                    EV_SET(
                        &change,
                        waitable->processId,
                        EVFILT_PROC,
                        EV_ADD | EV_ENABLE | EV_ONESHOT,
                        NOTE_EXIT,
                        0,
                        nullptr);
                }
                else
                {
                    int fileDescriptor = waitable->kind == WaitableKind::ProcessFileDescriptor
                        ? waitable->processFileDescriptor
                        : waitable->readFileDescriptor;
                    EV_SET(
                        &change,
                        fileDescriptor,
                        EVFILT_READ,
                        EV_ADD | EV_ENABLE,
                        0,
                        0,
                        nullptr);
                }

                if (kevent(waitQueue, &change, 1, nullptr, 0, nullptr) < 0)
                {
                    if (waitable->kind == WaitableKind::ProcessKqueue && errno == ESRCH)
                    {
                        minipal::MutexHolder lock(waitable->mutex);
                        ReapChild(waitable->processId);
                        waitable->signaled = true;
                        continue;
                    }

                    registrationFailed = true;
                    break;
                }
            }

            if (registrationFailed)
            {
                CloseFileDescriptor(waitQueue);
                return MINIPAL_WAIT_FAILED;
            }

            acquired = TryAcquireAny(waitables, count);
            if (acquired != MINIPAL_WAIT_TIMEOUT)
            {
                CloseFileDescriptor(waitQueue);
                return acquired;
            }

            timespec remainingTime;
            timespec* waitTimeout = nullptr;
            if (timeout != MINIPAL_WAIT_INFINITE)
            {
                uint64_t remaining;
                if (!GetRemainingNanoseconds(deadline, &remaining))
                {
                    CloseFileDescriptor(waitQueue);
                    return MINIPAL_WAIT_FAILED;
                }

                if (remaining == 0)
                {
                    CloseFileDescriptor(waitQueue);
                    return MINIPAL_WAIT_TIMEOUT;
                }

                remainingTime.tv_sec = static_cast<time_t>(remaining / 1000000000);
                remainingTime.tv_nsec = static_cast<long>(remaining % 1000000000);
                waitTimeout = &remainingTime;
            }

            struct kevent events[MINIPAL_MAX_WAIT_OBJECTS];
            int result;
            do
            {
                result = kevent(waitQueue, nullptr, 0, events, count, waitTimeout);
            } while (result < 0 && errno == EINTR && timeout == MINIPAL_WAIT_INFINITE);

            if (result > 0)
            {
                for (int eventIndex = 0; eventIndex < result; eventIndex++)
                {
                    if ((events[eventIndex].flags & EV_ERROR) != 0 && events[eventIndex].data != 0)
                    {
                        errno = static_cast<int>(events[eventIndex].data);
                        CloseFileDescriptor(waitQueue);
                        return MINIPAL_WAIT_FAILED;
                    }
                }

                MarkExitedProcesses(waitables, count, events, result);
            }

            int error = errno;
            CloseFileDescriptor(waitQueue);

            if (result == 0)
            {
                return MINIPAL_WAIT_TIMEOUT;
            }

            if (result < 0)
            {
                if (error == EINTR)
                {
                    continue;
                }

                errno = error;
                return MINIPAL_WAIT_FAILED;
            }
        }
    }
#endif // MINIPAL_WAIT_USES_KQUEUE

    Waitable* CreateProcessWaitable(uint32_t processId)
    {
        if (processId == 0 || processId > static_cast<uint32_t>(INT_MAX))
        {
            errno = EINVAL;
            return nullptr;
        }

#if !defined(MINIPAL_WAIT_FORCE_PROCESS_WATCHER)
#if defined(TARGET_LINUX) && defined(SYS_pidfd_open)
        int processFileDescriptor;
        do
        {
            processFileDescriptor = static_cast<int>(syscall(SYS_pidfd_open, processId, 0));
        } while (processFileDescriptor < 0 && errno == EINTR);

        if (processFileDescriptor >= 0)
        {
            if (!SetCloseOnExec(processFileDescriptor))
            {
                CloseFileDescriptor(processFileDescriptor);
                return nullptr;
            }

            Waitable* waitable = AllocateWaitable(WaitableKind::ProcessFileDescriptor);
            if (waitable == nullptr)
            {
                CloseFileDescriptor(processFileDescriptor);
                return nullptr;
            }

            waitable->processId = static_cast<pid_t>(processId);
            waitable->processFileDescriptor = processFileDescriptor;
            waitable->manualReset = true;
            return waitable;
        }

        if (errno == ENOSYS)
        {
            // The syscall can be present in the build headers but absent from the running kernel.
            return CreateProcessWatcherWaitable(static_cast<pid_t>(processId));
        }

        if (errno == ESRCH)
        {
            return CreateSignaledProcessWaitable(static_cast<pid_t>(processId));
        }
#endif // TARGET_LINUX && SYS_pidfd_open

#if MINIPAL_WAIT_USES_KQUEUE
        Waitable* waitable = CreateKqueueProcessWaitable(static_cast<pid_t>(processId));
        if (waitable != nullptr)
        {
            return waitable;
        }
#endif // MINIPAL_WAIT_USES_KQUEUE
#endif // !MINIPAL_WAIT_FORCE_PROCESS_WATCHER

        return CreateProcessWatcherWaitable(static_cast<pid_t>(processId));
    }

    void* DuplicateWaitable(void* handle)
    {
        if (handle == nullptr)
        {
            errno = EINVAL;
            return nullptr;
        }

        Waitable* waitable = static_cast<Waitable*>(handle);
        __atomic_add_fetch(&waitable->publicRefCount, 1, __ATOMIC_RELAXED);
        AddReference(waitable);
        return waitable;
    }
}

#if defined(MINIPAL_WAIT_TESTS)
extern "C" void minipal_wait_test_pause_process_watchers(bool pause)
{
    __atomic_store_n(&s_pauseProcessWatchers, pause ? 1 : 0, __ATOMIC_RELEASE);
}

extern "C" void minipal_wait_test_pause_exited_process_watchers(bool pause)
{
    __atomic_store_n(&s_pauseExitedProcessWatchers, pause ? 1 : 0, __ATOMIC_RELEASE);
}

extern "C" int32_t minipal_wait_test_get_process_watcher_count()
{
    return __atomic_load_n(&s_processWatcherCount, __ATOMIC_ACQUIRE);
}

extern "C" int32_t minipal_wait_test_get_paused_process_watcher_count()
{
    return __atomic_load_n(&s_pausedProcessWatcherCount, __ATOMIC_ACQUIRE);
}

extern "C" int32_t minipal_wait_test_get_paused_exited_process_watcher_count()
{
    return __atomic_load_n(&s_pausedExitedProcessWatcherCount, __ATOMIC_ACQUIRE);
}

extern "C" int32_t minipal_wait_test_get_process_watcher_signal_count()
{
    return __atomic_load_n(&s_processWatcherSignalCount, __ATOMIC_ACQUIRE);
}

extern "C" void minipal_wait_test_reset_process_watcher_signal_count()
{
    __atomic_store_n(&s_processWatcherSignalCount, 0, __ATOMIC_RELEASE);
}
#endif // MINIPAL_WAIT_TESTS

minipal_wait_handle::minipal_wait_handle(void* handle)
    : m_handle(handle)
{
}

minipal_wait_handle::minipal_wait_handle(const minipal_wait_handle& handle)
    : minipal_wait_handle(DuplicateWaitable(handle.m_handle))
{
}

minipal_wait_handle::~minipal_wait_handle()
{
    if (m_handle != nullptr)
    {
        ReleasePublicReference(static_cast<Waitable*>(m_handle));
    }
}

minipal_event::minipal_event(bool manualReset, bool initialState)
    : minipal_wait_handle(CreatePipeWaitable(WaitableKind::Event, manualReset, initialState))
{
}

bool minipal_event::Set()
{
    if (!IsValid())
    {
        errno = EINVAL;
        return false;
    }

    Waitable* waitable = static_cast<Waitable*>(GetWaitable());
    if (waitable->kind != WaitableKind::Event)
    {
        errno = EINVAL;
        return false;
    }

    return SignalPipe(waitable);
}

bool minipal_event::Reset()
{
    if (!IsValid())
    {
        errno = EINVAL;
        return false;
    }

    Waitable* waitable = static_cast<Waitable*>(GetWaitable());
    if (waitable->kind != WaitableKind::Event)
    {
        errno = EINVAL;
        return false;
    }

    return ResetPipe(waitable);
}

minipal_process_wait::minipal_process_wait(uint32_t processId)
    : minipal_wait_handle(CreateProcessWaitable(processId))
{
}

int32_t minipal_wait_handle::Wait(
    const minipal_wait_handle* const* handles,
    uint32_t count,
    uint32_t timeout)
{
    if (handles == nullptr || count == 0 || count > MINIPAL_MAX_WAIT_OBJECTS)
    {
        errno = EINVAL;
        return MINIPAL_WAIT_FAILED;
    }

    Waitable* waitables[MINIPAL_MAX_WAIT_OBJECTS];
    for (uint32_t index = 0; index < count; index++)
    {
        if (handles[index] == nullptr || !handles[index]->IsValid())
        {
            errno = EINVAL;
            return MINIPAL_WAIT_FAILED;
        }

        waitables[index] = static_cast<Waitable*>(handles[index]->m_handle);
    }

    uint64_t deadline = 0;
    if (timeout != 0 && timeout != MINIPAL_WAIT_INFINITE)
    {
        uint64_t currentTime;
        if (!GetMonotonicTime(&currentTime))
        {
            return MINIPAL_WAIT_FAILED;
        }

        deadline = currentTime + static_cast<uint64_t>(timeout) * 1000000;
    }

#if MINIPAL_WAIT_USES_KQUEUE
    return WaitWithKqueue(waitables, count, timeout, deadline);
#else
    return WaitWithPoll(waitables, count, timeout, deadline);
#endif
}
