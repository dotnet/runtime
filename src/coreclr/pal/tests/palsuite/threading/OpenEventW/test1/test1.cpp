// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test for OpenEventW.  This test creates an event,
**          opens a handle to the same event, then waits on both handles
**          in both a signalled and non-signalled state to verify they're.
**          pointing to the same event object.
**
**
**==========================================================================*/
#include <palsuite.h>

PALTEST(threading_OpenEventW_test1_paltest_openeventw_test1, "threading/OpenEventW/test1/paltest_openeventw_test1")
{
    BOOL bRet = FAIL;
    DWORD dwRet;
    HANDLE hEvent;
    HANDLE hOpenEvent;
    WCHAR theName[] = {'E','v','e','n','t','\0'};
    LPCWSTR lpName = theName;

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }

    /* Create an event (with a 0 initial state!) and ensure that the
       HANDLE is valid
    */
    hEvent = CreateEventW( NULL, TRUE, FALSE, lpName );
    if( hEvent == NULL )
    {
        Fail( "ERROR:%lu:CreateEvent call failed\n", GetLastError() );
    }


    /* Call OpenEventW to get another HANDLE on
       this event.  Ensure the HANDLE is valid.
    */
    hOpenEvent = OpenEventW( EVENT_ALL_ACCESS, TRUE, lpName );
    if( hOpenEvent == NULL )
    {
        Trace( "ERROR:%lu:OpenEventW call failed\n", GetLastError() );
        goto cleanup2;
    }

    /* wait on the original event to verify that it's not signalled */
    dwRet = WaitForSingleObject( hEvent, 0 );
    if( dwRet != WAIT_TIMEOUT )
    {
        Trace( "ERROR:WaitForSingleObject returned %lu, "
                "expected WAIT_TIMEOUT\n",
                dwRet );
        goto cleanup;
    }

    /* wait on the opened event to verify that it's not signalled either */
    dwRet = WaitForSingleObject( hOpenEvent, 0 );
    if( dwRet != WAIT_TIMEOUT )
    {
        Trace( "ERROR:WaitForSingleObject returned %lu, "
                "expected WAIT_TIMEOUT\n",
                dwRet );
        goto cleanup;
    }


    /* Set this opened HANDLE */
    if( ! SetEvent( hOpenEvent ) )
    {
        Trace( "ERROR:%lu:SetEvent call failed\n", GetLastError() );
        goto cleanup;
    }

    /* wait on the original event to verify that it's signalled */
    dwRet = WaitForSingleObject( hEvent, 0 );
    if( dwRet != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject returned %lu, "
                "expected WAIT_OBJECT_0\n",
                dwRet );
        goto cleanup;
    }

    /* wait on the opened event to verify that it's signalled too */
    dwRet = WaitForSingleObject( hOpenEvent, 0 );
    if( dwRet != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject returned %lu, "
                "expected WAIT_OBJECT_0\n",
                dwRet );
        goto cleanup;
    }

    /* success if we get here */
    bRet = PASS;

cleanup:
    /* close the opened handle */
    if( ! CloseHandle( hOpenEvent ) )
    {
        Trace( "ERROR:%lu:CloseHandle call failed\n", GetLastError() );
        bRet = FAIL;
    }

cleanup2:
    /* close the original event handle */
    if( ! CloseHandle( hEvent ) )
    {
        Trace( "ERROR:%lu:CloseHandle call failed\n", GetLastError() );
        bRet = FAIL;
    }

    /* check for failure */
    if( bRet == FAIL )
    {
        Fail( "test failed\n" );
    }


    /* terminate the PAL */
    PAL_Terminate();

    /* return success */
    return ( PASS );

}

