// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <string.h>
#include "rwlock.h"

bool minipal_rwlock_init(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    InitializeSRWLock(&rwlock->_impl);
    return true;
#else
    return pthread_rwlock_init(&rwlock->_impl, NULL) == 0;
#endif // HOST_WINDOWS
}

void minipal_rwlock_destroy(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifndef HOST_WINDOWS
    int st = pthread_rwlock_destroy(&rwlock->_impl);
    assert(st == 0);
    (void)st;
#endif // !HOST_WINDOWS

#ifdef _DEBUG
    memset(rwlock, 0, sizeof(*rwlock));
#endif // _DEBUG
}

void minipal_rwlock_acquire_read(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    AcquireSRWLockShared(&rwlock->_impl);
#else
    int st = pthread_rwlock_rdlock(&rwlock->_impl);
    assert(st == 0);
    (void)st;
#endif // HOST_WINDOWS
}

void minipal_rwlock_release_read(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    ReleaseSRWLockShared(&rwlock->_impl);
#else
    int st = pthread_rwlock_unlock(&rwlock->_impl);
    assert(st == 0);
    (void)st;
#endif // HOST_WINDOWS
}

void minipal_rwlock_acquire_write(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    AcquireSRWLockExclusive(&rwlock->_impl);
#else
    int st = pthread_rwlock_wrlock(&rwlock->_impl);
    assert(st == 0);
    (void)st;
#endif // HOST_WINDOWS
}

void minipal_rwlock_release_write(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    ReleaseSRWLockExclusive(&rwlock->_impl);
#else
    int st = pthread_rwlock_unlock(&rwlock->_impl);
    assert(st == 0);
    (void)st;
#endif // HOST_WINDOWS
}
