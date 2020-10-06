// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests that APCs are not executed if a thread never enters an
**          alertable state after they are queued.
**
**
**===================================================================*/

#include <palsuite.h>

const int ChildThreadSleepTime = 2000;
const int InterruptTime = 1000;

DWORD ChildThread;
BOOL InAPC;

/* synchronization events */
static HANDLE hSyncEvent1 = NULL;
static HANDLE hSyncEvent2 = NULL;

/* thread result because we have no GetExitCodeThread() API */
static BOOL bThreadResult = FAIL;


VOID PALAPI APCFunc_QueueUserAPC_test2(ULONG_PTR dwParam)
{
    InAPC = TRUE;
}

DWORD PALAPI SleeperProc_QueueUserAPC_test2(LPVOID lpParameter)
{
    DWORD ret;

    /* signal the main thread that we're ready to proceed */
    if( !  SetEvent( hSyncEvent1 ) )
    {
       Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
       bThreadResult = FAIL;
       goto done;
    }

    /* wait for notification from the main thread */
    ret = WaitForSingleObject( hSyncEvent2, 20000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        bThreadResult = FAIL;
        goto done;
    }

    /* call our sleep function */
    Sleep( ChildThreadSleepTime );

    /* success if we reach here */
    bThreadResult = PASS;


done:

    /* signal the main thread that we're finished */
    if( !  SetEvent( hSyncEvent1 ) )
    {
       Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
       bThreadResult = FAIL;
    }

    /* return success or failure */
    return bThreadResult;
}


PALTEST(threading_QueueUserAPC_test2_paltest_queueuserapc_test2, "threading/QueueUserAPC/test2/paltest_queueuserapc_test2")
{
    /* local variables */
    HANDLE hThread = 0;
    int ret;
    BOOL bResult = FAIL;

    /* initialize the PAL */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    InAPC = FALSE;

    /* create a pair of synchronization events to coordinate our threads */
    hSyncEvent1 = CreateEvent( NULL, FALSE, FALSE, NULL );
    if( hSyncEvent1 == NULL )
    {
        Trace( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    hSyncEvent2 = CreateEvent( NULL, FALSE, FALSE, NULL );
    if( hSyncEvent2 == NULL )
    {
        Trace( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* create a child thread */
    hThread = CreateThread( NULL,
                            0,
                            (LPTHREAD_START_ROUTINE)SleeperProc_QueueUserAPC_test2,
                            0,
                            0,
                            &ChildThread);

    if (hThread == NULL)
    {
        Trace( "ERROR:%lu:CreateThread() call failed\n",
            GetLastError());
        goto cleanup;
    }


    /* wait on our synchronization event to ensure the thread is running */
    ret = WaitForSingleObject( hSyncEvent1, 20000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        goto cleanup;
    }

    /* queue a user APC on the child thread */
    ret = QueueUserAPC(APCFunc_QueueUserAPC_test2, hThread, 0);
    if (ret == 0)
    {
        Trace( "ERROR:%lu:QueueUserAPC() call failed\n",
            GetLastError());
        goto cleanup;
    }

    /* signal the child thread to continue */
    if( ! SetEvent( hSyncEvent2 ) )
    {
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* wait on our synchronization event to ensure the other thread is done */
    ret = WaitForSingleObject( hSyncEvent1, 20000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        goto cleanup;
    }

    /* check that the thread executed successfully */
    if( bThreadResult == FAIL )
    {
        goto cleanup;
    }


    /* check whether the APC function was executed */
    if( InAPC )
    {
        Trace( "FAIL:APC function was executed but shouldn't have been\n" );
        goto cleanup;
    }

    /* success if we reach here */
    bResult = PASS;


cleanup:
    /* wait for the other thread to finish */
    if( hThread != NULL )
    {
        ret = WaitForSingleObject( hThread, INFINITE );
        if (ret == WAIT_FAILED)
        {
            Trace( "ERROR:%lu:WaitForSingleObject() returned %lu, "
                    "expected WAIT_OBJECT_0\n",
                    ret );
            bResult = FAIL;
        }
    }

    /* close our synchronization handles */
    if( hSyncEvent1 != NULL )
    {
        if( ! CloseHandle( hSyncEvent1 ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
            bResult = FAIL;
        }
    }

    if( hSyncEvent2 != NULL )
    {
        if( ! CloseHandle( hSyncEvent2 ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
            bResult = FAIL;
        }
    }

    if( bResult == FAIL )
    {
        Fail( "test failed\n" );
    }


    /* terminate the PAL */
    PAL_Terminate();

    /* return success */
    return PASS;
}
