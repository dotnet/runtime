// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "../dn-critsec.h"

bool DnCritSec_Initialize(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    ::InitializeCriticalSection(&cs->_impl);
    return true;
}

void DnCritSec_Destroy(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    ::DeleteCriticalSection(&cs->_impl);
#ifdef _DEBUG
    cs->_impl = {};
#endif // _DEBUG
}

void DnCritSec_Enter(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    ::EnterCriticalSection(&cs->_impl);
}

void DnCritSec_Leave(DN_CRIT_SEC* cs)
{
    assert(cs != nullptr);
    ::LeaveCriticalSection(&cs->_impl);
}