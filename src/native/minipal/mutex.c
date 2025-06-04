// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <string.h>
#include "mutex.h"

bool minipal_mutex_init(minipal_mutex* mtx)
{
    assert(mtx != NULL);
#ifdef HOST_WINDOWS
    InitializeCriticalSection(&mtx->_impl);
    return true;
#else
    pthread_mutexattr_t mutexAttributes;
    int st = pthread_mutexattr_init(&mutexAttributes);
    if (st != 0)
        return false;

    st = pthread_mutexattr_settype(&mutexAttributes, PTHREAD_MUTEX_RECURSIVE);
    if (st == 0)
        st = pthread_mutex_init(&mtx->_impl, &mutexAttributes);

    pthread_mutexattr_destroy(&mutexAttributes);

    return (st == 0);
#endif // HOST_WINDOWS
}

void minipal_mutex_destroy(minipal_mutex* mtx)
{
    assert(mtx != NULL);
#ifdef HOST_WINDOWS
    DeleteCriticalSection(&mtx->_impl);
#else
    int st = pthread_mutex_destroy(&mtx->_impl);
    assert(st == 0);
    (void)st;
#endif // HOST_WINDOWS

#ifdef _DEBUG
    memset(mtx, 0, sizeof(*mtx));
#endif // _DEBUG
}

void minipal_mutex_enter(minipal_mutex* mtx)
{
    assert(mtx != NULL);
#ifdef HOST_WINDOWS
    EnterCriticalSection(&mtx->_impl);
#else
    int st = pthread_mutex_lock(&mtx->_impl);
    assert(st == 0);
    (void)st;
#endif // HOST_WINDOWS
}

void minipal_mutex_leave(minipal_mutex* mtx)
{
    assert(mtx != NULL);
#ifdef HOST_WINDOWS
    LeaveCriticalSection(&mtx->_impl);
#else
    int st = pthread_mutex_unlock(&mtx->_impl);
    assert(st == 0);
    (void)st;
#endif // HOST_WINDOWS
}
