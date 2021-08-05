// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

namespace
{
    using IsInCooperativeMode_fn = BOOL(STDMETHODCALLTYPE*)(void);
    IsInCooperativeMode_fn s_isInCooperativeMode = nullptr;

    BOOL Double(int a, int* b)
    {
        if (b != NULL)
            *b = a * 2;

        BOOL ret = FALSE;
        if (s_isInCooperativeMode != nullptr)
            ret = s_isInCooperativeMode();

        return ret;
    }
}

extern "C" void DLL_EXPORT STDMETHODCALLTYPE SetIsInCooperativeModeFunction(IsInCooperativeMode_fn fn)
{
    s_isInCooperativeMode = fn;
}

extern "C" DLL_EXPORT BOOL Double_Default(int a, int* b)
{
    return Double(a, b);
}

extern "C" DLL_EXPORT int __cdecl Double_Cdecl(int a, int* b)
{
    return Double(a, b);
}

extern "C" DLL_EXPORT int __stdcall Double_Stdcall(int a, int* b)
{
    return Double(a, b);
}
