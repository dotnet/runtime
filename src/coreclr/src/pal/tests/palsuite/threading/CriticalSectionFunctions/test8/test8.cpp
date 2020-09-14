// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:      CriticalSectionFunctions/test8/test8.c
**
** Pyrpose:     Ensure critical section functionality is working by
**              having multiple threads racing on a CS under different
**              scenarios
**
**
**===================================================================*/
#include <palsuite.h>

#define MAX_THREAD_COUNT       128
#define DEFAULT_THREAD_COUNT   10
#define DEFAULT_LOOP_COUNT     1000

#ifndef MIN
#define MIN(a,b) (((a)<(b)) ? (a) : (b))
#endif

int g_iThreadCount = DEFAULT_THREAD_COUNT;
int g_iLoopCount = DEFAULT_LOOP_COUNT;
volatile LONG g_lCriticalCount = 0;
HANDLE g_hEvStart = NULL;

CRITICAL_SECTION g_cs;
DWORD PALAPI Thread_CriticalSectionFunctions_test8(LPVOID lpParam)
{
    int i, j, iLpCnt;
    DWORD dwRet = 0;
    DWORD dwTid = GetCurrentThreadId();
    LONG lRet;
    BOOL bSleepInside;
    BOOL bSleepOutside;

    Trace("[tid=%u] Thread starting\n", dwTid);

    dwRet = WaitForSingleObject(g_hEvStart, INFINITE);
    if (WAIT_OBJECT_0 != dwRet)
    {
        Fail("WaitForSingleObject returned unexpected %u [GetLastError()=%u]\n",
             dwRet, GetLastError());
    }

    for (j=0;j<8;j++)
    {
        bSleepInside  = 2 & j;
        bSleepOutside = 4 & j;

        iLpCnt = g_iLoopCount;
        if (bSleepInside || bSleepOutside)
        {
            iLpCnt /= 10;
        }

        for (i=0;i<iLpCnt;i++)
        {
            EnterCriticalSection(&g_cs);
            if (1 & i)
            {
                // Simple increment on odd iterations
                lRet = (g_lCriticalCount += 1);
            }
            else
            {
                // Interlocked increment on even iterations
                lRet = InterlockedIncrement(&g_lCriticalCount);
            }

            if (1 != lRet || 1 != g_lCriticalCount)
            {
                Fail("Detected %d threads in area protected by critical section "
                     "[expected: 1 thread]\n", g_lCriticalCount);
            } 
            if (bSleepInside)
            {
                Sleep(rand()%10);
            }
            if (1 != g_lCriticalCount)
            {
                Fail("Detected %d threads inside area protected by critical section "
                     "[expected: 1 thread]\n", (int)g_lCriticalCount);
            } 

            if (1 & i)
            {
                // Simple decrement on odd iterations
                lRet = (g_lCriticalCount -= 1);
            }
            else
            {
                // Interlocked decrement on even iterations
                lRet = InterlockedDecrement(&g_lCriticalCount);
            }
            LeaveCriticalSection(&g_cs);

            if (bSleepOutside)
            {
                Sleep(rand()%10);
            }
        }
    }

    Trace("[tid=%u] Thread done\n", dwTid);

    return 0;
}

PALTEST(threading_CriticalSectionFunctions_test8_paltest_criticalsectionfunctions_test8, "threading/CriticalSectionFunctions/test8/paltest_criticalsectionfunctions_test8")
{
    DWORD dwThreadId;
    DWORD dwRet;
    HANDLE hThreads[MAX_THREAD_COUNT] = { 0 };
    int iThreadCount = 0;
    int i, iVal;

    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return(FAIL);
    }

    srand(time(NULL));

    for (i=1; i<argc; i++)
    {
        if ('-' == *argv[i])
        {
            switch(*(argv[i]+1))
            {
            case 'n':
                if (i < argc-1)
                {
                    i += 1;
                    iVal = atoi(argv[i]);
                    if (0 < iVal)
                    {
                        g_iLoopCount = iVal;
                    }
                }
                break;
            case 't':
                if (i < argc-1)
                {
                    i += 1;
                    iVal = atoi(argv[i]);
                    if (0 < iVal && MAX_THREAD_COUNT >= iVal)
                    {
                        g_iThreadCount = iVal;
                    }
                }
                break;
            default:
                break;
            }
        }
    }

    Trace ("Iterations:\t%d\n", g_iLoopCount);
    Trace ("Threads:\t%d\n", g_iThreadCount);

    g_hEvStart = CreateEvent(NULL, TRUE, FALSE, NULL);

    if (g_hEvStart == NULL)
    {
        Fail("CreateEvent call failed.  GetLastError "
             "returned %u.\n", GetLastError());
    }

    InitializeCriticalSection(&g_cs);

    for (i=0;i<g_iThreadCount;i++)
    {
        hThreads[iThreadCount] = CreateThread(NULL,
                                              0,
                                              &Thread_CriticalSectionFunctions_test8,
                                              (LPVOID) NULL,
                                              0,
                                              &dwThreadId);
        if (NULL != hThreads[iThreadCount])
        {
            iThreadCount++;
        }
    }

    Sleep(100);

    Trace("Created %d client threads\n", g_iThreadCount);

    if (2 > iThreadCount) 
    {
        Fail("Failed to create minimum number if threads, i.e. 2\n");
    }

    if (!SetEvent(g_hEvStart))
    {
        Fail("SetEvent failed [GetLastError()=%u]\n", GetLastError());
    }

    for (i=0; i<iThreadCount; i+=64)
    {
        dwRet = WaitForMultipleObjects(MIN(iThreadCount-i,64),
                                       hThreads+i,
                                       TRUE,
                                       INFINITE);
        if (WAIT_OBJECT_0 != dwRet)
        {
            Fail("Wait for all threads failed\n");
        }
    }

    PAL_Terminate();
    return (PASS);
}
