// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test.c
**
** Purpose: Test to ensure TlsAlloc, PAL_MakeOptimizedTlsGetter, 
**  PAL_FreeOptimizedTlsGetter and TlsFree are working properly 
**  on supported platforms
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
#define THREAD_COUNT  5
DWORD dwTlsIndex; /* TLS index */

void PALAPI Run_Thread(LPVOID lpParam);

/**
 * main
 *
 * executable entry point
 */
INT __cdecl main( INT argc, CHAR **argv )
{
    DWORD  dwParam;
    DWORD  dwError;
    HANDLE hThread[THREAD_COUNT];
    DWORD  threadId[THREAD_COUNT];
    
    int i = 0;   
    int returnCode = 0;

    /*PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
	    return FAIL;
    }

    /*Allocate a TLS index. */
    if ((dwTlsIndex = TlsAlloc()) == TLS_OUT_OF_INDEXES)
    {   
        /*ERROR*/
        dwError = GetLastError();
        Fail("TlsAlloc() returned error %d\n",
              dwError);
    }


    for( i = 0; i < THREAD_COUNT; i++ )
    {
        dwParam = (int) i;
        //Create thread
        hThread[i] = CreateThread(
                                    NULL,                   /* no security attributes */
                                    0,                      /* use default stack size */
                                    (LPTHREAD_START_ROUTINE)Run_Thread,/* thread function */
                                    (LPVOID)dwParam,  /* argument to thread function */
                                    0,                      /* use default creation flags  */
                                    &threadId[i]     /* returns the thread identifier*/                                  
                                  );

        if(hThread[i] == NULL)
        {
            Fail("Create Thread failed for iteration %d GetLastError value is %d\n", i, GetLastError());
        }
  
    } 


    returnCode = WaitForMultipleObjects(THREAD_COUNT, hThread, TRUE, INFINITE);
    if( WAIT_OBJECT_0 != returnCode )
    {
        Trace("Wait for Object(s) returned %d, expected value is  %d, and GetLastError value is %d\n", returnCode, WAIT_OBJECT_0, GetLastError());
    }

    /* Release the TLS index */
    if( TlsFree( dwTlsIndex ) == 0 )
    {
        /* ERROR */
        dwError = GetLastError();
        Fail("TlsFree() returned 0 with error %d\n",
             dwError);
    }

    PAL_Terminate();
    return PASS;

}

void  PALAPI Run_Thread (LPVOID lpParam)
{
    unsigned int i = 0;

    LPVOID lpvData;
    DWORD  dwError;
    PAL_POPTIMIZEDTLSGETTER ptrOptimizedTlsGetter;

    int Id=(int)lpParam;


    lpvData = TlsGetValue(dwTlsIndex);
    if ( (lpvData != NULL) &&
        ((dwError = GetLastError()) == NO_ERROR) ) 
    {
        /*ERROR */
        Fail("Error:%d:TlsGetValue(%d) returned data "
            "even if data was not associated with the index, for thread id [%d]\n",
			dwError, dwTlsIndex, Id);
    }


    /* Initialize the TLS index for this thread.*/
    lpvData = (LPVOID) LocalAlloc(0, 256);

    if( lpvData == NULL )
    {
        /*ERROR */
        dwError = GetLastError();
        Fail("Unexpected LocalAlloc(0, 256) failure with error %d\n",
			  dwError);
    }

    if ( TlsSetValue(dwTlsIndex, lpvData) == 0 )
    {
        /*ERROR */
	    dwError = GetLastError();
        Fail("TlsSetValue(%d, %x) returned 0 with error %d\n",
			  dwTlsIndex,
			  lpvData,
			  dwError);
    }
                            
    ptrOptimizedTlsGetter = PAL_MakeOptimizedTlsGetter(dwTlsIndex);
    if( ptrOptimizedTlsGetter == NULL )
    {
         /* Retrieve a data pointer for the current thread. 
        The return value should be NULL since no data has been 
        set in the index */
        lpvData = TlsGetValue(dwTlsIndex);
        Trace("Not Inside the optimizer loop for thread [%d]\n", Id);

        if ( (lpvData == NULL) &&
            ((dwError = GetLastError()) == NO_ERROR) ) 
        {
            /*ERROR */
            Fail("Error:%d:TlsGetValue(%d) returned data "
                "as NULL even if data was associated with the index, for thread id [%d]\n",
			    dwError, dwTlsIndex, Id);
        }
    }
    else
    {
        /* Retrieve a data pointer for the current thread. 
        The return value should be NULL since no data has been 
        set in the index */
        lpvData = ptrOptimizedTlsGetter();

        if ( (lpvData == NULL) &&
            ((dwError = GetLastError()) == NO_ERROR) ) 
        {
            /*ERROR */
            Fail(" Error:%d: MakeOptimizedTlsGetter for dwTlsIndex (%d) returned data "
                "as NULL even if no data was associated with the index, for thread id [%d]\n",
			    dwError, dwTlsIndex, Id);
        }

        Trace("Inside the optimizer loop for thread [%d]\n", Id);
        PAL_FreeOptimizedTlsGetter(ptrOptimizedTlsGetter);
    }




}

