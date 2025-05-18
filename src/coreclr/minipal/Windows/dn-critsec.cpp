// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "../dn-critsec.h"

bool DnCritSec::Initialize()
{
    ::InitializeCriticalSection(&_cs);
    _isInitialized = true;
    return true;
}

void DnCritSec::Destroy()
{
    if (!_isInitialized)
        return;

    _isInitialized = false;
    ::DeleteCriticalSection(&_cs);
}

void DnCritSec::Enter()
{
    assert(_isInitialized);
    ::EnterCriticalSection(&_cs);
}

void DnCritSec::Leave()
{
    assert(_isInitialized);
    ::LeaveCriticalSection(&_cs);
}