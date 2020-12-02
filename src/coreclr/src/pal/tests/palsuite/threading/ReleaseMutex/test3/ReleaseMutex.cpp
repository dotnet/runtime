// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: ReleaseMutex/test3/ReleaseMutex.c
**
** Purpose: Test failure code for ReleaseMutex. 
**
** Dependencies: CreateMutex
**               ReleaseMutex
**               CreateThread
** 

**
**=========================================================*/

#include <palsuite.h>

DWORD dwTestResult_ReleaseMutex_test3;  /* global for test result */

DWORD dwThreadId_ReleaseMutex_test3;  /* consumer thread identifier */

HANDLE hMutex_ReleaseMutex_test3;  /* handle to mutex */

HANDLE hThread_ReleaseMutex_test3;  /* handle to thread */

/* 
 * Thread function. 
 */
DWORD
PALAPI 
ThreadFunction_ReleaseMutex_test3( LPVOID lpNoArg )
{

    dwTestResult_ReleaseMutex_test3 = ReleaseMutex(hMutex_ReleaseMutex_test3);

    return 0;
}

PALTEST(threading_ReleaseMutex_test3_paltest_releasemutex_test3, "threading/ReleaseMutex/test3/paltest_releasemutex_test3")
{

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (FAIL);
    }

    /*
     * set dwTestResult so test fails even if ReleaseMutex is not called
     */
    dwTestResult_ReleaseMutex_test3 = 1;

    /*
     * Create mutex
     */
    hMutex_ReleaseMutex_test3 = CreateMutexW (
	NULL,
	TRUE,
	NULL);

    if ( NULL == hMutex_ReleaseMutex_test3 ) 
    {
        Fail ( "hMutex = CreateMutex () - returned NULL\n"
		 "Failing Test.\nGetLastError returned %d\n", GetLastError());
    }

    /* 
     * Create ThreadFunction
     */
    hThread_ReleaseMutex_test3 = CreateThread(
	NULL, 
	0,    
	ThreadFunction_ReleaseMutex_test3,
	NULL,          
	0,             
	&dwThreadId_ReleaseMutex_test3);  

    if ( NULL == hThread_ReleaseMutex_test3 ) 
    {

	Fail ( "CreateThread() returned NULL.  Failing test.\n"
		 "GetLastError returned %d\n", GetLastError());
    }
    
    /*
     * Wait for ThreadFunction to complete
     */
    WaitForSingleObject (hThread_ReleaseMutex_test3, INFINITE);
    
    if (dwTestResult_ReleaseMutex_test3)
    {
	Fail ("ReleaseMutex() test was expected to return 0.\n" 
		"It returned %d.  Failing test.\n", dwTestResult_ReleaseMutex_test3 );
    }

    Trace ("ReleaseMutex() test returned 0.\nTest passed.\n");

    PAL_Terminate();
    return ( PASS );

}
