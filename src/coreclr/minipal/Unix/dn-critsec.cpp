// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "../dn-critsec.h"

bool DnCritSec_Initialize(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    pthread_mutexattr_t mutexAttributes;
    int st = pthread_mutexattr_init(&mutexAttributes);
    if (st != 0)
        return false;

    st = pthread_mutexattr_settype(&mutexAttributes, PTHREAD_MUTEX_RECURSIVE);
    if (st == 0)
        st = pthread_mutex_init(&cs->_impl, &mutexAttributes);

    pthread_mutexattr_destroy(&mutexAttributes);

    return (st == 0);
}

void DnCritSec_Destroy(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    int st = pthread_mutex_destroy(&cs->_impl);
    assert(st == 0);
#ifdef _DEBUG
    cs->_impl = {};
#endif // _DEBUG
}

void DnCritSec_Enter(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    int st = pthread_mutex_lock(&cs->_impl);
    assert(st == 0);
}

void DnCritSec_Leave(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    int st = pthread_mutex_unlock(&cs->_impl);
    assert(st == 0);
}