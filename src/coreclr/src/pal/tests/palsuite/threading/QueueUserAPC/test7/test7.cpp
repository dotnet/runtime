// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test7.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               CreateEvent
**               SetEvent
**               CreateThread
**               ResumeThread
**               WaitForMultipleObjectsEx
**               CloseHandle
**
** Purpose:
**
** Test to ensure proper operation of the QueueUserAPC()
** API by trying to queue an APC function on a thread and
** activating it with WaitForMultipleObjectsEx.
**
**
**===========================================================================*/
#include <palsuite.h>


static HANDLE hSyncEvent_QueueUserAPC_test7 = NULL;
static HANDLE hTestEvent_QueueUserAPC_test7 = NULL;
static int nAPCExecuted_QueueUserAPC_test7 = 0;
static BOOL bThreadResult_QueueUserAPC_test7 = FALSE;

VOID PALAPI APCFunc_QueueUserAPC_test7( ULONG_PTR dwParam )
{
    ++nAPCExecuted_QueueUserAPC_test7;
}

/**
 * ThreadFunc
 *
 * Dummy thread function for APC queuing.
 */
DWORD PALAPI ThreadFunc_QueueUserAPC_test7( LPVOID param )
{
    DWORD ret = 0;

    /* pessimism */
    bThreadResult_QueueUserAPC_test7 = FALSE;

    /* set the sync event to notify the main thread */
    if( ! SetEvent( hSyncEvent_QueueUserAPC_test7 ) )
    {
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        goto done;
    }

    /* wait until the test event is signalled */
    ret = WaitForSingleObject( hTestEvent_QueueUserAPC_test7, INFINITE );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
		goto done;
    }

    /* now do an alertable wait on the same event, which is now
       in an unsignalled state */
    ret = WaitForMultipleObjectsEx( 1, &hTestEvent_QueueUserAPC_test7, TRUE, 2000, TRUE );

    /* verify that we got a WAIT_IO_COMPLETION result */
    if( ret != WAIT_IO_COMPLETION )
    {
        Trace( "ERROR:WaitForMultipleObjectsEx returned %lu, "
                "expected WAIT_IO_COMPLETION\n",
                ret );
        goto done;
    }

    /* set the event again */
    if( ! SetEvent( hTestEvent_QueueUserAPC_test7 ) )
    {
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        goto done;
    }

    /* do a non-alertable wait on the same event */
    ret = WaitForMultipleObjectsEx( 1, &hTestEvent_QueueUserAPC_test7, TRUE, INFINITE, FALSE );

    /* verify that we got a WAIT_OBJECT_0 result */
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForMultipleObjectsEx returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        goto done;
    }

    /* success at this point */
    bThreadResult_QueueUserAPC_test7 = TRUE;


done:
    return bThreadResult_QueueUserAPC_test7;
}


PALTEST(threading_QueueUserAPC_test7_paltest_queueuserapc_test7, "threading/QueueUserAPC/test7/paltest_queueuserapc_test7")

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

    /* create an auto-reset event for the other thread to wait on */
    hTestEvent_QueueUserAPC_test7 = CreateEvent( NULL, FALSE, FALSE, NULL );
    if( hTestEvent_QueueUserAPC_test7 == NULL )
    {
        Fail( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
    }

    /* create an auto-reset event for synchronization */
    hSyncEvent_QueueUserAPC_test7 = CreateEvent( NULL, FALSE, FALSE, NULL );
    if( hSyncEvent_QueueUserAPC_test7 == NULL )
    {
        Trace( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
        if( ! CloseHandle( hTestEvent_QueueUserAPC_test7 ) )
        {
            Trace( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
        }
        Fail( "test failed\n" );
    }

    /* run another dummy thread to cause notification of the library       */
    hThread = CreateThread(    NULL,             /* no security attributes */
                               0,                /* use default stack size */
      (LPTHREAD_START_ROUTINE) ThreadFunc_QueueUserAPC_test7,       /* thread function        */
                      (LPVOID) NULL,             /* pass thread index as   */
                                                 /* function argument      */
                               CREATE_SUSPENDED, /* create suspended       */
                               &IDThread );      /* returns thread id      */

    /* Check the return value for success. */
    if( hThread == NULL )
    {
        /* error creating thread */
        Trace( "ERROR:%lu:CreateThread call failed\n", GetLastError() );
        if( ! CloseHandle( hTestEvent_QueueUserAPC_test7 ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "test failed\n" );
    }

    /* Resume the suspended thread */
    ResumeThread( hThread );

    /* wait until the other thread is ready to proceed */
    ret = WaitForSingleObject( hSyncEvent_QueueUserAPC_test7, 10000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        goto cleanup;
    }


    /* now queue our APC on the test thread */
    ret = QueueUserAPC( APCFunc_QueueUserAPC_test7, hThread, 0 );
    if( ret == 0 )
    {
        Trace( "ERROR:%lu:QueueUserAPC call failed\n", GetLastError() );
        goto cleanup;
    }

    /* signal the test event so the other thread will proceed */
    if( ! SetEvent( hTestEvent_QueueUserAPC_test7 ) )
    {
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* wait on the other thread to complete */
    ret = WaitForSingleObject( hThread, INFINITE );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        goto cleanup;
    }

    /* check the result of the other thread */
    if( bThreadResult_QueueUserAPC_test7 == FALSE )
    {
        goto cleanup;
    }

    /* check that the APC function was actually executed exactly one time */
    if( nAPCExecuted_QueueUserAPC_test7 != 1 )
    {
        Trace( "ERROR:APC function was executed %d times, "
                "expected once\n", nAPCExecuted_QueueUserAPC_test7 );
        goto cleanup;
    }

    /* set the success flag */
    bResult = PASS;


cleanup:
    /* close the global event handles */
    if( ! CloseHandle( hTestEvent_QueueUserAPC_test7 ) )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        bResult = FAIL;
    }

    if( ! CloseHandle( hSyncEvent_QueueUserAPC_test7 ) )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
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
