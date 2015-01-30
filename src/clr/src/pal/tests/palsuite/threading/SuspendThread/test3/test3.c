//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test3.c 
**
** Purpose: Test for SuspendThread. Tests a set of threads
** suspending each other in a cycle, along with testing safe 
** memory allocation. The expected failure case for this test
** is a hang due to mutex acquisition deadlock or suspending
** a thread holding an internal lock.
**
**
**=========================================================*/

#include <palsuite.h>

#define NUM_MALLOCS 256
#define MAX_THREADS 64
int numThreads, numIterations;
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

HANDLE CreateSuspendedThread(LPTHREAD_START_ROUTINE lpStartAddress, DWORD lpParameter)
{
    DWORD dwThreadId = 0;
    HANDLE hThread = CreateThread(NULL, 0, lpStartAddress,
                            (LPVOID)lpParameter, CREATE_SUSPENDED, &dwThreadId);
    if (hThread == NULL)
    {
        Fail("Failed to create a suspended thread.\n");
    }
    return hThread;
}

DWORD PALAPI TargetThreadRoutine1(LPVOID lpParameter)
{
    long int i, r;
    float j;
    LONG dwRet = -1;
    // targetIndex is the index of the target thread in the targetThreads array.
    DWORD targetIndex = (DWORD)lpParameter;

    for(i = 1; i <= numIterations; i++)
    {
        r = 4 * (rand() % 101);
        for(j = 0; j < r; j+=1.45);
        {
            MemoryRoutine();
            dwRet = SuspendThread(targetThreads[targetIndex]);
            if (dwRet < 0)
            {
                Fail("Failed to suspend a target thread - SuspendThread returned %d.\n", dwRet);
            }            
            MemoryRoutine();
            dwRet = ResumeThread(targetThreads[targetIndex]);
            if (dwRet < 0)
            {
                Fail("Failed to resume a target thread - ResumeThread returned %d.\n", dwRet);
            }                       
            MemoryRoutine();
        }
    }
    printf("A suspender routine is done\n");
    return 0;

}

DWORD PALAPI TargetThreadRoutine2(LPVOID lpParameter)
{
    long int i, r;
    float j;
    LONG dwRet = -1;
    // targetIndex is the index of the target thread in the targetThreads array.
    DWORD targetIndex = (DWORD)lpParameter;

    for(i = 1; i <= numIterations; i++)
    {
        r = 5.3 * (rand() % 107);
        for(j = 0; j < r; j+=1.67);
        { 
            MemoryRoutine();
            dwRet = SuspendThread(targetThreads[targetIndex]);
            if (dwRet < 0)
            {
                Fail("Failed to suspend a target thread - SuspendThread returned %d.\n", dwRet);
            }            
            MemoryRoutine();
            dwRet = ResumeThread(targetThreads[targetIndex]);
            if (dwRet < 0)
            {
                Fail("Failed to resume a target thread - ResumeThread returned %d.\n", dwRet);
            }                    
            MemoryRoutine();
        }

    }
    printf("A target routine is done\n");
    return 0;

}

int __cdecl main(int argc, char *argv[])
{
    int i;
    DWORD dwRet; 

    if(0 != PAL_Initialize(argc, argv))
    {
        return(FAIL);
    }

    if(argc == 3)
    {
        numThreads = atoi(argv[1]); // must be divisible by 2 - check for this.
        numIterations = atoi(argv[2]); // must be greater than 0.
    }
    else
    {
        numThreads = 4;
        numIterations = 1000;
    }

    if(numThreads < 4 || numThreads > MAX_THREADS)
    {
        Fail("numThreads must be greater than or equal to 4 and less than %d.\n", MAX_THREADS);
    }

    if(numThreads % 4 != 0)
    {
        Fail("numThreads must be divisible by 4.\n");
    }

    if(numIterations < 1)
    {
        Fail("numIterations must be greater than 0.\n");
    }

    srand((unsigned)time(NULL)); // seed random number generator

    for(i = 0; i < numThreads / 4; i++)
    {
        targetThreads[i + ((3 * numThreads) / 4)] = CreateSuspendedThread(&TargetThreadRoutine2, i);
        if (-1 == ResumeThread(targetThreads[i + ((3 * numThreads) / 4)]))
        {
            Fail("Failed to resume a target thread.\n");
        }

        targetThreads[(i + (numThreads / 2))] = CreateSuspendedThread(&TargetThreadRoutine1, (i + ((3 * numThreads) / 4)));
        if (-1 == ResumeThread(targetThreads[(i + (numThreads / 2))]))
        {
            Fail("Failed to resume a target thread.\n");
        }        

        targetThreads[(i + (numThreads / 4))] = CreateSuspendedThread(&TargetThreadRoutine2, (i + (numThreads / 2)));
        if (-1 == ResumeThread(targetThreads[(i + (numThreads / 4))]))
        {
            Fail("Failed to resume a target thread.\n");
        }        

        targetThreads[i] = CreateSuspendedThread(&TargetThreadRoutine1, (i + (numThreads / 4)));
        if (-1 == ResumeThread(targetThreads[i]))
        {
            Fail("Failed to resume a target thread.\n");
        }        
    }

    dwRet = WaitForMultipleObjects(numThreads, targetThreads, TRUE, INFINITE); // wait for all threads
    if (WAIT_FAILED == dwRet)
    {
        Fail("WaitForMultipleObjects failed\n");
    }
    
    PAL_Terminate();
    return(PASS);

}


