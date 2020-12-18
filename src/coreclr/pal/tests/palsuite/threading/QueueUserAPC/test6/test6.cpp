// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test6.c
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
** API by trying to queue APC functions on a thread that
** has already terminated.
**
**
**===========================================================================*/
#include <palsuite.h>


static BOOL bAPCExecuted_QueueUserAPC_test6 = FALSE;

VOID PALAPI APCFunc_QueueUserAPC_test6( ULONG_PTR dwParam )
{
    bAPCExecuted_QueueUserAPC_test6 = TRUE;
}

/**
 * ThreadFunc
 *
 * Dummy thread function for APC queuing.
 */
DWORD PALAPI ThreadFunc_QueueUserAPC_test6( LPVOID param )
{
    int i;

    /* simulate some activity */
    for( i=0; i<250000; i++ )
        ;

    return 0;
}


PALTEST(threading_QueueUserAPC_test6_paltest_queueuserapc_test6, "threading/QueueUserAPC/test6/paltest_queueuserapc_test6")

{
    /* local variables */
    HANDLE hThread = NULL;
    DWORD  IDThread;
    DWORD  ret;

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }

    /* run another dummy thread to cause notification of the library       */
    hThread = CreateThread(    NULL,             /* no security attributes */
                               0,                /* use default stack size */
      (LPTHREAD_START_ROUTINE) ThreadFunc_QueueUserAPC_test6,       /* thread function        */
                      (LPVOID) NULL,             /* pass thread index as   */
                                                 /* function argument      */
                               CREATE_SUSPENDED, /* create suspended       */
                               &IDThread );      /* returns thread id      */

    /* Check the return value for success. */
    if( hThread == NULL )
    {
        /* error creating thread */
        Fail( "ERROR:%lu:CreateThread call failed\n", GetLastError() );
    }

    /* Resume the suspended thread */
    ResumeThread( hThread );

    /* wait on the other thread to complete */
    ret = WaitForSingleObject( hThread, INFINITE );
    if( ret != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0\n",
                ret );
        if( ! CloseHandle( hThread ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "test failed\n" );
    }

    /* queue our APC on the finished thread */
    ret = QueueUserAPC( APCFunc_QueueUserAPC_test6, hThread, 0 );
    if( ret != 0 )
    {
        Trace( "ERROR:QueueUserAPC call succeeded on a terminated thread\n" );
        if( ! CloseHandle( hThread ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "test failed\n" );
    }

    if( ! CloseHandle( hThread ) )
    {
        Fail( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
    }

    /* dummy check that the APC function wasn't actually executed */
    if( bAPCExecuted_QueueUserAPC_test6 != FALSE )
    {
        Fail( "ERROR:APC function was executed\n" );
    }


    /* PAL termination */
    PAL_Terminate();

    /* return success */
    return PASS;
}
