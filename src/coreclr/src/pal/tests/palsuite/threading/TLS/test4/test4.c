// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test4.c (threading/tls)
**
** Purpose: Test to ensure that upon key creation, the value NULL 
**          is associated with the new key in all active threads. 
**          Upon thread creation, the value NULL is associated 
**          with all defined keys in the new thread.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               LocalAlloc
**               LocalFree
** 

**
**===========================================================================*/
#include <palsuite.h>

#define NUM_OF_THREADS 10 

DWORD dwTlsIndex; /* TLS index */
 
/**
 * ThreadFunc
 *
 * Thread function that checks that NULL is associated with the tls index
 */
DWORD PALAPI ThreadFunc(VOID)
{
    LPVOID lpvData;
    DWORD dwError;
 
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

    return PASS;
}

/**
 * main
 *
 * executable entry point
 */
INT __cdecl main( INT argc, CHAR **argv )
{
    DWORD  IDThread;
    LPVOID lpvData;
    DWORD  dwError;
    HANDLE hThread[NUM_OF_THREADS];
    int i;

    /*PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
	    return FAIL;
    }

    /*Allocate a TLS index. */
    if ((dwTlsIndex = TlsAlloc()) == TLS_OUT_OF_INDEXES)
    {/*ERROR*/
        DWORD dwError = GetLastError();
        Fail("TlsAlloc() returned error %d\n",
              dwError);
    }

    /*Check that the value associated with the tls index is NULL*/
    lpvData = TlsGetValue(dwTlsIndex);

    if ( (lpvData != NULL) &&
         ((dwError = GetLastError()) == NO_ERROR) ) 
    {/*ERROR */
	    Fail("TlsGetValue(%d) returned non-null data "
             "even if no data was associated with the index\n",
			  dwTlsIndex);
    }

    /*Create multiple threads.*/
    for (i = 0; i < NUM_OF_THREADS; i++)
    {
        hThread[i] = CreateThread(NULL,       /* no security attributes*/
                                  0,          /* use default stack size */
         (LPTHREAD_START_ROUTINE) ThreadFunc, /* thread function */
                                  NULL,       /* no thread function argument */
                                  0,          /* use default creation flags */
                                  &IDThread); /* returns thread identifier */

        /* Check the return value for success. */
        if (hThread[i] == NULL)
	    {/* ERROR */
            DWORD dwError = GetLastError();
            Fail("Unexpected CreateThread error %d\n",
                 dwError);
	    }
    }

    /* Wait for all threads to finish */
    for (i = 0; i < NUM_OF_THREADS; i++)
    {
        DWORD dwRet;

        dwRet = WaitForSingleObject(hThread[i], INFINITE);

        if( dwRet == WAIT_FAILED )
        {/* ERROR */
            DWORD dwError = GetLastError();
            Fail("Unexpected WaitForSingleObject error %d\n",
                 dwError);
        }
    }

    /* Release the TLS index */
    if( TlsFree( dwTlsIndex ) == 0 )
    {/* ERROR */
        DWORD dwError = GetLastError();
        Fail("TlsFree() returned 0 with error %d\n",
             dwError);
    }

    PAL_Terminate();
    return PASS;
}

