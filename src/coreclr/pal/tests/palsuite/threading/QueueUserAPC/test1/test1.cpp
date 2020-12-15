// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that APCs sent to a thread in an alertable state via
**          QueueUserAPC are executed in FIFO order.  Also tests that the APC
**          function is executed within the context of the correct thread and
**          that the dwData parameter gets sent correctly.
**
**
**===================================================================*/

#include <palsuite.h>

const int ChildThreadSleepTime = 2000;
const int InterruptTime = 1000;

VOID PALAPI APCFuncA(ULONG_PTR dwParam);
VOID PALAPI APCFuncB(ULONG_PTR dwParam);
VOID PALAPI APCFuncC(ULONG_PTR dwParam);
VOID PALAPI APCFuncD(ULONG_PTR dwParam);
DWORD PALAPI SleeperProc_QueueUserAPC_test1(LPVOID lpParameter);

const char *ExpectedResults_QueueUserAPC_test1 = "A0B0C0D0A1B1C1D1A2B2C2D2A3B3C3D3";
char ResultBuffer_QueueUserAPC_test1[256];
char *ResultPtr_QueueUserAPC_test1;
DWORD ChildThread_QueueUserAPC_test1;

/* synchronization events */
static HANDLE hSyncEvent1_QueueUserAPC_test1 = NULL;
static HANDLE hSyncEvent2_QueueUserAPC_test1 = NULL;

/* thread result because we have no GetExitCodeThread() API */
BOOL bThreadResult_QueueUserAPC_test1 = FAIL;

PALTEST(threading_QueueUserAPC_test1_paltest_queueuserapc_test1, "threading/QueueUserAPC/test1/paltest_queueuserapc_test1")
{
    HANDLE hThread = NULL;
    int ret;
    int i,j;
    BOOL bResult = FAIL;

    PAPCFUNC APCFuncs[] =
    {
        APCFuncA,
        APCFuncB,
        APCFuncC,
        APCFuncD,
    };

    /* initialize the PAL */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ResultPtr_QueueUserAPC_test1 = ResultBuffer_QueueUserAPC_test1;

    /* create a pair of synchronization events to coordinate our threads */
    hSyncEvent1_QueueUserAPC_test1 = CreateEvent( NULL, FALSE, FALSE, NULL );
    if( hSyncEvent1_QueueUserAPC_test1 == NULL )
    {
        Trace( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    hSyncEvent2_QueueUserAPC_test1 = CreateEvent( NULL, FALSE, FALSE, NULL );
    if( hSyncEvent2_QueueUserAPC_test1 == NULL )
    {
        Trace( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* create a child thread which will call SleepEx */
    hThread = CreateThread( NULL,
                            0,
                            (LPTHREAD_START_ROUTINE)SleeperProc_QueueUserAPC_test1,
                            0,
                            0,
                            &ChildThread_QueueUserAPC_test1);

    if( hThread == NULL )
    {
        Trace( "ERROR:%lu:CreateThread() call failed\n",
            GetLastError());
        goto cleanup;
    }


    /* wait on our synchronization event to ensure the thread is running */
    ret = WaitForSingleObject( hSyncEvent1_QueueUserAPC_test1, 20000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        goto cleanup;
    }


    /* queue our user APC functions on the thread */
    for (i=0; i<4; i++)
    {
        for (j=0; j<sizeof(APCFuncs)/sizeof(APCFuncs[0]); j++)
        {
            ret = QueueUserAPC(APCFuncs[j], hThread, '0' + i);
            if (ret == 0)
            {
                Trace( "ERROR:%lu:QueueUserAPC() call failed\n",
                    GetLastError());
                goto cleanup;
            }
        }
    }

    /* signal the child thread to continue */
    if( ! SetEvent( hSyncEvent2_QueueUserAPC_test1 ) )
    {
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        goto cleanup;
    }


    /* wait on our synchronization event to ensure the other thread is done */
    ret = WaitForSingleObject( hSyncEvent1_QueueUserAPC_test1, 20000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        goto cleanup;
    }


    /* check that the thread executed successfully */
    if( bThreadResult_QueueUserAPC_test1 == FAIL )
    {
        goto cleanup;
    }


    /* check the result buffer */
    if (strcmp(ExpectedResults_QueueUserAPC_test1, ResultBuffer_QueueUserAPC_test1) != 0)
    {
        Trace( "FAIL:Expected the APC function calls to produce a result of "
            " \"%s\", got \"%s\"\n",
            ExpectedResults_QueueUserAPC_test1,
            ResultBuffer_QueueUserAPC_test1 );
        goto cleanup;
    }

    /* success if we get here */
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
    if( hSyncEvent1_QueueUserAPC_test1 != NULL )
    {
        if( ! CloseHandle( hSyncEvent1_QueueUserAPC_test1 ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
            bResult = FAIL;
        }
    }

    if( hSyncEvent2_QueueUserAPC_test1 != NULL )
    {
        if( ! CloseHandle( hSyncEvent2_QueueUserAPC_test1 ) )
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

VOID PALAPI APCFuncA(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread_QueueUserAPC_test1)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread_QueueUserAPC_test1);
    }

    *ResultPtr_QueueUserAPC_test1++ = 'A';
    *ResultPtr_QueueUserAPC_test1++ = val;
    *ResultPtr_QueueUserAPC_test1 = 0;
}

VOID PALAPI APCFuncB(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread_QueueUserAPC_test1)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread_QueueUserAPC_test1);
    }

    *ResultPtr_QueueUserAPC_test1++ = 'B';
    *ResultPtr_QueueUserAPC_test1++ = val;
    *ResultPtr_QueueUserAPC_test1 = 0;
}

VOID PALAPI APCFuncC(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread_QueueUserAPC_test1)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread_QueueUserAPC_test1);
    }

    *ResultPtr_QueueUserAPC_test1++ = 'C';
    *ResultPtr_QueueUserAPC_test1++ = val;
    *ResultPtr_QueueUserAPC_test1 = 0;
}

VOID PALAPI APCFuncD(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread_QueueUserAPC_test1)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread_QueueUserAPC_test1);
    }

    *ResultPtr_QueueUserAPC_test1++ = 'D';
    *ResultPtr_QueueUserAPC_test1++ = val;
    *ResultPtr_QueueUserAPC_test1 = 0;
}

/* Entry Point for child thread.  All it does is call SleepEx. */
DWORD PALAPI SleeperProc_QueueUserAPC_test1(LPVOID lpParameter)
{
    DWORD ret;

    /* signal the main thread that we're ready to proceed */
    if( !  SetEvent( hSyncEvent1_QueueUserAPC_test1 ) )
    {
       Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
       bThreadResult_QueueUserAPC_test1 = FAIL;
       goto done;
    }

    /* wait for notification from the main thread */
    ret = WaitForSingleObject( hSyncEvent2_QueueUserAPC_test1, 20000 );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        bThreadResult_QueueUserAPC_test1 = FAIL;
        goto done;
    }

    /* call SleepEx to activate any queued APCs */
    ret = SleepEx(ChildThreadSleepTime, TRUE);
    if (ret != WAIT_IO_COMPLETION)
    {
        Trace( "ERROR:SleepEx() call returned %lu, "
                "expected WAIT_IO_COMPLETION\n",
                ret );
        bThreadResult_QueueUserAPC_test1 = FAIL;
        goto done;
    }

    /* everything passed here */
    bThreadResult_QueueUserAPC_test1 = PASS;


done:
    /* signal the main thread that we're finished */
    if( !  SetEvent( hSyncEvent1_QueueUserAPC_test1 ) )
    {
       Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
       bThreadResult_QueueUserAPC_test1 = FAIL;
    }

    /* return success or failure */
    return bThreadResult_QueueUserAPC_test1;
}
