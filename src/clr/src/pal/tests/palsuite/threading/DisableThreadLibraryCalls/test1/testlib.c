// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  testlib.c
**
** Purpose: Simple library that counts thread attach/detach notifications
**
**
**===================================================================*/

#include <palsuite.h>

static int Count;

BOOL __stdcall DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{

    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        Count = 0;
    }
    else if (fdwReason == DLL_THREAD_ATTACH ||
        fdwReason == DLL_THREAD_DETACH)
    {
        Count++;
    }

    return TRUE;
}

#ifdef WIN32
BOOL __stdcall _DllMainCRTStartup(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    return DllMain(hinstDLL, fdwReason, lpvReserved);
}
#endif

#ifdef WIN32
__declspec(dllexport)
#endif
int __stdcall GetCallCount()
{
    return Count;
}
