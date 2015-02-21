//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test4.c 
**
** Purpose: Test for SuspendThread. Suspending threads suspend
** targets from a pool using random selection or iterative
** selection. This also tests SuspendThread and ResumeThread
** being invoked in a shutdown scenario. Finally, suspension safe 
** memory allocation functions are tested. The expected failure 
** case for this test is a hang.
**
**
**=========================================================*/

#include <palsuite.h>

// Declare sched_yield(), as we cannot include <sched.h> here.
int sched_yield(void);

#define NUM_MALLOCS 256
#define MAX_THREADS 64
int numThreads, numIterations, targetThreadsSelectionAlgo, targetThreadsPos;
HANDLE targetThreads[MAX_THREADS];

void MemoryRoutine()
{
    int *ptr[NUM_MALLOCS];
    unsigned long int i, j;

    for(i = 0; i < NUM_MALLOCS; i++)
    {
        ptr[i] = (int *)malloc(10);
    }

    for(j = 0; j < NUM_MALLOCS; j++)
    {
        if(ptr[j] != NULL)
            free((void*)ptr[j]);
    }
 
}

HANDLE CreateSuspendedThread(LPTHREAD_START_ROUTINE lpStartAddress)
{
    DWORD dwThreadId = 0;
    HANDLE hThread = CreateThread(NULL, 0, lpStartAddress,
                            (LPVOID)0, CREATE_SUSPENDED, &dwThreadId);
    if (hThread == NULL)
    {
        Fail("Failed to create a suspended thread.\n");
    }
    return hThread;
}

HANDLE GetRandomTargetThread()
{
    return targetThreads[(int)(rand() % (numThreads / 2))];
}

HANDLE GetNextTargetThread()
{
    // next thread in queue.
    HANDLE h = targetThreads[targetThreadsPos];
    if(targetThreadsPos < ((numThreads / 2) - 1))
    {
        targetThreadsPos++;
    }
    else if(targetThreadsPos == ((numThreads / 2) - 1))
    {
        targetThreadsPos = 0;
    }
    else
    {
        Fail("The target thread index cannot be %d in GetNextTargetThread.\n", targetThreadsPos);
    }
    return h;

}

DWORD PALAPI SuspenderThreadRoutine()
{
    HANDLE targetThread;
    LONG dwRet;
    long int i;

    for(i = 0; i < numIterations; i++)
    {
        if(targetThreadsSelectionAlgo)
        {
            targetThread = GetRandomTargetThread();
        }
        else
        {
            targetThread = GetNextTargetThread();
        }

        dwRet = SuspendThread(targetThread);
        if (dwRet < 0)
        {
            Fail("Failed to suspend a target thread - SuspendThread returned %d.\n", dwRet);
        }     
        MemoryRoutine();
        dwRet = ResumeThread(targetThread);
        if (dwRet < 0)
        {
            Fail("Failed to resume a target thread - ResumeThread returned %d.\n", dwRet);
        }                
    }

    printf("Suspender Routine is done.\n");

    return 0;

}

DWORD PALAPI TargetThreadRoutine()
{

    while(TRUE)
    {
        MemoryRoutine();
        sched_yield();
    }

    printf("Target Routine is done.\n");
    return 0;

}

int __cdecl main(int argc, char *argv[])
{
    int i, j, k;
    DWORD dwRet;
    HANDLE suspenderRoutineHandles[1024];
    targetThreadsPos = 0;

    if(0 != PAL_Initialize(argc, argv))
    {
        return(FAIL);
    }

    if(argc == 4)
    {
        numThreads = atoi(argv[1]); // must be divisible by 2 - check for this.
        numIterations = atoi(argv[2]); // must be greater than 0.
        targetThreadsSelectionAlgo = atoi(argv[3]); // must be 0 or 1.
    }
    else
    {
        numThreads = 2;
        numIterations = 1000;
        targetThreadsSelectionAlgo = 0;
    }

    if(numThreads < 2 || numThreads > MAX_THREADS)
    {
        Fail("numThreads must be greater than or equal to 2 and less than %d.\n", MAX_THREADS);
    }

    if(numThreads % 2 != 0)
    {
        Fail("numThreads must be divisible by 2.\n");
    }

    if(numIterations < 1)
    {
        Fail("numIterations must be greater than 0.\n");
    }

    if(targetThreadsSelectionAlgo != 0 && targetThreadsSelectionAlgo != 1)
    {
        Fail("The target threads selection algorithm should be 0 for iterative thread selection or 1 for random selection.\n");
    }

    srand((unsigned)time(NULL)); // seed random number generator

    for(i = 0; i < (numThreads / 2); i++)
    {
        targetThreads[i] = CreateSuspendedThread(&TargetThreadRoutine);
        targetThreads[(i + (numThreads / 2))] = CreateSuspendedThread(&SuspenderThreadRoutine);
    }

    for(k = 0; k < (numThreads / 2); k++)
    {
        suspenderRoutineHandles[k] = targetThreads[(k + (numThreads / 2))];
    }

    for(j = 0; j < numThreads; j++)
    {
        if (-1 == ResumeThread(targetThreads[j])) // start all the target threads first.
        {
            Fail("Failed to resume a target thread.\n");
        } 
    }

    dwRet = WaitForMultipleObjects(numThreads / 2, suspenderRoutineHandles, TRUE, 60000); 
    if (WAIT_FAILED == dwRet)
    {
        Fail("WaitForMultipleObjects failed\n");
    }
    
    PAL_Terminate();
    return(PASS);

}

