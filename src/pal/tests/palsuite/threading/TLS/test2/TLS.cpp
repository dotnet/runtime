// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: tls.c
**
** Purpose: Test to ensure TlsAlloc and TlsFree are working when we try 
**          to allocate the guaranted minimum number of indicies.
** 

**
**===========================================================================*/
#include <palsuite.h>

#define NUM_OF_INDEX    64
/* Minimum guaranteed is at least 64 for all systems.*/

/**
 * main
 *
 * executable entry point
 */
INT __cdecl main( INT argc, CHAR **argv )
{
    DWORD dwIndexes[NUM_OF_INDEX];
    int i,j;

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
	    return FAIL;
    }

    /* Allocate a bunch of TLS indexes. */
    for( i = 0; i < NUM_OF_INDEX; i++ )
    {
        if( (dwIndexes[i] = TlsAlloc()) == TLS_OUT_OF_INDEXES )
        {/*ERROR */
            DWORD dwError = GetLastError();
            Fail("TlsAlloc() returned -1 with error %d"
                 "when trying to allocate %d index\n",
                  dwError,
                  i);
        }
    }

    /* Free the TLS indexes.*/
    for( j = 0; j < NUM_OF_INDEX; j++ )
    {
        if( TlsFree(dwIndexes[j]) == 0 )
        {/* ERROR */
            DWORD dwError = GetLastError();
            Fail("TlsFree() returned 0 with error %d"
                 "when trying to free %d index\n",
                  dwError,
                  i);
        }
    }

    PAL_Terminate();

    return PASS;
}

