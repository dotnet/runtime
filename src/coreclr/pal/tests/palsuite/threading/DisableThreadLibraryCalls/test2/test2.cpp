// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test2.c
**
** Purpose: Test to ensure DisableThreadLibraryCalls() called for one
**          library will not disrupt THREAD_ATTACH notifications etc. by
**          other loaded modules.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               CreateThread
**               ResumeThread
**               LoadLibrary
**               FreeLibrary
**               GetProcAddress
**               WaitForSingleObject
**               GetLastError
**
**
**===========================================================================*/
#include <palsuite.h>

/* SHLEXT is defined only for Unix variants */

#if defined(SHLEXT)
#define rgchLibraryFile1 "dllmain1"SHLEXT
#define rgchLibraryFile2 "dllmain2"SHLEXT
#define szFunction       "GetAttachCount"
#else
#define rgchLibraryFile1 "dllmain1"
#define rgchLibraryFile2 "dllmain2"
#define szFunction       "_GetAttachCount@0"
#endif

/* define our test function type */
typedef int ( PALAPI *LPTESTFUNC )( void );



/**
 * ThreadFunc
 *
 * Dummy thread function for causing DLL thread notifications.
 */
DWORD PALAPI ThreadFunc_DisableThreadLibraryCalls_test2( LPVOID param )
{
    /* simulate some brief "work" */
    int i;
    for( i=0; i<100000; i++ )
    {
    }

    return 0;
}


/* main program entry point */
int __cdecl main( int argc, char **argv )

{
    /* local variables */

    HANDLE      hLib1 = NULL;
    HANDLE      hLib2 = NULL;
    LPTESTFUNC  pFunc1;
    LPTESTFUNC  pFunc2;
    int         attachCount1a = 0;
    int         attachCount1b = 0;
    int         attachCount2a = 0;
    int         attachCount2b = 0;

    HANDLE      hThread = NULL;
    DWORD       IDThread;
    DWORD       dwRet;

    BOOL        bResult = FAIL;

    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* Load the first test library */
    hLib1 = LoadLibrary( rgchLibraryFile1 );
    if(hLib1 == NULL)
    {
        Fail(   "ERROR:%lu:LoadLibrary() call failed for %s\n",
                GetLastError(),
                rgchLibraryFile1 );
    }

    /* Load the second test library */
    hLib2 = LoadLibrary( rgchLibraryFile2 );
    if(hLib2 == NULL)
    {
        Trace( "ERROR:%lu:LoadLibrary() call failed for %s\n",
                GetLastError(),
                rgchLibraryFile2 );
        if( ! FreeLibrary( hLib1 ) )
        {
            Trace( "ERROR:%lu:FreeLibrary() call failed\n", GetLastError() );
        }
        Fail( "test failed\n" );
    }


    /* Get the addresses of our test functions in the dlls */
    pFunc1 = (LPTESTFUNC)GetProcAddress( hLib1, szFunction );
    if( pFunc1 == NULL )
    {
        Trace( "ERROR:%lu%:Unable to find \"%s\" in library \"%s\"\n",
                GetLastError(),
                szFunction,
                rgchLibraryFile1 );
        goto cleanup;
    }

    pFunc2 = (LPTESTFUNC)GetProcAddress( hLib2, szFunction );
    if( pFunc1 == NULL )
    {
        Trace( "ERROR:%lu%:Unable to find \"%s\" in library \"%s\"\n",
                GetLastError(),
                szFunction,
                rgchLibraryFile2 );
        goto cleanup;
    }

    /* disable thread library calls for the first library */
    if( ! DisableThreadLibraryCalls( (HMODULE)hLib1 ) )
    {
        Trace( "ERROR:%lu:DisableThreadLibraryCalls() call failed\n",
                GetLastError() );
        goto cleanup;
    }

    /* Execute the test function to get the attach count */
    attachCount1a = pFunc1();
    attachCount2a = pFunc2();

    /* run another dummy thread to cause notification of the libraries     */
    hThread = CreateThread(    NULL,             /* no security attributes */
                               0,                /* use default stack size */
      (LPTHREAD_START_ROUTINE) ThreadFunc_DisableThreadLibraryCalls_test2,       /* thread function        */
                      (LPVOID) NULL,             /* pass thread index as   */
                                                 /* function argument      */
                               CREATE_SUSPENDED, /* create suspended       */
                               &IDThread );      /* returns thread id      */

    /* Check the return value for success. */
    if( hThread == NULL )
    {
        /* error creating thread */
        Trace( "ERROR:%lu:CreateThread() call failed\n",
              GetLastError() );
        goto cleanup;
    }

    /* Resume the suspended thread */
    if( ResumeThread( hThread ) == -1 )
    {
        Trace( "ERROR:%lu:ResumeThread() call failed\n", GetLastError() );
        goto cleanup;
    }


    /* wait for the thread to complete */
    dwRet = WaitForSingleObject( hThread, INFINITE );
    if( dwRet != WAIT_OBJECT_0 )
    {
        Trace( "ERROR: WaitForSingleObject returned %lu, "
                "expected WAIT_OBJECT_0\n",
                dwRet );
        goto cleanup;
    }


    /* Execute the test function to get the new detach count */
    attachCount1b = pFunc1();
    attachCount2b = pFunc2();

    /* validate the result */
    if( attachCount1b != attachCount1a )
    {
        Trace( "FAIL: unexpected DLL attach count %d, expected %d\n",
                attachCount1b,
                attachCount1a );
        goto cleanup;
    }

    /* validate the result */
    if( attachCount2b != (attachCount2a + 1) )
    {
        Trace( "FAIL: unexpected DLL attach count %d, expected %d\n",
                attachCount2b,
                (attachCount2a + 1) );
        goto cleanup;
    }

    bResult = PASS;

cleanup:
    /* Unload the test libraries */
    if( !FreeLibrary( hLib1 ) )
    {
        Trace( "ERROR:%u:FreeLibrary() failed on library \"%s\"\n",
                GetLastError(),
                rgchLibraryFile1 );
        bResult = FAIL;
    }

    if( !FreeLibrary( hLib2 ) )
    {
        Trace( "ERROR:%u:FreeLibrary() failed on library \"%s\"\n",
                GetLastError(),
                rgchLibraryFile2 );
        bResult = FAIL;
    }

    /* check for failure */
    if( bResult == FAIL )
    {
        Fail( "test failed\n" );
    }

    /* terminate the PAL */
    PAL_Terminate();

    /* return success */
    return PASS;
}

