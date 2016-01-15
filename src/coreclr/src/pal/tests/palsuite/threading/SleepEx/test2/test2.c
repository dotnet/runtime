//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests that a child thread in the middle of a SleepEx call will be
**          interrupted by QueueUserAPC if the alert flag was set.
**
**
**===================================================================*/

#include <palsuite.h>

const int ChildThreadSleepTime = 2000;
const int InterruptTime = 1000; 
/* We need to keep in mind that BSD has a timer resolution of 10ms, so
   we need to adjust our delta to keep that in mind. Besides we need some 
   tolerance to account for different scheduling strategies, heavy load 
   scenarios, etc.
   
   Real-world data also tells us we can expect a big difference between
   values when run on real iron vs run in a hypervisor.

   Thread-interruption times when run on bare metal will typically yield
   around 0ms on Linux and between 0 and 16ms on FreeBSD. However, when run
   in a hypervisor (like VMWare ESXi) we may get values around an order of
   magnitude higher, up to 110 ms for some tests.
*/
const DWORD AcceptableDelta = 150;

const int Iterations = 5;

void RunTest(BOOL AlertThread);
VOID PALAPI APCFunc(ULONG_PTR dwParam);
DWORD PALAPI SleeperProc(LPVOID lpParameter);

DWORD ThreadSleepDelta;

int __cdecl main( int argc, char **argv ) 
{
    int i;
    DWORD dwAvgDelta;

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
     * Check that Queueing an APC in the middle of a sleep does interrupt
     * it, if it's in an alertable state.
     */
    dwAvgDelta = 0;
    for (i=0;i<Iterations;i++)
    {
	RunTest(TRUE);
	dwAvgDelta += ThreadSleepDelta - InterruptTime;
    }
    dwAvgDelta /= Iterations;

    if (dwAvgDelta > AcceptableDelta)
    {
        Fail("Expected thread to sleep for %d ms (and get interrupted).\n"
             "Average delta: %u ms,  acceptable delta: %u\n", 
             InterruptTime, dwAvgDelta, AcceptableDelta);
    }

    /* 
     * Check that Queueing an APC in the middle of a sleep does NOT interrupt 
     * it, if it is not in an alertable state.
     */
    dwAvgDelta = 0;
    for (i=0;i<Iterations;i++)
    {
	RunTest(FALSE);
	dwAvgDelta += ThreadSleepDelta - ChildThreadSleepTime;
    }
    dwAvgDelta /= Iterations;

    if (dwAvgDelta > AcceptableDelta)
    {
        Fail("Expected thread to sleep for %d ms (and not be interrupted).\n"
             "Average delta: %u ms,  acceptable delta: %u\n", 
             ChildThreadSleepTime, dwAvgDelta, AcceptableDelta);
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
                            (LPTHREAD_START_ROUTINE)SleeperProc,
                            (LPVOID) AlertThread,
                            0,
                            &dwThreadId);

    if (hThread == NULL)
    {
        Fail("ERROR: Was not able to create the thread to test SleepEx!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    if (SleepEx(InterruptTime, FALSE) != 0)
    {
        Fail("The creating thread did not sleep!\n");
    }

    ret = QueueUserAPC(APCFunc, hThread, 0);
    if (ret == 0)
    {
        Fail("QueueUserAPC failed! GetLastError returned %d\n", GetLastError());
    }

    ret = WaitForSingleObject(hThread, INFINITE);
    if (ret == WAIT_FAILED)
    {
        Fail("Unable to wait on child thread!\nGetLastError returned %d.", 
            GetLastError());
    }
}

/* Function doesn't do anything, just needed to interrupt SleepEx */
VOID PALAPI APCFunc(ULONG_PTR dwParam)
{

}

/* Entry Point for child thread. */
DWORD PALAPI SleeperProc(LPVOID lpParameter)
{
    UINT64 OldTimeStamp;
    UINT64 NewTimeStamp;
    BOOL Alertable;
    DWORD ret;

    Alertable = (BOOL) lpParameter;

    LARGE_INTEGER performanceFrequency;
    if (!QueryPerformanceFrequency(&performanceFrequency))
    {
        return FAIL;
    }

    OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

    ret = SleepEx(ChildThreadSleepTime, Alertable);
    
    NewTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

    if (Alertable && ret != WAIT_IO_COMPLETION)
    {
        Fail("Expected the interrupted sleep to return WAIT_IO_COMPLETION.\n"
            "Got %d\n", ret);
    }
    else if (!Alertable && ret != 0)
    {
        Fail("Sleep did not timeout.  Expected return of 0, got %d.\n", ret);
    }


    ThreadSleepDelta = NewTimeStamp - OldTimeStamp;

    return 0;
}
