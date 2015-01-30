//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test to ensure that WaitForMultipleObjects() performs
**          as expected when called with an INFINITE timeout.
**
**
**==========================================================================*/
#include <palsuite.h>

/* global data */
static BOOL   bEventSet = FALSE;
static HANDLE hEvent = NULL;


/* handler function */
static BOOL PALAPI CtrlHandler( DWORD CtrlType )
{
    if( CtrlType == CTRL_C_EVENT )
    {
        if( hEvent != NULL )
        {
            bEventSet = TRUE;
            if( ! SetEvent( hEvent ) )
            {
                Fail( "ERROR:%lu:SetEvent call failed", GetLastError() );
            }
        }
        return TRUE;
    }

    return FALSE;
}


/* main entry point function */
int __cdecl main( int argc, char **argv )

{
    /* local variables */
    BOOL    ret = FAIL;
    DWORD   dwRet;


    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* set the console control handler function */
    if( ! SetConsoleCtrlHandler( CtrlHandler, TRUE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                "CtrlHandler\n",
                GetLastError() );
        goto done;
    }


    /* create a manual-reset event on which to wait */
    hEvent = CreateEvent( NULL, TRUE, FALSE, NULL );
    if( hEvent == NULL )
    {
        Trace( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
        goto done;
    }

    /* helpful instructions */
    Trace( "This program is designed to test WaitForMultipleObjects using\n"
            "a timeout value of INFINITE, and consequently it's expected\n"
            "to run forever, waiting and waiting. Press CTRL-C when you feel\n"
            "you've sufficiently approximated infinity to end the test.\n" );

    /* wait on the event forever, until the user stops the program */
    dwRet = WaitForMultipleObjects( 1, &hEvent, TRUE, INFINITE );
    if( dwRet != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForMultipleObjects returned %lu, "
                "expected WAIT_OBJECT_0\n",
                dwRet );
        goto done;
    }

    /* verify that the bEventSet flag has been set */
    if( ! bEventSet )
    {
        Trace( "ERROR:WaitForMultipleObjects returned WAIT_OBJECT_0 "
                "but the event was never set\n" );
        goto done;
    }

    /* success if we get here */
    ret = PASS;


done:
    /* close our event handle */
    if( hEvent != NULL )
    {
        if( ! CloseHandle( hEvent ) )
        {
            Trace( "ERROR:%lu:CloseHandle call failed\n", GetLastError() );
            ret = FAIL;
        }
    }

    /* unset the control handle that was set */
    if( ! SetConsoleCtrlHandler( CtrlHandler, FALSE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                "remove CtrlHandler\n",
                GetLastError() );
    }

    /* check for failure */
    if( ret == FAIL )
    {
        Fail( "test failed\n" );
    }


    /* PAL termination */
    PAL_Terminate();


    /* return success */
    return PASS;
}


