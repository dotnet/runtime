// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

void RunTest_WFSOExSemaphoreTest(BOOL AlertThread);
VOID PALAPI APCFunc_WFSOExSemaphoreTest(ULONG_PTR dwParam);
DWORD PALAPI WaiterProc_WFSOExSemaphoreTest(LPVOID lpParameter);

DWORD ThreadWaitDelta_WFSOExSemaphoreTest;
static volatile bool s_preWaitTimestampRecorded = false;

PALTEST(threading_WaitForSingleObject_WFSOExSemaphoreTest_paltest_waitforsingleobject_wfsoexsemaphoretest, "threading/WaitForSingleObject/WFSOExSemaphoreTest/paltest_waitforsingleobject_wfsoexsemaphoretest")
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

    RunTest_WFSOExSemaphoreTest(TRUE);
    if ((ThreadWaitDelta_WFSOExSemaphoreTest - InterruptTime) > AcceptableDelta)
    {
        Fail("Expected thread to wait for %d ms (and get interrupted).\n"
            "Thread waited for %d ms! (Acceptable delta: %d)\n",
            InterruptTime, ThreadWaitDelta_WFSOExSemaphoreTest, AcceptableDelta);
    }


     /*
     * Check that Queueing an APC in the middle of a wait does NOT interrupt
     * it, if it is not in an alertable state.
     */
    RunTest_WFSOExSemaphoreTest(FALSE);
    if ((ThreadWaitDelta_WFSOExSemaphoreTest - ChildThreadWaitTime) > AcceptableDelta)
    {
        Fail("Expected thread to wait for %d ms (and not be interrupted).\n"
            "Thread waited for %d ms! (Acceptable delta: %d)\n",
            ChildThreadWaitTime, ThreadWaitDelta_WFSOExSemaphoreTest, AcceptableDelta);
    }


    PAL_Terminate();
    return PASS;
}

void RunTest_WFSOExSemaphoreTest(BOOL AlertThread)
{
    HANDLE hThread = 0;
    DWORD dwThreadId = 0;
    int ret;

    s_preWaitTimestampRecorded = false;
    hThread = CreateThread( NULL,
                            0,
                            (LPTHREAD_START_ROUTINE)WaiterProc_WFSOExSemaphoreTest,
                            (LPVOID) AlertThread,
                            0,
                            &dwThreadId);

    if (hThread == NULL)
    {
        Fail("ERROR: Was not able to create the thread to test!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    // Wait for the pre-wait timestamp to be recorded on the other thread before sleeping, since the sleep duration here will be
    // compared against the sleep/wait duration on the other thread
    while (!s_preWaitTimestampRecorded)
    {
        Sleep(0);
    }

    Sleep(InterruptTime);

    ret = QueueUserAPC(APCFunc_WFSOExSemaphoreTest, hThread, 0);
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
VOID PALAPI APCFunc_WFSOExSemaphoreTest(ULONG_PTR dwParam)
{
}

/* Entry Point for child thread. */
DWORD PALAPI WaiterProc_WFSOExSemaphoreTest(LPVOID lpParameter)
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
    hSemaphore = CreateSemaphoreExW(NULL, 0, 1, NULL, 0, 0);

    if (hSemaphore == NULL)
    {
        Fail("Failed to create semaphore!  GetLastError returned %d.\n",
            GetLastError());
    }

    Alertable = (BOOL)(SIZE_T) lpParameter;

    OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);
    s_preWaitTimestampRecorded = true;

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


    ThreadWaitDelta_WFSOExSemaphoreTest = NewTimeStamp - OldTimeStamp;

    ret = CloseHandle(hSemaphore);
    if (!ret)
    {
        Fail("Unable to close handle to semaphore!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    return 0;
}



