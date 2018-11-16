// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <xplatform.h>

bool g_myResourceReleaseMethodCalled = false;

extern "C" DLL_EXPORT void __stdcall MyResourceReleaseMethod(HANDLE hnd)
{
    g_myResourceReleaseMethodCalled = true;
    
    //call CloseHandle to actually release the handle corresponding to the SafeFileHandle
    CloseHandle(hnd);
}

extern "C" DLL_EXPORT bool GetMyResourceReleaseMethodCalled()
{
    return g_myResourceReleaseMethodCalled;
}

extern "C" DLL_EXPORT void ResetMyResourceReleaseMethodCalled()
{
    g_myResourceReleaseMethodCalled = false;
}

extern "C" DLL_EXPORT void __stdcall SHReleasing_OutParams(IUnknown* ppIUnknFOO, HANDLE* phnd, IUnknown** ppIUnknBAR, int* pInt)
{
    //initialize the hnd out param
    *phnd = (HANDLE)123;

    //initialize the IUnknBAR out param
    *ppIUnknBAR = ppIUnknFOO;
    ppIUnknFOO->AddRef(); //addref Foo

    //initialize the int out param
    *pInt = 123;
}
