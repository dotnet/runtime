// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(threading_GetCurrentThread_test1_paltest_getcurrentthread_test1, "threading/GetCurrentThread/test1/paltest_getcurrentthread_test1")
{

    HANDLE hThread;
    int nPriority;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

#if !HAVE_SCHED_OTHER_ASSIGNABLE
    /* Defining thread priority for SCHED_OTHER is implementation defined.
       Some platforms like NetBSD cannot reassign it as they are dynamic.
    */
    printf("paltest_getcurrentthread_test1 has been disabled on this platform\n");
#else
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
#endif

    PAL_Terminate();
    return ( PASS );

}
