//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: dllmain1.c
**
** Purpose: Test to ensure DllMain() is called with THREAD_ATTACH
**          when a thread in the application is started.
**
** Dependencies: none
**
**
**===========================================================================*/

#include <palsuite.h>

/* count of the number of times DllMain() was called with THREAD_DETACH */
static int g_attachCount = 0;


/* standard DllMain() */
BOOL PALAPI DllMain(HINSTANCE hinstDLL, DWORD reason, LPVOID lpvReserved)
{
    BOOL bResult = TRUE;

    switch( reason )
    {
        case DLL_PROCESS_ATTACH:
        {
            break;
        }

        case DLL_PROCESS_DETACH:
        {
            break;
        }

        case DLL_THREAD_ATTACH:
            /* increment g_attachCount */
            g_attachCount++;
            break;

        case DLL_THREAD_DETACH:
            break;
    }
    return bResult;
}


#ifdef WIN32
BOOL __stdcall _DllMainCRTStartup(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    return DllMain(hinstDLL, fdwReason, lpvReserved);
}
#endif

/* function to return the current attach count */
#ifdef WIN32
__declspec(dllexport)
#endif
int PALAPI GetAttachCount( void )
{
    return g_attachCount;
}
