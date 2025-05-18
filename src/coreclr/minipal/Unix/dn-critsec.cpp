// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "../dn-critsec.h"

bool DnCritSec::Initialize()
{
    pthread_mutexattr_t mutexAttributes;
    int st = pthread_mutexattr_init(&mutexAttributes);
    if (st != 0)
        return false;

    st = pthread_mutexattr_settype(&mutexAttributes, PTHREAD_MUTEX_RECURSIVE);
    if (st == 0)
        st = pthread_mutex_init(&_cs, &mutexAttributes);

    pthread_mutexattr_destroy(&mutexAttributes);

    _isInitialized = (st == 0);
    return _isInitialized;
}

void DnCritSec::Destroy()
{
    if (!_isInitialized)
        return;

    _isInitialized = false;
    int st = pthread_mutex_destroy(&_cs);
    assert(st == 0);
}

void DnCritSec::Enter()
{
    assert(_isInitialized);
    int st = pthread_mutex_lock(&_cs);
    assert(st == 0);
}

void DnCritSec::Leave()
{
    assert(_isInitialized);
    int st = pthread_mutex_unlock(&_cs);
    assert(st == 0);
}