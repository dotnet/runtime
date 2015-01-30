//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: testlib.c (filemapping_memmgt\getprocaddress\test1)
**
** Purpose: Create a simple library containing one function
**          to test GetProcAddress
**
**
**===========================================================================*/
#include "pal.h"

#if WIN32
__declspec(dllexport)
#endif

/**
 * Simple function that returns i+1
 */
int __stdcall SimpleFunction(int i)
{
    return i+1;
}

#if WIN32
int __stdcall _DllMainCRTStartup(void *hinstDLL, int reason, void *lpvReserved)
{
    return 1;
}
#endif
