//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source:    dllmain.c
**
** Purpose: Test to ensure DllMain() is called with DLL_THREAD_DETACH
**          only the initial time that the library is loaded.
** 
** Depends: None
** 

**
**===========================================================================*/

#include <palsuite.h>

/* count of the number of times DllMain()
 * was called with THREAD_ATTACH.
 */
static int g_attachCount = 0;

/* standard DllMain() */
BOOL __stdcall DllMain(HINSTANCE hinstDLL, DWORD reason, LPVOID lpvReserved)
{
    switch( reason )
    {
        case DLL_PROCESS_ATTACH:
        {
            g_attachCount++;
            break;
        }
    
        case DLL_PROCESS_DETACH:
        {
            break;
        }
        
        case DLL_THREAD_ATTACH:
        {
            break;
        }
    
        case DLL_THREAD_DETACH:
        {
            break;
        }
    }

    return TRUE;
}

#if _WIN32
BOOL __stdcall _DllMainCRTStartup(HINSTANCE hinstDLL, DWORD reason, LPVOID lpvReserved)
{
    return DllMain(hinstDLL, reason, lpvReserved);
}
#endif



/* function to return the current attach count */
#if _WIN32
__declspec(dllexport)
#endif
int PALAPI GetAttachCount( void )
{
    return g_attachCount;
}

