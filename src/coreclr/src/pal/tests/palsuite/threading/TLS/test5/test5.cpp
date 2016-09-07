// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test5.c (threading/tls)
**
** Purpose: Test that creates a key, sets its value, deletes the key, 
**          creates a new key, and gets its value to make sure its NULL.
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
    LPVOID lpvData;
    DWORD  dwError;

    /*PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
	    return FAIL;
    }

    /**
     * create a key, set its value, delete the key
     */

    /*Allocate a TLS index. */
    if ((dwTlsIndex = TlsAlloc()) == TLS_OUT_OF_INDEXES)
    {/*ERROR*/
        DWORD dwError = GetLastError();
        Fail("TlsAlloc() returned error %d\n",
              dwError);
    }

    /* Initialize the TLS index for this thread.*/
    lpvData = (LPVOID) LocalAlloc(0, 256);

    if( lpvData == NULL )
    {/*ERROR */
        dwError = GetLastError();
        Fail("Unexpected LocalAlloc(0, 256) failure with error %d\n",
			  dwError);
    }

    if ( TlsSetValue(dwTlsIndex, lpvData) == 0 )
    {/*ERROR */
	    dwError = GetLastError();
        Fail("TlsSetValue(%d, %x) returned 0 with error %d\n",
			  dwTlsIndex,
			  lpvData,
			  dwError);
    }

    /* Release the TLS index */
    if( TlsFree( dwTlsIndex ) == 0 )
    {/* ERROR */
        DWORD dwError = GetLastError();
        Fail("TlsFree() returned 0 with error %d\n",
             dwError);
    }


    /**
     * create a new key, and get its value
     */

    /*Allocate a TLS index. */
    if ((dwTlsIndex = TlsAlloc()) == TLS_OUT_OF_INDEXES)
    {/*ERROR*/
        DWORD dwError = GetLastError();
        Fail("TlsAlloc() returned error %d\n",
              dwError);
    }

    /* Retrieve a data pointer for the current thread. 
       The return value should be NULL since no data has been 
       set in the index */
    lpvData = TlsGetValue(dwTlsIndex);

    if ( (lpvData != NULL) &&
         ((dwError = GetLastError()) == NO_ERROR) ) 
    {/*ERROR */
	    Fail("TlsGetValue(%d) returned data "
             "even if no data was associated with the index\n",
			  dwTlsIndex);
    }

    PAL_Terminate();
    return PASS;
}

