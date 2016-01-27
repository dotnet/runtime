// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test2.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               CreateThread
**               SetThreadPriority
**               GetThreadPriority
**               ResumeThread
**               WaitForSingleObject
**               GetLastError
**
** Purpose:
**
** Test to ensure proper operation of the GetCurrentThread()
** API. The test launches a thread in suspended mode, and sets
** its priority to a non-default value using the handle returned
** by CreateThread(). The new thread calls GetCurrentThred() to
** retrieve a handle to itself, and calls GetThreadPriority()
** to verify that its priority matches what it was set to on
** the main execution thread.
** 

**
**===========================================================================*/
#include <palsuite.h>


/* we store the return code from the child thread here because */
/* we're missing the GetExitCodeThread() API                   */

static int g_priority = 0;

/**
 * ThreadFunc
 *
 * Thread function that calls GetCurrentThread() to get a pseudo-handle
 * to itself, then checks its priority and exits with that value.
 */
DWORD PALAPI ThreadFunc( LPVOID param )
{
    int      priority;
    HANDLE   hThread;
    
    /* call GetCurrentThread() to get a pseudo-handle to */
    /* the current thread                                */
    hThread = GetCurrentThread();
    if( hThread == NULL )
    {
        Fail( "GetCurrentThread() call failed\n" );
    }
    
    
    /* get the current thread priority */
    priority = GetThreadPriority( hThread );
    if( priority == THREAD_PRIORITY_ERROR_RETURN )
    {
        /* GetThreadPriority call failed */
        Fail( "ERROR:%lu:GetThreadPriority() call failed\n", GetLastError() );
    }

    /* store this globally because we don't have GetExitCodeThread() */
    g_priority = priority;    
    return (DWORD)priority;
}


/**
 * main
 *
 * executable entry point
 */
INT __cdecl main( INT argc, CHAR **argv )
{
    HANDLE   hThread = NULL;
    DWORD    IDThread;
    DWORD    dwRet;

    SIZE_T i = 0;

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* Create multiple threads. */
    hThread = CreateThread(    NULL,         /* no security attributes    */
                               0,            /* use default stack size    */
      (LPTHREAD_START_ROUTINE) ThreadFunc,   /* thread function           */
                      (LPVOID) i,            /* pass thread index as      */
                                             /* function argument         */
                               CREATE_SUSPENDED, /* create suspended      */
                               &IDThread );  /* returns thread identifier */

    /* Check the return value for success. */
    if( hThread == NULL )
    {
        /* ERROR */
        Fail( "ERROR:%lu:CreateThread failed\n", GetLastError() );
    }

    /* set the thread priority of the new thread to the highest value */
    if( ! SetThreadPriority( hThread, THREAD_PRIORITY_TIME_CRITICAL) )
    {
        Fail( "ERROR:%lu:SetThreadPriority() call failed\n", GetLastError() );
    }

    /* let the child thread run now */    
    ResumeThread( hThread );


    /* wait for the thread to finish */
    dwRet = WaitForSingleObject( hThread, INFINITE );
    if( dwRet == WAIT_FAILED )
    {
        /* ERROR */
        Fail( "ERROR:%lu:WaitForSingleObject call failed\n", GetLastError() );
    }

    /* validate the thread's exit code */
    if( g_priority != THREAD_PRIORITY_TIME_CRITICAL )
    {
        /* ERROR */
        Fail( "FAIL:Unexpected thread priority %d returned, expected %d\n",
                g_priority, THREAD_PRIORITY_TIME_CRITICAL );
    }
    
    

    PAL_Terminate();
    return PASS;
}

