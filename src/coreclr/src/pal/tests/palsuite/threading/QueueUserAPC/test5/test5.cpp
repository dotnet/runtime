// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test5.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               CreateEvent
**               SetEvent
**               CreateThread
**               ResumeThread
**               WaitForSingleObject
**               CloseHandle
**
** Purpose:
**
** Test to ensure proper operation of the QueueUserAPC()
** API by trying to queue and activate APC functions on
** a thread that was created suspended, prior to resuming
** it. We're verifying the following behavior:
**
** "If an application queues an APC before the thread begins
** running, the thread begins by calling the APC function.
** After the thread calls an APC function, it calls the APC
** functions for all APCs in its APC queue."
**
**
**===========================================================================*/
#include <palsuite.h>


static HANDLE hEvent = NULL;
static BOOL bAPCExecuted = FALSE;

VOID PALAPI APCFunc( ULONG_PTR dwParam )
{
    bAPCExecuted = TRUE;
}

/**
 * ThreadFunc
 *
 * Dummy thread function for APC queuing.
 */
DWORD PALAPI ThreadFunc( LPVOID param )
{
    DWORD ret = 0;

    /* alertable wait until the global event is signalled */
    ret = WaitForSingleObject( hEvent, INFINITE );
    if( ret != WAIT_OBJECT_0 )
    {
        Fail( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
    }

    return 0;
}


int __cdecl main( int argc, char **argv )

{
    /* local variables */
    HANDLE hThread = NULL;
    DWORD  IDThread;
    DWORD  ret;
    BOOL   bResult = FALSE;

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }

    /* create an event for the other thread to wait on */
    hEvent = CreateEvent( NULL, TRUE, FALSE, NULL );
    if( hEvent == NULL )
    {
        Fail( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
    }

    /* run another dummy thread to cause notification of the library       */
    hThread = CreateThread(    NULL,             /* no security attributes */
                               0,                /* use default stack size */
      (LPTHREAD_START_ROUTINE) ThreadFunc,       /* thread function        */
                      (LPVOID) NULL,             /* pass thread index as   */
                                                 /* function argument      */
                               CREATE_SUSPENDED, /* create suspended       */
                               &IDThread );      /* returns thread id      */

    /* Check the return value for success. */
    if( hThread == NULL )
    {
        /* error creating thread */
        Trace( "ERROR:%lu:CreateThread call failed\n", GetLastError() );
        if( ! CloseHandle( hEvent ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "test failed\n" );
    }

    /* queue our APC on the suspended thread */
    ret = QueueUserAPC( APCFunc, hThread, 0 );
    if( ret == 0 )
    {
        Fail( "ERROR:%lu:QueueUserAPC call failed\n", GetLastError() );
    }

    /* wait on the suspended thread */
    ret = WaitForSingleObject( hThread, 2000 );
    if( ret != WAIT_TIMEOUT )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_TIMEOUT\n",
                ret );
        goto cleanup;
    }

    /* verify that the APC function was not executed */
    if( bAPCExecuted == TRUE )
    {
        Trace( "ERROR:APC function was executed for a suspended thread\n" );
        goto cleanup;
    }

    /* Resume the suspended thread */
    ResumeThread( hThread );

    /* do another wait on the resumed thread */
    ret = WaitForSingleObject( hThread, 2000 );

    /* verify that we got a WAIT_TIMEOUT result */
    if( ret != WAIT_TIMEOUT )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_TIMEOUT\n",
                ret );
        goto cleanup;
    }

    /* check that the APC function was actually executed */
    if( bAPCExecuted == FALSE )
    {
        Trace( "ERROR:APC function was not executed\n" );
        goto cleanup;
    }

    /* set the success flag */
    bResult = PASS;

cleanup:
    /* signal the event so the other thread will exit */
    if( ! SetEvent( hEvent ) )
    {
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        bResult = FAIL;
    }

    /* close the global event handle */
    if( ! CloseHandle( hEvent ) )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        bResult = FAIL;
    }

    /* wait on the other thread to complete */
    ret = WaitForSingleObject( hThread, 2000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        bResult = FAIL;
    }

    /* close the thread handle */
    if( ! CloseHandle( hThread ) )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        bResult = FAIL;
    }

    /* output final failure result for failure case */
    if( bResult == FAIL )
    {
        Fail( "test failed\n" );
    }

    /* PAL termination */
    PAL_Terminate();

    /* return success */
    return PASS;
}
