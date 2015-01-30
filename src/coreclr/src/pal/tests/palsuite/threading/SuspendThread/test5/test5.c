//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test8.c
**
** Purpose:
**
** QueueUserAPC, Wait Subsystem and Thread Suspension stress test 
** 
** This test was written while researching a failure where a thread gets 
** stuck inside pthread_condition_signal, which is calling cond_queue_deq. 
** The thread would endlessly loop while nativigating the thread list, waiting 
** on the target condition. This occurs in the context of a thread posting an 
** APC to a thread of the VM's pool. This test partially reproduces the VM 
** scenario by posting (from different threads) lots of APCs to a couple of 
** threads which are looping in alterable SleepEx with a very short timeout. 
**    
**
**
**===========================================================================*/
#include <palsuite.h>

#ifndef MIN
#define MIN(a,b) (((a)<(b)) ? (a) : (b))
#endif

#define MAX_THREAD_COUNT 128
#define DEFAULT_EXECUTING_THREAD_COUNT 2
#define DEFAULT_POSTING_THREAD_COUNT 3
#define MAX_RECURSION_DEPTH 10
#define APC_FUNC_SLEEPTIME 100 // ms
#define DEFAULT_TEST_DURATION 60 // s

volatile LONG g_rglOutstandingAPCs[MAX_THREAD_COUNT];

LONG g_lExecutingThreadCount = DEFAULT_EXECUTING_THREAD_COUNT;
LONG g_lPostingThreadCount = DEFAULT_POSTING_THREAD_COUNT;

volatile LONG g_lCurrentExecutingThreadCount = 0;
LONG g_lAPCPosted = 0;
LONG g_lSuspCount = 0;
LONG g_lResumeCount = 0;
LONG g_lSuspFailCount = 0;
LONG g_lResumeFailCount = 0;

int g_iTestDuration = DEFAULT_TEST_DURATION;

typedef struct _executing_thread_info
{
    DWORD dwThreadIdx;
    LONG lDepth;
    LONG lAPCCount;
    HANDLE hDummyEvent;
} executing_thread_info;

executing_thread_info g_rgEti[MAX_THREAD_COUNT];

HANDLE g_rghExecutingThread[MAX_THREAD_COUNT] = { 0 };
HANDLE g_rghPostingThread[MAX_THREAD_COUNT] = { 0 };

HANDLE g_hDoneEvent = NULL;
HANDLE g_hDummyEvent = NULL;

BOOL g_bDone = FALSE;

VOID PALAPI APCFunc(ULONG_PTR ulptrParam)
{
    LONG lThreadIdx = (LONG)ulptrParam;
    executing_thread_info * peti = (executing_thread_info *)&g_rgEti[lThreadIdx];
    LONG lVal;
    BOOL bAlertableSleep = (BOOL)(peti->lDepth < MAX_RECURSION_DEPTH);
    
    lVal = InterlockedDecrement(&g_rglOutstandingAPCs[lThreadIdx]);
    
    peti->lAPCCount++;
    peti->lDepth += 1;
    SleepEx(rand() % APC_FUNC_SLEEPTIME, bAlertableSleep);
    peti->lDepth -= 1;
}

DWORD PALAPI ExecutingThread(LPVOID pvParam)
{
    DWORD dwRet = 0;
    DWORD dwThreadId = (DWORD)pvParam;
    HANDLE rghWaitobjs[2];
    DWORD dwTmo = 10;

    Trace("[Executing thread %u] Started \n", dwThreadId);

    InterlockedIncrement(&g_lCurrentExecutingThreadCount);

    rghWaitobjs[0] = g_hDoneEvent;
    rghWaitobjs[1] = g_rgEti[dwThreadId].hDummyEvent;

    do 
    {
        switch (rand()%2)
        {
            case 0:
            {
                dwRet = SleepEx(1 + (rand() % dwTmo), TRUE);
                break;
            }
            case 1:
            default:
            {
                dwRet = WaitForMultipleObjectsEx(2, rghWaitobjs, FALSE, 1 + (rand() % dwTmo), TRUE);
                break;
            }
        }       
    } while (!g_bDone);

    InterlockedDecrement(&g_lCurrentExecutingThreadCount);
    Trace("[Executing thread %u] Done\n", dwThreadId);    
    return 0;
}

DWORD PALAPI PostingThread(LPVOID pvParam)
{
    DWORD dwRet = 0;
    DWORD dwThreadId = (DWORD)pvParam;
    int i;
    LONG lRet;

    Trace("[Posting thread %u] Started \n", dwThreadId);

    do 
    {
        i = rand() % g_lExecutingThreadCount;
        lRet = InterlockedIncrement(&g_rglOutstandingAPCs[i]);

        if (MAX_RECURSION_DEPTH > lRet)
        {
            dwRet = QueueUserAPC(APCFunc, g_rghExecutingThread[i], (ULONG_PTR)i);
            if (dwRet == 0)
            {
                Fail( "ERROR:%lu:QueueUserAPC call failed\n", GetLastError());
            }                

            InterlockedIncrement(&g_lAPCPosted);
        }
        else
        {
            InterlockedDecrement(&g_rglOutstandingAPCs[i]);
        }

        if (0 == rand()%4)
        {
            SetEvent(g_hDummyEvent);
        }
    } while (WAIT_TIMEOUT == WaitForSingleObject(g_hDoneEvent, rand()%10));

    Trace("[Posting thread %u] Done\n", dwThreadId);    
    return 0;
}

DWORD PALAPI SuspenderThread()
{
    HANDLE targetThread;
    DWORD ret;
    int i,j,idx,jdx;

    printf("[Suspender Thread] Starting\n");

    while (!g_bDone)
    {        
        jdx = rand()%2;
        for (j=0; j<2; j++, jdx++)
        {
            switch(jdx % 2)
            {
                case 0:
                {
                    idx = rand() % g_lCurrentExecutingThreadCount;
                    for (i=0; i < g_lCurrentExecutingThreadCount; i++)
                    {
                        targetThread = g_rghExecutingThread[idx];
                        if (NULL != targetThread)
                        {
                            ret = SuspendThread(targetThread);
                            if (-1 != ret)
                            {
                                g_lSuspCount += 1;
                            }
                            else
                            {
                                g_lSuspFailCount += 1;
                            }
                        }
                        idx = (idx+1) % g_lCurrentExecutingThreadCount;
                    }
                    break;
                }
                case 1:
                default:
                {
                    idx = rand() % g_lPostingThreadCount;
                    for (i=0; i < g_lPostingThreadCount; i++)
                    {
                        targetThread = g_rghPostingThread[idx];
                        if (NULL != targetThread)
                        {
                            ret = SuspendThread(targetThread);              
                            if (-1 != ret)
                            {
                                g_lSuspCount += 1;
                            }
                            else
                            {
                                g_lSuspFailCount += 1;
                            }
                        }
                        idx = (idx+1) % g_lPostingThreadCount;
                    }
                    break;
                }
            }
        }

        Sleep(rand() % 100);        

        jdx = rand() % 2;
        for (j=0; j<2; j++, jdx++)
        {
            switch(jdx % 2)
            {
                case 0:
                {
                    idx = rand() % g_lCurrentExecutingThreadCount;
                    for (i=0; i < g_lCurrentExecutingThreadCount; i++)
                    {
                        targetThread = g_rghExecutingThread[idx];
                        if (NULL != targetThread)
                        {
                            ret = ResumeThread(targetThread);              
                            if (-1 != ret)
                            {
                                g_lResumeCount += 1;
                            }
                            else
                            {
                                g_lResumeFailCount += 1;
                            }
                        }
                        idx = (idx+1) % g_lCurrentExecutingThreadCount;
                    }
                    break;
                }
                case 1:
                default:
                {
                    idx = rand() % g_lPostingThreadCount;
                    for (i=0; i < g_lPostingThreadCount; i++)
                    {
                        targetThread = g_rghPostingThread[idx];
                        if (NULL != targetThread)
                        {
                            ret = ResumeThread(targetThread);              
                            if (-1 != ret)
                            {
                                g_lResumeCount += 1;
                            }
                            else
                            {
                                g_lResumeFailCount += 1;
                            }
                        }
                        idx = (idx+1) % g_lPostingThreadCount;
                    }
                    break;
                }
            }
        }

        Sleep(rand() % 100);
    }

    printf("[Suspender Thread] Done\n");

    return 0;
}

int __cdecl main( int argc, char **argv )
{
    int i, j;
    DWORD dwThreadId;
    DWORD dwRet;
    HANDLE hSuspenderThread = NULL;
    
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return(FAIL);
    }

    i = (int)(GetTickCount() % INT_MAX);
    srand(i);

    Trace("[main] Starting [seed=%d r1=%d r2=%d r3=%d]\n", i, rand(), rand(), rand());

    g_hDoneEvent = CreateEvent( NULL, TRUE, FALSE, NULL );
    if ( g_hDoneEvent == NULL )
    {
        Fail("ERROR:%#x: CreateEvent() call failed\n", GetLastError());
    }
    g_hDummyEvent = CreateEvent( NULL, FALSE, FALSE, NULL );

    for (i=0; i<g_lExecutingThreadCount; i++)
    {
        g_rgEti[i].hDummyEvent = CreateEvent( NULL, FALSE, FALSE, NULL );
        if (g_rgEti[i].hDummyEvent == NULL)
        {
            Fail("ERROR:%#x: CreateEvent() call failed\n", GetLastError());
        }
    }        

    for (i=0, j=0; i<g_lExecutingThreadCount; i++)
    {
        g_rgEti[j].dwThreadIdx = j;
        g_rgEti[j].lDepth = 0;
        g_rgEti[j].lAPCCount = 0;
    
        g_rghExecutingThread[j] = CreateThread(
                                NULL,
                                0,
                                (LPTHREAD_START_ROUTINE)ExecutingThread,
                                (LPVOID)j,
                                0,
                                &dwThreadId);

        if (NULL != g_rghExecutingThread[j])
        {
            j++;
        }
    }

    g_lExecutingThreadCount = j;
    
    if (1 > g_lExecutingThreadCount)
    {
        Fail("Unable to create enough executing threads\n");
    }
    Trace("[main] %d executing threads created\n", g_lExecutingThreadCount);

    for (i=0, j=0; i<g_lPostingThreadCount; i++)
    {    
        g_rghPostingThread[j] = CreateThread(
                                            NULL,
                                            0,
                                            (LPTHREAD_START_ROUTINE)PostingThread,
                                            (LPVOID)j,
                                            0,
                                            &dwThreadId);
        if(NULL != g_rghPostingThread[j])
        {
            j++;
        }
    }
    g_lPostingThreadCount = j;
    
    if (1 > g_lPostingThreadCount)
    {
        Fail("Unable to create enough posting threads\n");
    }
    Trace("[main] %d posting threads created\n", g_lPostingThreadCount);

    hSuspenderThread = CreateThread(
                                    NULL,
                                    0,
                                    (LPTHREAD_START_ROUTINE)SuspenderThread,
                                    NULL,
                                    0,
                                    &dwThreadId);

    Sleep (g_iTestDuration * 1000);

    if (!SetEvent(g_hDoneEvent))
    {
        Fail("ERROR:%lu:SetEvent() call failed\n", GetLastError());
    }

    Sleep(g_iTestDuration / 10);
    
    g_bDone = TRUE;

    Trace("[main] Initiating shutdown\n" );

    /* wait on the other thread to complete */
    for (i=0; i < g_lExecutingThreadCount; i += MAXIMUM_WAIT_OBJECTS)
    {
        do 
        {
            dwRet = WaitForMultipleObjects(
                                            MIN(g_lExecutingThreadCount - i, MAXIMUM_WAIT_OBJECTS),
                                            g_rghExecutingThread + i,
                                            TRUE,
                                            1000);
        } while (WAIT_TIMEOUT == dwRet);
        
        if (WAIT_OBJECT_0 != dwRet)
        {
            Fail("Wait for all threads failed\n");
        }
    }

    j = 0;
    Trace("[main] Number of APC executed per target thread: { " );
    for (i=0; i < g_lExecutingThreadCount; i++)
    {
        Trace("%d ", (int)g_rgEti[i].lAPCCount);
        j += (int)g_rgEti[i].lAPCCount;
    }
    Trace(" }\n");
    Trace("[main] Total number of APC executed:    %d\n", j);
    Trace("[main] Total number of APC posted:      %d\n", g_lAPCPosted);
    Trace("[main] Successfull thread suspensions:  %d [%d failures]\n", g_lSuspCount, g_lSuspFailCount);
    Trace("[main] Successfull thread resumes:      %d [%d failures]\n", g_lResumeCount, g_lResumeFailCount);

    /* PAL termination */
    PAL_Terminate();

    /* return success */
    return PASS;
}
