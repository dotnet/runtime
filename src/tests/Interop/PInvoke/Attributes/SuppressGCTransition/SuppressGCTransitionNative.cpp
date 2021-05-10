// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <cstdint>
#include <atomic>
#include <string>
#include <algorithm>

namespace
{
    std::atomic<uint32_t> _n{ 0 };
}

extern "C"
BOOL DLL_EXPORT STDMETHODVCALLTYPE NextUInt(/* out */ uint32_t *n)
{
    if (n == nullptr)
        return FALSE;

    *n = (++_n);
    return TRUE;
}

typedef int (STDMETHODVCALLTYPE *CALLBACKPROC)(int n);

extern "C"
BOOL DLL_EXPORT STDMETHODVCALLTYPE InvokeCallback(CALLBACKPROC cb, int* n)
{
    if (cb == nullptr || n == nullptr)
        return FALSE;

    *n = cb((++_n));
    return TRUE;
}
