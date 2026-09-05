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

// Initialize the read-write lock.
bool minipal_rwlock_init(minipal_rwlock* rwlock);

// Destroy the read-write lock.
void minipal_rwlock_destroy(minipal_rwlock* rwlock);

// Enter the read-write lock in shared mode. Blocks until the lock can be entered.
bool minipal_rwlock_enter_read(minipal_rwlock* rwlock);

// Leave the read-write lock from shared mode.
void minipal_rwlock_leave_read(minipal_rwlock* rwlock);

// Enter the read-write lock in exclusive mode. Blocks until the lock can be entered.
bool minipal_rwlock_enter_write(minipal_rwlock* rwlock);

// Leave the read-write lock from exclusive mode.
void minipal_rwlock_leave_write(minipal_rwlock* rwlock);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_RWLOCK_H
