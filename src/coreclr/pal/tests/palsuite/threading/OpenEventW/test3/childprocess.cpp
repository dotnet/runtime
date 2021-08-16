// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: childprocess.c
**
** Purpose: Test to ensure that OpenEventW() works when
** opening an event created by another process. The test
** program launches this program as a child, which creates
** a named, initially-unset event. The child waits up to
** 10 seconds for the parent process to open that event
** and set it, and returns PASS if the event was set or FAIL
** otherwise. The parent process checks the return value
** from the child to verify that the opened event was
** properly used across processes.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               CreateEventW
**               WaitForSingleObject
**               CloseHandle
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(threading_OpenEventW_test3_paltest_openeventw_test3_child, "threading/OpenEventW/test3/paltest_openeventw_test3_child")
{
    /* local variables */
    HANDLE                  hEvent = NULL;
    WCHAR                   wcName[] = {'P','A','L','R','o','c','k','s','\0'};
    LPWSTR                  lpName = wcName;
    
    int result = PASS;

    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }


    /* open a handle to the event created in the child process */
    hEvent = OpenEventW( EVENT_ALL_ACCESS,  /* we want all rights */
                         FALSE,             /* no inherit         */
                         lpName );

    if( hEvent == NULL )
    {
        /* ERROR */
        Trace( "ERROR:%lu:OpenEventW() call failed\n", GetLastError() );
        result = FAIL;
        goto parentwait;
    }

    /* set the event -- should take effect in the child process */
    if( ! SetEvent( hEvent ) )
    {
        /* ERROR */
        Trace( "ERROR:%lu:SetEvent() call failed\n", GetLastError() );
        result = FAIL;
    }

parentwait:
    /* close the event handle */
    if( ! CloseHandle( hEvent ) )
    {
        /* ERROR */
        Fail(   "ERROR:%lu:CloseHandle() call failed in child\n",
                GetLastError());
    }

    /* terminate the PAL */
    PAL_TerminateEx(result);

    /* return success or failure */
    return result;
}
