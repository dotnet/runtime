// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test5.c
**
** Purpose: Positive test for OpenEventW.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               CreateEvent
**               CloseHandle
**               WaitForSingleObject
**
** Purpose:
**
** Test to ensure proper operation of the OpenEventW()
** API by creating a new named event with CreateEventW()
** and verifying that it can be opened with OpenEventW().
** It should be possible to use the event handles
** interchangeably, we test by setting the event with the
** original handle and waiting on it with the new one,
** then resetting it with the new one and waiting
** on it with the original one.
**
**
**===========================================================================*/
#include <palsuite.h>



PALTEST(threading_OpenEventW_test5_paltest_openeventw_test5, "threading/OpenEventW/test5/paltest_openeventw_test5")

{
    /* local variables */
    BOOL                    ret = FAIL;
    DWORD                   dwRet = 0;
    HANDLE                  hEvent = NULL;
    HANDLE                  hTestEvent = NULL;
    LPSECURITY_ATTRIBUTES   lpEventAttributes = NULL;
    BOOL                    bManualReset = TRUE;
    BOOL                    bInitialState = FALSE;
    WCHAR                   wcName[] = {'S','h','a','k','e','I','t','\0'};
    LPWSTR                  lpNameW = wcName;


    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* create an event which we can use with SetEvent */
    hEvent = CreateEvent( lpEventAttributes,
                          bManualReset,
                          bInitialState,
                          lpNameW );

    if( hEvent == NULL )
    {
        /* ERROR */
        Fail( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
    }

    /* open a new handle to our event */
    hTestEvent = OpenEventW(EVENT_ALL_ACCESS,  /* we want all rights */
                            FALSE,             /* no inherit         */
                            lpNameW );

    if( hTestEvent == NULL )
    {
        /* ERROR */
        Trace( "ERROR:%lu:OpenEventW() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* verify that the event isn't signalled yet by waiting on both */
    /* handles to the event object                                  */
    dwRet = WaitForSingleObject( hEvent, 0 );
    if( dwRet != WAIT_TIMEOUT )
    {
        /* ERROR */
        Trace( "ERROR:WaitForSingleObject() call returned %lu, "
                "expected WAIT_TIMEOUT\n",
                dwRet );
        goto cleanup;
    }

    dwRet = WaitForSingleObject( hTestEvent, 0 );
    if( dwRet != WAIT_TIMEOUT )
    {
        /* ERROR */
        Trace( "ERROR:WaitForSingleObject() call returned %lu, "
                "expected WAIT_TIMEOUT\n",
                dwRet );
        goto cleanup;
    }


    /* set the event using the original handle */
    if( ! SetEvent( hEvent ) )
    {
        /* ERROR */
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* verify that the event is signalled by waiting on both handles */
    dwRet = WaitForSingleObject( hEvent, 0 );
    if( dwRet != WAIT_OBJECT_0 )
    {
        /* ERROR */
        Trace( "ERROR:WaitForSingleObject() call returned %lu, "
                "expected WAIT_OBJECT_0\n",
                dwRet );
        goto cleanup;
    }

    dwRet = WaitForSingleObject( hTestEvent, 0 );
    if( dwRet != WAIT_OBJECT_0 )
    {
        /* ERROR */
        Trace( "ERROR:WaitForSingleObject() call returned %lu, "
                "expected WAIT_OBJECT_0\n",
                dwRet );
        goto cleanup;
    }

    /* reset the event using the new handle */
    if( ! ResetEvent( hTestEvent ) )
    {
        /* ERROR */
        Trace( "ERROR:%lu:ResetEvent() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* verify that the event isn't signalled by waiting on both  handles */
    dwRet = WaitForSingleObject( hEvent, 0 );
    if( dwRet != WAIT_TIMEOUT )
    {
        /* ERROR */
        Trace( "ERROR:WaitForSingleObject() call returned %lu, "
                "expected WAIT_TIMEOUT\n",
                dwRet );
        goto cleanup;
    }

    dwRet = WaitForSingleObject( hTestEvent, 0 );
    if( dwRet != WAIT_TIMEOUT )
    {
        /* ERROR */
        Trace( "ERROR:WaitForSingleObject() call returned %lu, "
                "expected WAIT_TIMEOUT\n",
                dwRet );
        goto cleanup;
    }


    /* test was successful */
    ret = PASS;


cleanup:
    /* close the new event handle */
    if( hTestEvent != NULL )
    {
        if( ! CloseHandle( hTestEvent ) )
        {
            ret = FAIL;
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
    }

    /* close the original event handle */
    if( ! CloseHandle( hEvent ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
    }

    /* failure message */
    if( ret != PASS )
    {
        Fail( "Test failed\n" );
    }


    /* PAL termination */
    PAL_TerminateEx(ret);

    /* return success or failure */
    return ret;
}
