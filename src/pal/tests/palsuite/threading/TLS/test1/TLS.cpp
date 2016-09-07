// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: tls.c
**
** Purpose: Test to ensure TlsAlloc, TlsGetValue, TlsSetValue
**          and TlsFree are working properly together.
**
** Dependencies: PAL_Initialize
**               Fail
**               Sleep
**               LocalAlloc
**               LocalFree
**               WaitForSingleObject
**               CreateThread
**               GetLastError
** 

**
**===========================================================================*/
#include <palsuite.h>

#define NUM_OF_THREADS 10 

DWORD dwTlsIndex; /* TLS index */
 
/**
 * CommonFunction
 *
 * Helper function that calls TlsGetValue
 */
VOID CommonFunction(VOID) 
{ 
    LPVOID lpvData;
    DWORD dwError;
 
    /* Retrieve a data pointer for the current thread. */
    lpvData = TlsGetValue(dwTlsIndex);

    if ( (lpvData == 0) &&
         ((dwError = GetLastError()) != NO_ERROR) ) 
    {/*ERROR */
	    Fail("TlsGetValue(%d) returned 0 with error %d\n",
			  dwTlsIndex,
		      dwError);
    }

    Sleep(5000);
}

/**
 * ThreadFunc
 *
 * Thread function that stores a value in the thread's tls slot
 * for the predefined tls index
 */
DWORD PALAPI ThreadFunc(LPVOID lpThreadParameter)
{
    LPVOID lpvData;
    DWORD dwError;

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

    CommonFunction();

    /* Release the dynamic memory. */
    lpvData = TlsGetValue(dwTlsIndex);

    if ( (lpvData == 0) &&
         ((dwError = GetLastError()) != NO_ERROR) )
    {/*ERROR */
	    Fail("TlsGetValue(%d) returned 0 with error %d\n",
			  dwTlsIndex,
			  dwError);
    }
    else
    {
	    if( LocalFree((HLOCAL) lpvData) != NULL )
        {
            dwError = GetLastError();
            Fail("Unexpected LocalFree(%x) failure with error %d\n",
                 lpvData,
			     dwError);
        }
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
    DWORD IDThread;
    HANDLE hThread[NUM_OF_THREADS];
    int i;

    /*PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
	    return FAIL;
    }

    /*Allocate a TLS index. */
    if ((dwTlsIndex = TlsAlloc()) == TLS_OUT_OF_INDEXES)
    {/*RROR*/
        DWORD dwError = GetLastError();
        Fail("TlsAlloc() returned error %d\n",
              dwError);
    }

    /*Create multiple threads.*/

    for (i = 0; i < NUM_OF_THREADS; i++)
    {
        hThread[i] = CreateThread(NULL,       /* no security attributes*/
                                  0,          /* use default stack size */
                                  ThreadFunc, /* thread function */
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

