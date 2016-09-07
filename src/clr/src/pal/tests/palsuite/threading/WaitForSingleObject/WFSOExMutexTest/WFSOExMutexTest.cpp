// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  	WFSOExMutex.c
**
** Purpose: 	Tests a child thread in the middle of a 
**          		WaitForSingleObjectEx call will be interrupted by QueueUserAPC
**         		if the alert flag was set.
**
**
**===================================================================*/

#include <palsuite.h>

/*Based on SleepEx/test2 */

const int ChildThreadWaitTime = 4000;
const int InterruptTime = 2000;
const DWORD AcceptableDelta = 300;

void RunTest(BOOL AlertThread);
VOID PALAPI APCFunc(ULONG_PTR dwParam);
DWORD PALAPI WaiterProc(LPVOID lpParameter);

DWORD ThreadWaitDelta;
HANDLE hMutex;



int __cdecl main( int argc, char **argv ) 
{
    int ret=0;
	
	if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

	/*
      On some platforms (e.g. FreeBSD 4.9) the first call to some synch objects 
      (such as conditions) involves some pthread internal initialization that 
      can make the first wait slighty longer, potentially going above the
      acceptable delta for this test. Let's add a dummy wait to preinitialize
      internal structures
    */
    Sleep(100);
	
	/*
	The state of a mutex object is signaled when it is not owned by any thread. 
	The creating thread can use the bInitialOwner flag to request immediate ownership 
	of the mutex. Otherwise, a thread must use one of the wait functions to request 
	ownership. When the mutex's state is signaled, one waiting thread is granted 
	ownership, the mutex's state changes to nonsignaled, and the wait function returns. 
	Only one thread can own a mutex at any given time. The owning thread uses the 
	ReleaseMutex function to release its ownership.
	*/
	
	/* Create a mutex that is not in the signalled state */
    hMutex = CreateMutex(NULL,      //No security attributes
                         TRUE,      //Iniitally owned
                         NULL);     //Name of mutex

    if (hMutex == NULL)
    {
        Fail("Failed to create mutex!  GetLastError returned %d.\n",
            GetLastError());
    }
	/* 
     * Check that Queueing an APC in the middle of a wait does interrupt
     * it, if it's in an alertable state.
     */

    RunTest(TRUE);
    if ((ThreadWaitDelta - InterruptTime) > AcceptableDelta)
    {
        Fail("Expected thread to wait for %d ms (and get interrupted).\n"
            "Thread waited for %d ms! (Acceptable delta: %d)\n", 
            InterruptTime, ThreadWaitDelta, AcceptableDelta);
    }


     /* 
     * Check that Queueing an APC in the middle of a wait does NOT interrupt 
     * it, if it is not in an alertable state.
     */
    RunTest(FALSE);
    if ((ThreadWaitDelta - ChildThreadWaitTime) > AcceptableDelta)
    {
        Fail("Expected thread to wait for %d ms (and not be interrupted).\n"
            "Thread waited for %d ms! (Acceptable delta: %d)\n", 
            ChildThreadWaitTime, ThreadWaitDelta, AcceptableDelta);
    }


   
	//Release Mutex
	ret = ReleaseMutex(hMutex);
	if (0==ret)
    {
        Fail("Unable to Release Mutex!\n"
            "GetLastError returned %d\n", GetLastError());
    }

	//Close Mutex Handle
	ret = CloseHandle(hMutex);
    if (!ret)
    {
        Fail("Unable to close handle to Mutex!\n"
            "GetLastError returned %d\n", GetLastError());
    }
	
	PAL_Terminate();
    return PASS;
}

void RunTest(BOOL AlertThread)
{
    
	HANDLE hThread = 0;
    DWORD dwThreadId = 0;

	int ret=0;

    hThread = CreateThread( NULL, 
                            0, 
                            (LPTHREAD_START_ROUTINE)WaiterProc,
                            (LPVOID) AlertThread,
                            0,
                            &dwThreadId);

    if (hThread == NULL)
    {
        Fail("ERROR: Was not able to create the thread to test!\n"
            "GetLastError returned %d\n", GetLastError());
    }


	
	Sleep(InterruptTime);

    ret = QueueUserAPC(APCFunc, hThread, 0);
    
	if (ret == 0)
    {
        Fail("QueueUserAPC failed! GetLastError returned %d\n", 
            GetLastError());
    }
   
    ret = WaitForSingleObject(hThread, INFINITE);
    
	if (ret == WAIT_FAILED)
    {
        Fail("Unable to wait on child thread!\nGetLastError returned %d.\n", 
            GetLastError());
    }

	
	if (0==CloseHandle(hThread))
	    	{
	    	Trace("Could not close Thread handle\n"); 
		Fail ( "GetLastError returned %d\n", GetLastError());  
    	} 	
}

/* Function doesn't do anything, just needed to interrupt the wait*/
VOID PALAPI APCFunc(ULONG_PTR dwParam)
{    
}

/* Entry Point for child thread. */
DWORD PALAPI WaiterProc(LPVOID lpParameter)
{
    UINT64 OldTimeStamp;
    UINT64 NewTimeStamp;
    BOOL Alertable;
    DWORD ret;

    Alertable = (BOOL) lpParameter;

    LARGE_INTEGER performanceFrequency;
    if (!QueryPerformanceFrequency(&performanceFrequency))
    {
        Fail("Failed to query performance frequency!");
    }

    OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

    ret = WaitForSingleObjectEx(	hMutex, 
								ChildThreadWaitTime, 
        							Alertable);
    
    NewTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

    if (Alertable && ret != WAIT_IO_COMPLETION)
    {
        Fail("Expected the interrupted wait to return WAIT_IO_COMPLETION.\n"
            "Got %d\n", ret);
    }
    else if (!Alertable && ret != WAIT_TIMEOUT)
    {
        Fail("WaitForSingleObjectEx did not timeout.\n"
            "Expected return of WAIT_TIMEOUT, got %d.\n", ret);
    }

    ThreadWaitDelta = NewTimeStamp - OldTimeStamp;
  
    return 0;
}



