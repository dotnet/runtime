// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

BOOL PALAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
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
BOOL PALAPI _DllMainCRTStartup(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    return DllMain(hinstDLL, fdwReason, lpvReserved);
}
#endif

#ifdef WIN32
__declspec(dllexport)
#endif
int PALAPI GetCallCount()
{
    return Count;
}
