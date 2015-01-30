//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
