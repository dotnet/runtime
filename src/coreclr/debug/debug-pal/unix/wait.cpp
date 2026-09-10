// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <errno.h>
#include <fcntl.h>
#include <limits.h>
#include <new>
#include <poll.h>
#include <stdint.h>
#include <time.h>
#include <unistd.h>

#include <minipal/mutex.h>

#include "debugwait.h"

namespace
{
    constexpr uint32_t MaxWaitHandles = 64;

    enum class AcquireResult
    {
        Failed,
        NotReady,
        Ready,
    };

    struct Waitable
    {
        minipal_mutex mutex;
        int32_t refCount;
        int readFileDescriptor;
        int writeFileDescriptor;
        bool mutexInitialized;
        bool resetOnWait;
        bool signaled;
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

    Waitable* AllocateWaitable(bool resetOnWait)
    {
        Waitable* waitable = new (std::nothrow) Waitable;
        if (waitable == nullptr)
        {
            return nullptr;
        }

        waitable->refCount = 1;
        waitable->mutexInitialized = false;
        waitable->resetOnWait = resetOnWait;
        waitable->signaled = false;
        waitable->readFileDescriptor = -1;
        waitable->writeFileDescriptor = -1;

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

        CloseFileDescriptor(waitable->readFileDescriptor);
        CloseFileDescriptor(waitable->writeFileDescriptor);

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

    bool WriteByte(int fileDescriptor)
    {
        uint8_t value = 1;
        ssize_t result;
        do
        {
            result = write(fileDescriptor, &value, sizeof(value));
        } while (result < 0 && errno == EINTR);

        return result == sizeof(value);
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

    bool SignalEventLocked(Waitable* waitable)
    {
        if (waitable->signaled)
        {
            return true;
        }

        if (!WriteByte(waitable->writeFileDescriptor))
        {
            return false;
        }

        waitable->signaled = true;
        return true;
    }

    bool SignalEvent(Waitable* waitable)
    {
        minipal::MutexHolder lock(waitable->mutex);
        return SignalEventLocked(waitable);
    }

    bool ResetEvent(Waitable* waitable)
    {
        assert(waitable->resetOnWait);
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

    AcquireResult TryAcquire(Waitable* waitable)
    {
        minipal::MutexHolder lock(waitable->mutex);
        if (!waitable->signaled)
        {
            return AcquireResult::NotReady;
        }

        if (waitable->resetOnWait &&
            !ReadByte(waitable->readFileDescriptor))
        {
            return AcquireResult::Failed;
        }

        if (waitable->resetOnWait)
        {
            waitable->signaled = false;
        }
        return AcquireResult::Ready;
    }

    int32_t TryAcquireAny(Waitable* const* waitables, uint32_t count)
    {
        for (uint32_t index = 0; index < count; index++)
        {
            AcquireResult result = TryAcquire(waitables[index]);
            if (result == AcquireResult::Failed)
            {
                return WaitHandle::Failed;
            }

            if (result == AcquireResult::Ready)
            {
                return static_cast<int32_t>(index);
            }
        }

        return WaitHandle::Timeout;
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

    Waitable* CreateWaitable(bool initialState, bool resetOnWait)
    {
        Waitable* waitable = AllocateWaitable(resetOnWait);
        if (waitable == nullptr)
        {
            return nullptr;
        }

        int eventPipe[2];
        if (!CreatePipe(eventPipe))
        {
            ReleaseReference(waitable);
            return nullptr;
        }

        waitable->readFileDescriptor = eventPipe[0];
        waitable->writeFileDescriptor = eventPipe[1];

        if (initialState && !SignalEvent(waitable))
        {
            ReleaseReference(waitable);
            return nullptr;
        }

        return waitable;
    }

    int32_t WaitWithPoll(
        Waitable* const* waitables,
        uint32_t count,
        uint32_t timeout,
        uint64_t deadline)
    {
        pollfd descriptors[MaxWaitHandles] = {};
        for (uint32_t index = 0; index < count; index++)
        {
            descriptors[index].fd = waitables[index]->readFileDescriptor;
            descriptors[index].events = POLLIN;
        }

        while (true)
        {
            int32_t acquired = TryAcquireAny(waitables, count);
            if (acquired != WaitHandle::Timeout || timeout == 0)
            {
                return acquired;
            }

            int pollTimeout = -1;
            if (timeout != WaitHandle::Infinite)
            {
                uint64_t remaining;
                if (!GetRemainingNanoseconds(deadline, &remaining))
                {
                    return WaitHandle::Failed;
                }

                if (remaining == 0)
                {
                    return WaitHandle::Timeout;
                }

                pollTimeout = GetPollTimeout(remaining);
            }

            int result = poll(descriptors, count, pollTimeout);
            if (result == 0)
            {
                return WaitHandle::Timeout;
            }

            if (result < 0)
            {
                if (errno == EINTR)
                {
                    continue;
                }

                return WaitHandle::Failed;
            }

            for (uint32_t index = 0; index < count; index++)
            {
                if ((descriptors[index].revents & POLLNVAL) != 0)
                {
                    errno = EBADF;
                    return WaitHandle::Failed;
                }
            }
        }
    }

    void* DuplicateWaitable(void* handle)
    {
        if (handle == nullptr)
        {
            errno = EINVAL;
            return nullptr;
        }

        Waitable* waitable = static_cast<Waitable*>(handle);
        AddReference(waitable);
        return waitable;
    }
}

WaitHandle::WaitHandle(void* handle)
    : m_handle(handle)
{
}

WaitHandle::WaitHandle(const WaitHandle& handle)
    : WaitHandle(DuplicateWaitable(handle.m_handle))
{
}

WaitHandle::~WaitHandle()
{
    if (m_handle != nullptr)
    {
        ReleaseReference(static_cast<Waitable*>(m_handle));
    }
}

WaitEvent::WaitEvent(bool initialState)
    : WaitHandle(CreateWaitable(initialState, true))
{
}

bool WaitEvent::Set()
{
    if (!IsValid())
    {
        errno = EINVAL;
        return false;
    }

    return SignalEvent(static_cast<Waitable*>(GetWaitable()));
}

bool WaitEvent::Reset()
{
    if (!IsValid())
    {
        errno = EINVAL;
        return false;
    }

    return ResetEvent(static_cast<Waitable*>(GetWaitable()));
}

WaitLatch::WaitLatch()
    : WaitHandle(CreateWaitable(false, false))
{
}

bool WaitLatch::Set()
{
    if (!IsValid())
    {
        errno = EINVAL;
        return false;
    }

    return SignalEvent(static_cast<Waitable*>(GetWaitable()));
}

int32_t WaitHandle::Wait(
    const WaitHandle* const* handles,
    uint32_t count,
    uint32_t timeout)
{
    if (handles == nullptr || count == 0 || count > MaxWaitHandles)
    {
        errno = EINVAL;
        return Failed;
    }

    Waitable* waitables[MaxWaitHandles];
    for (uint32_t index = 0; index < count; index++)
    {
        if (handles[index] == nullptr || !handles[index]->IsValid())
        {
            errno = EINVAL;
            return Failed;
        }

        waitables[index] = static_cast<Waitable*>(handles[index]->m_handle);
    }

    uint64_t deadline = 0;
    if (timeout != 0 && timeout != Infinite)
    {
        uint64_t currentTime;
        if (!GetMonotonicTime(&currentTime))
        {
            return Failed;
        }

        deadline = currentTime + static_cast<uint64_t>(timeout) * 1000000;
    }

    return WaitWithPoll(waitables, count, timeout, deadline);
}
