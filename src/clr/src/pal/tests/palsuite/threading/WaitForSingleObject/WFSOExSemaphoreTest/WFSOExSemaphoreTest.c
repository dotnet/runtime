//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  	WFSOExSemaphore.c
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

int __cdecl main( int argc, char **argv ) 
{
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


    PAL_Terminate();
    return PASS;
}

void RunTest(BOOL AlertThread)
{
    HANDLE hThread = 0;
    DWORD dwThreadId = 0;
    int ret;

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
    HANDLE hSemaphore;
    UINT64 OldTimeStamp;
    UINT64 NewTimeStamp;
    BOOL Alertable;
    DWORD ret;

    LARGE_INTEGER performanceFrequency;
    if (!QueryPerformanceFrequency(&performanceFrequency))
    {
        Fail("Failed to query performance frequency!");
    }

    /* Create a semaphore that is not in the signalled state */
    hSemaphore = CreateSemaphoreW(NULL, 0, 1, NULL);

    if (hSemaphore == NULL)
    {
        Fail("Failed to create semaphore!  GetLastError returned %d.\n",
            GetLastError());
    }

    Alertable = (BOOL) lpParameter;

    OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

    ret = WaitForSingleObjectEx(	hSemaphore, 
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

    ret = CloseHandle(hSemaphore);
    if (!ret)
    {
        Fail("Unable to close handle to semaphore!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    return 0;
}



