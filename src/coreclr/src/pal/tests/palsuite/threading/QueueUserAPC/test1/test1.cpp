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
DWORD PALAPI SleeperProc(LPVOID lpParameter);

const char *ExpectedResults = "A0B0C0D0A1B1C1D1A2B2C2D2A3B3C3D3";
char ResultBuffer[256];
char *ResultPtr;
DWORD ChildThread;

/* synchronization events */
static HANDLE hSyncEvent1 = NULL;
static HANDLE hSyncEvent2 = NULL;

/* thread result because we have no GetExitCodeThread() API */
BOOL bThreadResult = FAIL;

int __cdecl main (int argc, char **argv)
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

    ResultPtr = ResultBuffer;

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

    /* create a child thread which will call SleepEx */
    hThread = CreateThread( NULL,
                            0,
                            (LPTHREAD_START_ROUTINE)SleeperProc,
                            0,
                            0,
                            &ChildThread);

    if( hThread == NULL )
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


    /* check the result buffer */
    if (strcmp(ExpectedResults, ResultBuffer) != 0)
    {
        Trace( "FAIL:Expected the APC function calls to produce a result of "
            " \"%s\", got \"%s\"\n",
            ExpectedResults,
            ResultBuffer );
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

VOID PALAPI APCFuncA(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread);
    }

    *ResultPtr++ = 'A';
    *ResultPtr++ = val;
    *ResultPtr = 0;
}

VOID PALAPI APCFuncB(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread);
    }

    *ResultPtr++ = 'B';
    *ResultPtr++ = val;
    *ResultPtr = 0;
}

VOID PALAPI APCFuncC(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread);
    }

    *ResultPtr++ = 'C';
    *ResultPtr++ = val;
    *ResultPtr = 0;
}

VOID PALAPI APCFuncD(ULONG_PTR dwParam)
{
    char val = (int) dwParam;

    if (GetCurrentThreadId() != ChildThread)
    {
        Fail("Executing APC in thread %d, should be in %d!\n",
            GetCurrentThreadId(), ChildThread);
    }

    *ResultPtr++ = 'D';
    *ResultPtr++ = val;
    *ResultPtr = 0;
}

/* Entry Point for child thread.  All it does is call SleepEx. */
DWORD PALAPI SleeperProc(LPVOID lpParameter)
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

    /* call SleepEx to activate any queued APCs */
    ret = SleepEx(ChildThreadSleepTime, TRUE);
    if (ret != WAIT_IO_COMPLETION)
    {
        Trace( "ERROR:SleepEx() call returned %lu, "
                "expected WAIT_IO_COMPLETION\n",
                ret );
        bThreadResult = FAIL;
        goto done;
    }

    /* everything passed here */
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
