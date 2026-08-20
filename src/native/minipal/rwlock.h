// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_RWLOCK_H
#define HAVE_MINIPAL_RWLOCK_H

#include <stdbool.h>

#ifdef HOST_WINDOWS
#include <windows.h>
typedef SRWLOCK MINIPAL_RWLOCK_IMPL;
#else // !HOST_WINDOWS
#include <pthread.h>
typedef pthread_rwlock_t MINIPAL_RWLOCK_IMPL;
#endif // HOST_WINDOWS

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _minipal_rwlock
{
    MINIPAL_RWLOCK_IMPL _impl;
} minipal_rwlock;

// Initialize the rwlock.
bool minipal_rwlock_init(minipal_rwlock* rwlock);

// Destroy the rwlock.
void minipal_rwlock_destroy(minipal_rwlock* rwlock);

// Acquire the lock in shared mode.
void minipal_rwlock_acquire_read(minipal_rwlock* rwlock);

// Release the lock in shared mode.
void minipal_rwlock_release_read(minipal_rwlock* rwlock);

// Acquire the lock in exclusive mode.
void minipal_rwlock_acquire_write(minipal_rwlock* rwlock);

// Release the lock in exclusive mode.
void minipal_rwlock_release_write(minipal_rwlock* rwlock);

#ifdef __cplusplus
}
#endif // __cplusplus

#ifdef __cplusplus
namespace minipal
{
    class ReadLockHolder final
    {
        minipal_rwlock& _rwlock;

    public:
        explicit ReadLockHolder(minipal_rwlock& rwlock)
            : _rwlock{ rwlock }
        {
            minipal_rwlock_acquire_read(&_rwlock);
        }

        ~ReadLockHolder() noexcept
        {
            minipal_rwlock_release_read(&_rwlock);
        }

        ReadLockHolder(ReadLockHolder const&) = delete;
        ReadLockHolder& operator=(ReadLockHolder const&) = delete;

        ReadLockHolder(ReadLockHolder&&) = delete;
        ReadLockHolder& operator=(ReadLockHolder&&) = delete;
    };

    class WriteLockHolder final
    {
        minipal_rwlock& _rwlock;

    public:
        explicit WriteLockHolder(minipal_rwlock& rwlock)
            : _rwlock{ rwlock }
        {
            minipal_rwlock_acquire_write(&_rwlock);
        }

        ~WriteLockHolder() noexcept
        {
            minipal_rwlock_release_write(&_rwlock);
        }

        WriteLockHolder(WriteLockHolder const&) = delete;
        WriteLockHolder& operator=(WriteLockHolder const&) = delete;

        WriteLockHolder(WriteLockHolder&&) = delete;
        WriteLockHolder& operator=(WriteLockHolder&&) = delete;
    };
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_RWLOCK_H
