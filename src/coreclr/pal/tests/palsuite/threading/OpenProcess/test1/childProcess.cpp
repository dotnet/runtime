// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: childprocess.c
**
** Purpose: Test to ensure OpenProcess works properly.
** All this program does is return a predefined value.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               CreateMutexW
**               WaitForSingleObject
**               CloseHandle
**
**
**=========================================================*/

#include <palsuite.h>
#include "myexitcode.h"


PALTEST(threading_OpenProcess_test1_paltest_openprocess_test1_child, "threading/OpenProcess/test1/paltest_openprocess_test1_child")
{
    HANDLE hMutex;
    WCHAR wszMutexName[] = { 'T','E','S','T','1','\0' };
    DWORD dwRet;
    int i;

    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* open a mutex to synchronize with the parent process */
    hMutex = CreateMutexW( NULL, FALSE, wszMutexName );
    if( hMutex == NULL )
    {
        Fail( "ERROR:%lu:CreateMutex() call failed\r\n", GetLastError() );
    }

    /* acquire the mutex lock */
    dwRet = WaitForSingleObject( hMutex, 10000 );
    if( dwRet != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected WAIT_OBJECT_0",
                dwRet );
        if( ! CloseHandle( hMutex ) )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "test failed\n" );
    }


    /* simulate some activity  */
    for( i=0; i<50000; i++ )
        ;

    /* close our mutex handle */
    if( ! CloseHandle( hMutex ) )
    {
        Fail( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
    }

    /* terminate the PAL */
    PAL_Terminate();

    /* return the predefined exit code */
    return TEST_EXIT_CODE;
}
