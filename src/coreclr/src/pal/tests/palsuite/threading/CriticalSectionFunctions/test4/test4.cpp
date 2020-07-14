// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: criticalsectionfunctions/test4/test4.c
** 
** Purpose: Test to see if threads blocked on a CRITICAL_SECTION object will 
** be released in an orderly manner.  This case looks at the following
** scenario.  If one thread owns a CRITICAL_SECTION object and two threads 
** block in EnterCriticalSection, trying to hold the already owned 
** CRITICAL_SECTION object, when the first thread releases the CRITICAL_SECTION
** object, will one and only one of the waiters get unblocked?
**
** Dependencies: CreateThread
**               InitializeCriticalSection
**               EnterCriticalSection
**               LeaveCriticalSection
**               DeleteCriticalSection
**               Sleep
**               WaitForSingleObject
** 

**
**=========================================================*/

#include <palsuite.h>

#define NUM_BLOCKING_THREADS 2
                         
BOOL bTestResult;
CRITICAL_SECTION CriticalSection;
HANDLE hThread[NUM_BLOCKING_THREADS];
HANDLE hEvent;
DWORD dwThreadId[NUM_BLOCKING_THREADS];
volatile int flags[NUM_BLOCKING_THREADS] = {0,0};

DWORD PALAPI ThreadTest1(LPVOID lpParam)
{

    EnterCriticalSection ( &CriticalSection );

    flags[0] = 1;
    
    return 0;

}

DWORD PALAPI ThreadTest2(LPVOID lpParam)
{

    EnterCriticalSection ( &CriticalSection );

    flags[1] = 1;

    return 0;

}

int __cdecl main(int argc, char **argv)
{

    DWORD dwRet;
    DWORD dwRet1;
    bTestResult = FAIL;
    
    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return(bTestResult);
    }

    /*
     * Create Critical Section Object
     */
    InitializeCriticalSection ( &CriticalSection );

    EnterCriticalSection ( &CriticalSection );

    hThread[0] = CreateThread(NULL,
                              0,
                              &ThreadTest1,
                              (LPVOID) 0,
                              CREATE_SUSPENDED,
                              &dwThreadId[0]);
    if (hThread[0] == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread(%p, %d, %p, %p, %d, %p) call "
             "failed.\nGetLastError returned %d.\n", NULL, 0, &ThreadTest1,
             (LPVOID) 0, CREATE_SUSPENDED, &dwThreadId[0], GetLastError());
        LeaveCriticalSection(&CriticalSection);
        DeleteCriticalSection ( &CriticalSection );
        Fail("");
    }
    
    hThread[1] = CreateThread(NULL,
                              0,
                              &ThreadTest2,
                              (LPVOID) 0,
                              CREATE_SUSPENDED,
                              &dwThreadId[1]);
    if (hThread[1] == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread(%p, %d, %p, %p, %d, %p) call "
             "failed.\nGetLastError returned %d.\n", NULL, 0, &ThreadTest2,
             (LPVOID) 0, CREATE_SUSPENDED, &dwThreadId[1], GetLastError());
        LeaveCriticalSection(&CriticalSection);

        dwRet = ResumeThread(hThread[0]);
        if (-1 == dwRet)
        {
            Trace("PALSUITE ERROR: ResumeThread(%p) call failed.\n"
                  "GetLastError returned '%d'.\n", hThread[0],
             GetLastError());
    }

        dwRet = WaitForSingleObject(hThread[0], 10000);
        if (WAIT_OBJECT_0 == dwRet)
        {
            Trace("PALSUITE ERROR: WaitForSingleObject(%p, %d) call "
                  "failed.  '%d' was returned instead of the expected '%d'.\n"
                  "GetLastError returned '%d'.\n", hThread[0], 10000, dwRet, 
                  WAIT_OBJECT_0, GetLastError());
        }

        if (0 == CloseHandle(hThread[0]))
        {
            Trace("PALSUITE NOTIFICATION: CloseHandle(%p) call failed.\n"
                  "GetLastError returned %d.  Not failing tests.\n", 
                  hThread[0], GetLastError());
        }

        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }

    /* 
     * Set other thread priorities to be higher than ours & Sleep to ensure 
     * we give up the processor. 
     */
    dwRet = (DWORD) SetThreadPriority(hThread[0], 
                                      THREAD_PRIORITY_ABOVE_NORMAL);
    if (0 == dwRet)
    {
        Trace("PALSUITE ERROR: SetThreadPriority(%p, %d) call failed.\n"
              "GetLastError returned %d", hThread[0], 
              THREAD_PRIORITY_ABOVE_NORMAL, GetLastError());
    }
    
    dwRet = (DWORD) SetThreadPriority(hThread[1], 
                                      THREAD_PRIORITY_ABOVE_NORMAL);
    if (0 == dwRet)
    {
        Trace("PALSUITE ERROR: SetThreadPriority(%p, %d) call failed.\n"
              "GetLastError returned %d", hThread[1], 
              THREAD_PRIORITY_ABOVE_NORMAL, GetLastError());
    }

    dwRet = ResumeThread(hThread[0]);
    if (-1 == dwRet)
    {
        Trace("PALSUITE ERROR: ResumeThread(%p, %d) call failed.\n"
              "GetLastError returned %d", hThread[0], 
              GetLastError() );
    }
   
    dwRet = ResumeThread(hThread[1]); 
    if (-1 == dwRet)
    {
        Trace("PALSUITE ERROR: ResumeThread(%p, %d) call failed.\n"
              "GetLastError returned %d", hThread[0], 
              GetLastError());              
    }

    Sleep (0);

    LeaveCriticalSection (&CriticalSection);
    
    dwRet = WaitForSingleObject(hThread[0], 10000);
    dwRet1 = WaitForSingleObject(hThread[1], 10000);

    if ((WAIT_OBJECT_0 == dwRet) || 
        (WAIT_OBJECT_0 == dwRet1))
    {
        if ((1 == flags[0] && 0 == flags[1]) ||
            (0 == flags[0] && 1 == flags[1]))
        {
            bTestResult = PASS;
        }
        else 
        {
            bTestResult = FAIL;
            Trace ("PALSUITE ERROR: flags[%d] = {%d,%d}.  These values are"
                   "inconsistent.\nCriticalSection test failed.\n",
                   NUM_BLOCKING_THREADS, flags[0], flags[1]);
        }

        /* Fail the test if both threads returned WAIT_OBJECT_0 */
        if ((WAIT_OBJECT_0 == dwRet) && (WAIT_OBJECT_0 == dwRet1))
        {
            bTestResult = FAIL;
            Trace ("PALSUITE ERROR: WaitForSingleObject(%p, %d) and "
                   "WaitForSingleObject(%p, %d)\nboth returned dwRet = '%d'\n"
                   "One should have returned WAIT_TIMEOUT ('%d').\n", 
                   hThread[0], 10000, hThread[1], 10000, dwRet, WAIT_TIMEOUT);
        }        
    }
    else 
    {
        bTestResult = FAIL;
        Trace ("PALSUITE ERROR: WaitForSingleObject(%p, %d) and "
               "WaitForSingleObject(%p, %d)\nReturned dwRet = '%d' and\n"
               "dwRet1 = '%d' respectively.\n", hThread[0], 10000, hThread[1],
               10000, dwRet, dwRet1);
    }    

    if (WAIT_OBJECT_0 == dwRet)
    {
        if (0 == CloseHandle(hThread[0]))
        {
            Trace("PALSUITE NOTIFICATION: CloseHandle(%p) call failed.\n"
                  "GetLastError returned %d.  Not failing tests.\n", 
                  hThread[0], GetLastError());
        }
    }
    if (WAIT_OBJECT_0 == dwRet1)
    {
        if (0 == CloseHandle(hThread[1]))
        {
            Trace("PALSUITE NOTIFICATION: CloseHandle(%p) call failed.\n"
                  "GetLastError returned %d.  Not failing tests.\n", 
                  hThread[1], GetLastError());
        }
    }

    /* Leaking the CS on purpose, since there is still a thread 
       waiting on it */

    PAL_TerminateEx(bTestResult);
    return (bTestResult);
}
