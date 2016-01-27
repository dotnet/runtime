// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: GetCurrentThread/test1/thread.c
**
** Purpose: Test to ensure GetCurrentThread returns a handle to 
** the current thread.
**
** Dependencies: GetThreadPriority
**               SetThreadPriority
**               Fail   
**               Trace
** 

**
**=========================================================*/

#include <palsuite.h>

int __cdecl main( int argc, char **argv ) 
{

    HANDLE hThread; 
    int nPriority;
 
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
   
    hThread = GetCurrentThread();
    
    nPriority = GetThreadPriority(hThread);

    if ( THREAD_PRIORITY_NORMAL != nPriority )
    {
	if ( THREAD_PRIORITY_ERROR_RETURN == nPriority ) 
	{
	    Fail ("GetThreadPriority function call failed for %s\n"
		    "GetLastError returned %d\n", argv[0], GetLastError());
	}
	else 
	{
	    Fail ("GetThreadPriority function call failed for %s\n"
		    "The priority returned was %d\n", argv[0], nPriority);
	}
    }
    else
    {
	nPriority = 0;
	
	if (0 == SetThreadPriority(hThread, THREAD_PRIORITY_HIGHEST)) 
	{
	    Fail ("Unable to set thread priority.  Either handle doesn't"
		    " point to current thread \nor SetThreadPriority "
		    "function failed.  Failing test.\n");
	}
	
	nPriority = GetThreadPriority(hThread);
	
	if ( THREAD_PRIORITY_ERROR_RETURN == nPriority ) 
	{
	    Fail ("GetThreadPriority function call failed for %s\n"
		    "GetLastError returned %d\n", argv[0], GetLastError());
	}
	else if ( THREAD_PRIORITY_HIGHEST == nPriority ) 
	{
	    Trace ("GetCurrentThread returns handle to the current "
		    "thread.\n");
	    exit ( PASS );
	} 
	else 
	{
	    Fail ("Unable to set thread priority.  Either handle doesn't"
		    " point to current thread \nor SetThreadPriority "
		    "function failed.  Failing test.\n");
	}
    }

    PAL_Terminate();
    return ( PASS );    
    
}
