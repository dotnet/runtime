// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(__linux__) && !defined(_GNU_SOURCE)
// glibc hides pthread_rwlockattr_setkind_np in strict standards modes.
#define _GNU_SOURCE
#endif

#include <assert.h>
#include "minipalconfig.h"
#include "rwlock.h"

bool minipal_rwlock_init(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    InitializeSRWLock(&rwlock->_impl);
    return true;
#elif HAVE_PTHREAD_RWLOCK_PREFER_WRITER_NONRECURSIVE_NP
    pthread_rwlockattr_t attributes;
    int st = pthread_rwlockattr_init(&attributes);
    if (st == 0)
    {
        st = pthread_rwlockattr_setkind_np(&attributes, PTHREAD_RWLOCK_PREFER_WRITER_NONRECURSIVE_NP);
        if (st == 0)
            st = pthread_rwlock_init(&rwlock->_impl, &attributes);

        pthread_rwlockattr_destroy(&attributes);
        if (st == 0)
            return true;
    }

    return pthread_rwlock_init(&rwlock->_impl, NULL) == 0;
#else
    return pthread_rwlock_init(&rwlock->_impl, NULL) == 0;
#endif
}

void minipal_rwlock_destroy(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifndef HOST_WINDOWS
    int st = pthread_rwlock_destroy(&rwlock->_impl);
    assert(st == 0);
    (void)st;
#endif // !HOST_WINDOWS
}

bool minipal_rwlock_enter_read(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    AcquireSRWLockShared(&rwlock->_impl);
#else
    int st = pthread_rwlock_rdlock(&rwlock->_impl);
    if (st != 0)
        return false;
#endif // HOST_WINDOWS
    return true;
}

void minipal_rwlock_leave_read(minipal_rwlock* rwlock)
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

bool minipal_rwlock_enter_write(minipal_rwlock* rwlock)
{
    assert(rwlock != NULL);
#ifdef HOST_WINDOWS
    AcquireSRWLockExclusive(&rwlock->_impl);
#else
    int st = pthread_rwlock_wrlock(&rwlock->_impl);
    if (st != 0)
        return false;
#endif // HOST_WINDOWS
    return true;
}

void minipal_rwlock_leave_write(minipal_rwlock* rwlock)
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
