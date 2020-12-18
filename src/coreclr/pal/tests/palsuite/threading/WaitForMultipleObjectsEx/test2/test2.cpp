// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests that a child thread in the middle of a
**          WaitForMultipleObjectsEx call will be interrupted by QueueUserAPC
**          if the alert flag was set.
**
**
**===================================================================*/

#include <palsuite.h>

/* Based on SleepEx/test2 */

const unsigned int ChildThreadWaitTime = 1000;
const unsigned int InterruptTime = 500;

#define TOLERANCE 10

void RunTest_WFMO_test2(BOOL AlertThread);
VOID PALAPI APCFunc_WFMO_test2(ULONG_PTR dwParam);
DWORD PALAPI WaiterProc_WFMO_test2(LPVOID lpParameter);

DWORD ThreadWaitDelta_WFMO_test2;
static volatile bool s_preWaitTimestampRecorded = false;

PALTEST(threading_WaitForMultipleObjectsEx_test2_paltest_waitformultipleobjectsex_test2, "threading/WaitForMultipleObjectsEx/test2/paltest_waitformultipleobjectsex_test2")
{

    DWORD delta = 0;

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
    RunTest_WFMO_test2(TRUE);
    // Make sure that the wait returns in time greater than interrupt and less than
    // wait timeout
    if ( 
        ((ThreadWaitDelta_WFMO_test2 >= ChildThreadWaitTime) && (ThreadWaitDelta_WFMO_test2 - ChildThreadWaitTime) > TOLERANCE)
        || (( ThreadWaitDelta_WFMO_test2 < InterruptTime) && (InterruptTime - ThreadWaitDelta_WFMO_test2) > TOLERANCE)
        )
    {
        Fail("Expected thread to wait for %d ms (and get interrupted).\n"
             "Interrupt Time: %d ms,  ThreadWaitDelta %u\n",
             ChildThreadWaitTime, InterruptTime, ThreadWaitDelta_WFMO_test2);
    }

    /*
     * Check that Queueing an APC in the middle of a wait does NOT interrupt
     * it, if it is not in an alertable state.
     */
    RunTest_WFMO_test2(FALSE);

    // Make sure that time taken for thread to return from wait is more than interrupt
    // and also not less than the complete child thread wait time

    delta = ThreadWaitDelta_WFMO_test2 - ChildThreadWaitTime;
    if( (ThreadWaitDelta_WFMO_test2 < ChildThreadWaitTime) && ( delta > TOLERANCE) )
    {
        Fail("Expected thread to wait for %d ms (and not get interrupted).\n"
             "Interrupt Time: %d ms,  ThreadWaitDelta %u\n",
             ChildThreadWaitTime, InterruptTime, ThreadWaitDelta_WFMO_test2);
    }


    PAL_Terminate();
    return PASS;
}

void RunTest_WFMO_test2(BOOL AlertThread)
{
    HANDLE hThread = 0;
    DWORD dwThreadId = 0;
    int ret;

    s_preWaitTimestampRecorded = false;
    hThread = CreateThread( NULL,
                            0,
                            (LPTHREAD_START_ROUTINE)WaiterProc_WFMO_test2,
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

    ret = QueueUserAPC(APCFunc_WFMO_test2, hThread, 0);
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
}

/* Function doesn't do anything, just needed to interrupt the wait*/
VOID PALAPI APCFunc_WFMO_test2(ULONG_PTR dwParam)
{
}

/* Entry Point for child thread. */
DWORD PALAPI WaiterProc_WFMO_test2(LPVOID lpParameter)
{
    HANDLE Semaphore;
    UINT64 OldTimeStamp;
    UINT64 NewTimeStamp;
    BOOL Alertable;
    DWORD ret;

    /* Create a semaphore that is not in the signalled state */
    Semaphore = CreateSemaphoreExW(NULL, 0, 1, NULL, 0, 0);

    if (Semaphore == NULL)
    {
        Fail("Failed to create semaphore!  GetLastError returned %d.\n",
            GetLastError());
    }

    Alertable = (BOOL)(SIZE_T) lpParameter;

    LARGE_INTEGER performanceFrequency;
    if (!QueryPerformanceFrequency(&performanceFrequency))
    {
        Fail("Failed to query performance frequency!");
    }

    OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);
    s_preWaitTimestampRecorded = true;

    ret = WaitForMultipleObjectsEx(1, &Semaphore, FALSE, ChildThreadWaitTime,
        Alertable);

    NewTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);


    if (Alertable && ret != WAIT_IO_COMPLETION)
    {
        Fail("Expected the interrupted wait to return WAIT_IO_COMPLETION.\n"
            "Got %d\n", ret);
    }
    else if (!Alertable && ret != WAIT_TIMEOUT)
    {
        Fail("WaitForMultipleObjectsEx did not timeout.\n"
            "Expected return of WAIT_TIMEOUT, got %d.\n", ret);
    }

    ThreadWaitDelta_WFMO_test2 = NewTimeStamp - OldTimeStamp;

    ret = CloseHandle(Semaphore);
    if (!ret)
    {
        Fail("Unable to close handle to semaphore!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    return 0;
}


