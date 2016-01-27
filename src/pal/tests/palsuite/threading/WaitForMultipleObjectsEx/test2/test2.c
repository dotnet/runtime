// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

void RunTest(BOOL AlertThread);
VOID PALAPI APCFunc(ULONG_PTR dwParam);
DWORD PALAPI WaiterProc(LPVOID lpParameter);

DWORD ThreadWaitDelta;

int __cdecl main( int argc, char **argv ) 
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
    RunTest(TRUE);
    // Make sure that the wait returns in time greater than interrupt and less than 
    // wait timeout
    if ( 
        ((ThreadWaitDelta >= ChildThreadWaitTime) && (ThreadWaitDelta - ChildThreadWaitTime) > TOLERANCE) 
        || (( ThreadWaitDelta < InterruptTime) && (ThreadWaitDelta - InterruptTime) > TOLERANCE)
        )
    {
        Fail("Expected thread to wait for %d ms (and get interrupted).\n"
             "Interrupt Time: %d ms,  ThreadWaitDelta %u\n", 
             ChildThreadWaitTime, InterruptTime, ThreadWaitDelta);
    }

    /* 
     * Check that Queueing an APC in the middle of a wait does NOT interrupt 
     * it, if it is not in an alertable state.
     */
    RunTest(FALSE);

    // Make sure that time taken for thread to return from wait is more than interrupt
    // and also not less than the complete child thread wait time

    delta = ThreadWaitDelta - ChildThreadWaitTime;
    if( (ThreadWaitDelta < ChildThreadWaitTime) && ( delta > TOLERANCE) ) 
    {
        Fail("Expected thread to wait for %d ms (and not get interrupted).\n"
             "Interrupt Time: %d ms,  ThreadWaitDelta %u\n", 
             ChildThreadWaitTime, InterruptTime, ThreadWaitDelta);
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
}

/* Function doesn't do anything, just needed to interrupt the wait*/
VOID PALAPI APCFunc(ULONG_PTR dwParam)
{    
}

/* Entry Point for child thread. */
DWORD PALAPI WaiterProc(LPVOID lpParameter)
{
    HANDLE Semaphore;
    UINT64 OldTimeStamp;
    UINT64 NewTimeStamp;
    BOOL Alertable;
    DWORD ret;

    /* Create a semaphore that is not in the signalled state */
    Semaphore = CreateSemaphoreW(NULL, 0, 1, NULL);

    if (Semaphore == NULL)
    {
        Fail("Failed to create semaphore!  GetLastError returned %d.\n",
            GetLastError());
    }

    Alertable = (BOOL) lpParameter;

    LARGE_INTEGER performanceFrequency;
    if (!QueryPerformanceFrequency(&performanceFrequency))
    {
        Fail("Failed to query performance frequency!");
    }

    OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

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

    ThreadWaitDelta = NewTimeStamp - OldTimeStamp;

    ret = CloseHandle(Semaphore);
    if (!ret)
    {
        Fail("Unable to close handle to semaphore!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    return 0;
}


