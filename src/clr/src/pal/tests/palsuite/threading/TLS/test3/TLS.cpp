// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: tls.c
**
** Purpose: Test to ensure TlsGetValue, TlsSetValue and TlsFree 
**          are not working with an invalid index
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               LocalAlloc
**               LocalFree
** 

**
**===========================================================================*/
#include <palsuite.h>

DWORD dwTlsIndex; /* TLS index */

/**
 * main
 *
 * executable entry point
 */
INT __cdecl main( INT argc, CHAR **argv )
{
    CHAR   lpstrData[256] = "";
    LPVOID lpvData = NULL;
    BOOL   bRet;

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
         return FAIL;
    }

    /* Invalid TLS index */
    dwTlsIndex = -1;

    /*
     * Set some data in the invalid TLS index
     *Should return 0 and an error
     */
    bRet = TlsSetValue(dwTlsIndex, (LPVOID)lpstrData);

    if ( bRet != 0)
    {/*ERROR */
        Fail("TlsSetValue(%d, %x) returned %d "
             "when it should have returned 0 and an error\n",
			  dwTlsIndex,
			  lpvData,
			  bRet);
    }

    /*
     * Get the data at the invalid index
     * Should return 0 and an error
     */
    lpvData = TlsGetValue(dwTlsIndex);

    if ( lpvData != 0 )
    {/* ERROR */
	    Fail("TlsGetValue(%d) returned %d "
             "when it should have returned 0 and an error\n",
			  dwTlsIndex,
			  lpvData);
    }

    /*
     * Release the invalid TLS index
     * Should return 0 and an error
     */
    bRet = TlsFree( dwTlsIndex );

    if(  bRet != 0 )
    {/* ERROR */
        Fail("TlsFree() returned %d "
             "when it should have returned 0 and an error\n",
			  bRet);
    }

    PAL_Terminate();
    return PASS;
}


