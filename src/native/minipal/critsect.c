// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <string.h>
#include "critsect.h"

bool minipal_critsec_init(DN_CRIT_SECT* cs)
{
    assert(cs != NULL);
#ifdef HOST_WINDOWS
    InitializeCriticalSection(&cs->_impl);
    return true;
#else
    pthread_mutexattr_t mutexAttributes;
    int st = pthread_mutexattr_init(&mutexAttributes);
    if (st != 0)
        return false;

    st = pthread_mutexattr_settype(&mutexAttributes, PTHREAD_MUTEX_RECURSIVE);
    if (st == 0)
        st = pthread_mutex_init(&cs->_impl, &mutexAttributes);

    pthread_mutexattr_destroy(&mutexAttributes);

    return (st == 0);
#endif // HOST_WINDOWS
}

void minipal_critsec_destroy(DN_CRIT_SECT* cs)
{
    assert(cs != NULL);
#ifdef HOST_WINDOWS
    DeleteCriticalSection(&cs->_impl);
#else
    int st = pthread_mutex_destroy(&cs->_impl);
    assert(st == 0);
#endif // HOST_WINDOWS

#ifdef _DEBUG
    memset(cs, 0, sizeof(*cs));
#endif // _DEBUG
}

void minipal_critsec_enter(DN_CRIT_SECT* cs)
{
    assert(cs != NULL);
#ifdef HOST_WINDOWS
    EnterCriticalSection(&cs->_impl);
#else
    int st = pthread_mutex_lock(&cs->_impl);
    assert(st == 0);
#endif // HOST_WINDOWS
}

void minipal_critsec_leave(DN_CRIT_SECT* cs)
{
    assert(cs != NULL);
#ifdef HOST_WINDOWS
    LeaveCriticalSection(&cs->_impl);
#else
    int st = pthread_mutex_unlock(&cs->_impl);
    assert(st == 0);
#endif // HOST_WINDOWS
}
