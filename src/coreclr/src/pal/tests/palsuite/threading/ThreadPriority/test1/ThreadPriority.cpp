// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: threadpriority.c
**
** Purpose: Test to ensure GetThreadPriority works properly.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               CreateThread
**               WaitForSingleObject
**               GetLastError
**               time()
** 

**
**===========================================================================*/
#include <palsuite.h>

/**
 * CheckThreadPriority
 *
 * Helper function that checks the current thread priority
 * against an expected value.
 */
static VOID CheckThreadPriority( HANDLE hThread, int expectedPriority )
{
    int priority;
    DWORD dwError = 0;

    /* get the current thread priority */
    priority = GetThreadPriority( hThread );
    if( priority == THREAD_PRIORITY_ERROR_RETURN )
    {
        /* GetThreadPriority call failed */
        dwError = GetLastError();
        Fail( "Unexpected GetThreadPriority() failure "
              "with error %d\n", dwError );
    }
    else if( priority != expectedPriority )
    {
        /* unexpected thread priority detected */
        Fail( "Unexpected initial thread priority value %d reported\n",
              priority );
    }
}


/**
 * main
 *
 * executable entry point
 */
PALTEST(threading_ThreadPriority_test1_paltest_threadpriority_test1, "threading/ThreadPriority/test1/paltest_threadpriority_test1")
{

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }

    /* set the thread priority of the main to the highest possible value
       this will give the chance to the main thread to create all the
       other threads */
    if(!SetThreadPriority( GetCurrentThread(), THREAD_PRIORITY_NORMAL))
    {
        DWORD dwError;

        dwError = GetLastError();
        Fail( "Unexpected SetThreadPriority() failure with error %d\n",
			  dwError );
    }

    CheckThreadPriority( GetCurrentThread(), THREAD_PRIORITY_NORMAL );
    //Add verification of timing out here..
    PAL_Terminate();
    return PASS;
}
