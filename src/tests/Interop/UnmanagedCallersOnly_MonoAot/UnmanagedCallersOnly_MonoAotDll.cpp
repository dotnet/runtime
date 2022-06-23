// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

typedef int (__stdcall *CALLBACKPROC_STDCALL)(int n);
typedef int (__cdecl *CALLBACKPROC_CDECL)(int n);

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Stdcall(CALLBACKPROC_STDCALL pCallbackProc, int n)
{
    return pCallbackProc(n);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Cdecl(CALLBACKPROC_CDECL pCallbackProc, int n)
{
    return pCallbackProc(n);
}
