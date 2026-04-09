// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
int PALAPI SimpleFunction(int i)
{
    return i+1;
}

#if WIN32
int PALAPI _DllMainCRTStartup(void *hinstDLL, int reason, void *lpvReserved)
{
    return 1;
}
#endif
