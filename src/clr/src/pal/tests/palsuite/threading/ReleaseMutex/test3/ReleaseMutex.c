//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

DWORD dwTestResult;  /* global for test result */

DWORD dwThreadId;  /* consumer thread identifier */

HANDLE hMutex;  /* handle to mutex */

HANDLE hThread;  /* handle to thread */

/* 
 * Thread function. 
 */
DWORD
PALAPI 
ThreadFunction( LPVOID lpNoArg )
{

    dwTestResult = ReleaseMutex(hMutex);

    return 0;
}

int __cdecl main (int argc, char **argv) 
{

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (FAIL);
    }

    /*
     * set dwTestResult so test fails even if ReleaseMutex is not called
     */
    dwTestResult = 1;

    /*
     * Create mutex
     */
    hMutex = CreateMutexW (
	NULL,
	TRUE,
	NULL);

    if ( NULL == hMutex ) 
    {
        Fail ( "hMutex = CreateMutex () - returned NULL\n"
		 "Failing Test.\nGetLastError returned %d\n", GetLastError());
    }

    /* 
     * Create ThreadFunction
     */
    hThread = CreateThread(
	NULL, 
	0,    
	ThreadFunction,
	NULL,          
	0,             
	&dwThreadId);  

    if ( NULL == hThread ) 
    {

	Fail ( "CreateThread() returned NULL.  Failing test.\n"
		 "GetLastError returned %d\n", GetLastError());
    }
    
    /*
     * Wait for ThreadFunction to complete
     */
    WaitForSingleObject (hThread, INFINITE);
    
    if (dwTestResult)
    {
	Fail ("ReleaseMutex() test was expected to return 0.\n" 
		"It returned %d.  Failing test.\n", dwTestResult );
    }

    Trace ("ReleaseMutex() test returned 0.\nTest passed.\n");

    PAL_Terminate();
    return ( PASS );

}
