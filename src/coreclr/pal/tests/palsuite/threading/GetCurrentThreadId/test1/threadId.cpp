// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: getcurrentthreadid/test1/threadid.c
**
** Purpose: Test to ensure GetCurrentThreadId returns the threadId of the
** current thread. 
** 
** Dependencies: CloseHandle
**               WaitForSingleObject
**               CreateThread
** 

**
**=========================================================*/


#include <palsuite.h>

DWORD dwThreadIdTF;

DWORD PALAPI ThreadFunction ( LPVOID lpParam )
{
    Trace ("thread code executed\n");
    dwThreadIdTF = GetCurrentThreadId();
    return 0;
}

PALTEST(threading_GetCurrentThreadId_test1_paltest_getcurrentthreadid_test1, "threading/GetCurrentThreadId/test1/paltest_getcurrentthreadid_test1")
{
    extern DWORD dwThreadIdTF;
    DWORD dwThreadIdCT;
    HANDLE hThread; 
    DWORD dwThreadParam = 1;
    DWORD dwThreadWait;
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
    
    hThread = CreateThread(
        NULL,            
        0,               
        ThreadFunction,  
        &dwThreadParam,  
        0,               
        &dwThreadIdCT);  
    
    if ( NULL == hThread ) 
    {
        Fail ( "CreateThread() call failed - returned NULL");
    }
    else 
    {
	dwThreadWait = WaitForSingleObject( hThread, INFINITE );   
    
        Trace ("dwThreadWait returned %d\n", dwThreadWait );
    
	if ( dwThreadIdCT == dwThreadIdTF )
	{
            Trace ( "ThreadId numbers match - GetCurrentThreadId"
		     " works.  dwThreadIdCT == dwThreadIdTF == %d\n",
		     dwThreadIdTF );
	    PAL_Terminate();
            return ( PASS );
	}
	else 
	{
            Fail ( "ThreadId numbers don't match - "
		     "GetCurrentThreadId fails dwThreadIdCT = %d "
		     "and dwThreadIdTF = %d\n", dwThreadIdCT, dwThreadIdTF);
	}
    }

    PAL_TerminateEx(FAIL);
    return (FAIL);

}
