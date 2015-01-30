//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main( int argc, char **argv ) 
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
