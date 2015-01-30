//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
** 
** Source:  dlltest.c (FreeLibraryAndExitThread test dll)
**
** Purpose: This file will be used to create the shared library
**          that will be used in the FreeLibraryAndExitThread
**          test. A very simple shared library, with one function
**          "DllTest" which merely returns 1.
**
**
**===================================================================*/
#include "pal.h"

#if WIN32
__declspec(dllexport)
#endif

int _stdcall DllTest()
{
    return 1;
}

#if WIN32
int __stdcall _DllMainCRTStartup(void *hinstDLL, int reason, void * lpvReserved)
{
    return 1;
}
#endif
