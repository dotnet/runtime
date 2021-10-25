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

    using IsInCooperativeMode_fn = BOOL(STDMETHODCALLTYPE*)(void);
    IsInCooperativeMode_fn s_isInCooperativeMode = nullptr;
}

extern "C"
void DLL_EXPORT STDMETHODVCALLTYPE SetIsInCooperativeModeFunction(IsInCooperativeMode_fn fn)
{
    s_isInCooperativeMode = fn;
}

extern "C"
BOOL DLL_EXPORT STDMETHODVCALLTYPE NextUInt(/* out */ uint32_t* n)
{
    BOOL ret;
    if (n == nullptr)
    {
        ret = FALSE;
    }
    else
    {
        *n = (++_n);
        ret = TRUE;
    }

    if (s_isInCooperativeMode != nullptr)
        ret = s_isInCooperativeMode();

    return ret;
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
