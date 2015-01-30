//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test4.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               GetCurrentThread
**               SleepEx
**
** Purpose:
**
** Test to ensure proper operation of the QueueUserAPC()
** API by trying to queue APC functions on the current
** thread.
**
**
**===========================================================================*/
#include <palsuite.h>


static BOOL bAPCExecuted = FALSE;

VOID PALAPI APCFunc( ULONG_PTR dwParam )
{
    bAPCExecuted = TRUE;
}

int __cdecl main( int argc, char **argv )

{
    /* local variables */
    HANDLE hThread = NULL;
    DWORD  ret;

    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }

    /* get the current thread */
    hThread = GetCurrentThread();
    ret = QueueUserAPC( APCFunc, hThread, 0 );
    if( ret == 0 )
    {
        Fail( "ERROR:%lu:QueueUserAPC call failed\n", GetLastError() );
    }

    /* call SleepEx() to put the thread in an alertable state */
    ret = SleepEx( 2000, TRUE );
    if( ret != WAIT_IO_COMPLETION )
    {
        Fail( "ERROR:Expected sleep to return WAIT_IO_COMPLETION, got %lu\n",
            ret );
    }

    /* check that the APC function was executed */
    if( bAPCExecuted == FALSE )
    {
        Fail( "ERROR:APC function was not executed\n" );
    }

    /* PAL termination */
    PAL_Terminate();

    /* return success */
    return PASS;
}
