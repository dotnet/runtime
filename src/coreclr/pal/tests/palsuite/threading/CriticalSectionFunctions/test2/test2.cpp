// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CriticalSectionFunctions/test2/test2.c
** 
** Purpose: Test that we are able to nest critical section calls.  
** The initial thread makes a call to EnterCriticalSection once, 
** blocking on a CRITICAL_SECTION object and creates a new thread.  
** The newly created thread blocks on the same CRITICAL_SECTION object.
** The first thread now makes a call to LeaveCriticalSection.
** Test to see that the new thread doesn't get unblocked.
**
** Dependencies: CreateThread
**               InitializeCriticalSection
**               EnterCriticalSection
**               LeaveCriticalSection
**               DeleteCriticalSection
**               WaitForSingleObject
** 

**
**=========================================================*/

#include <palsuite.h>

volatile BOOL t0_tflag = FAIL;  /* thread 0 timeout flag */
volatile BOOL t1_aflag = FAIL;  /* thread 1 access flag */
volatile BOOL t1_cflag = FAIL;  /* thread 1 critical section flag */
volatile BOOL bTestResult = FAIL;

DWORD PALAPI Thread_CriticalSectionFunctions_test2(LPVOID lpParam)
{
    t1_aflag = PASS;
    EnterCriticalSection(&CriticalSection);
    t1_cflag = PASS;
    LeaveCriticalSection(&CriticalSection);
    return 0;
}

PALTEST(threading_CriticalSectionFunctions_test2_paltest_criticalsectionfunctions_test2, "threading/CriticalSectionFunctions/test2/paltest_criticalsectionfunctions_test2")
{
    HANDLE hThread;
    DWORD dwThreadId;
    DWORD dwRet;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (bTestResult);
    }
    
    /*
     * Create critical section object and enter it
     */
    InitializeCriticalSection ( &CriticalSection );
    EnterCriticalSection(&CriticalSection);

    /*
     * Create a suspended thread 
     */
    hThread = CreateThread(NULL,
                           0,
                           &Thread_CriticalSectionFunctions_test2,
                           (LPVOID) NULL,
                           CREATE_SUSPENDED,
                           &dwThreadId);

    if (hThread == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread call failed.  GetLastError "
             "returned %d.\n", GetLastError());
        LeaveCriticalSection(&CriticalSection);
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }

    EnterCriticalSection(&CriticalSection);
    /* 
     * Set priority of the thread to greater than that of the currently
     * running thread so it is guaranteed to run.
     */
    dwRet = (DWORD) SetThreadPriority(hThread, THREAD_PRIORITY_ABOVE_NORMAL);

    if (0 == dwRet)
    {
        Trace("PALSUITE ERROR: SetThreadPriority (%p, %d) call failed.\n"
             "GetLastError returned %d.\n", hThread, 
             THREAD_PRIORITY_NORMAL, GetLastError());
    LeaveCriticalSection(&CriticalSection);
        CloseHandle(hThread);
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }

    dwRet = ResumeThread(hThread);
     
    if (-1 == dwRet)
    {
        Trace("PALSUITE ERROR: ResumeThread(%p) call failed.\nGetLastError "
             "returned %d.\n", hThread, GetLastError());
        LeaveCriticalSection(&CriticalSection);
        CloseHandle(hThread);
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }
    /* 
     * Sleep until we know the thread has been invoked.  This sleep in 
     * combination with the higher priority of the other thread should 
     * guarantee both threads block on the critical section.
     */
    while (t1_aflag == FAIL)
    {
        Sleep(1);
    }

    LeaveCriticalSection(&CriticalSection);

    switch ((WaitForSingleObject(
        hThread,
        10000)))      /* Wait 10 seconds */
    {
    case WAIT_OBJECT_0: 
        /* Object (thread) is signaled */
        LeaveCriticalSection(&CriticalSection);
        CloseHandle(hThread);
        DeleteCriticalSection(&CriticalSection);
        Fail("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
             "returned\nWAIT_TIMEOUT ('%d'), instead it returned "
             "WAIT_OBJECT_0 ('%d').\nA nested LeaveCriticalSection(%p) "
             "call released both threads that were waiting on it!\n", 
             hThread, 10000, WAIT_TIMEOUT, WAIT_OBJECT_0, &CriticalSection);
        break;
    case WAIT_ABANDONED: 
        /*
         * Object was mutex object whose owning
         * thread has terminated.  Shouldn't occur.
         */
        Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
             "returned\nWAIT_TIMEOUT ('%d'), instead it returned "
             "WAIT_ABANDONED ('%d').\nGetLastError returned '%d'\n", 
             hThread, 10000, WAIT_TIMEOUT, WAIT_ABANDONED, GetLastError());
        LeaveCriticalSection(&CriticalSection);
        CloseHandle(hThread);
        DeleteCriticalSection(&CriticalSection);
        Fail("");
        break;
    case WAIT_FAILED:    /* WaitForSingleObject function failed */
        Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
             "returned\nWAIT_TIMEOUT ('%d'), instead it returned "
             "WAIT_FAILED ('%d').\nGetLastError returned '%d'\n", 
             hThread, 10000, WAIT_TIMEOUT, WAIT_FAILED, GetLastError());
        LeaveCriticalSection(&CriticalSection);
        CloseHandle(hThread);
        DeleteCriticalSection(&CriticalSection);
        Fail("");
        break;
    case WAIT_TIMEOUT: 
        /* 
         * We expect this thread to timeout waiting for the 
         * critical section object to become available.
         */
        t0_tflag = PASS;
        break;  
    }

    LeaveCriticalSection(&CriticalSection);

    if (WAIT_OBJECT_0 != WaitForSingleObject (hThread, 10000)) 
    {
        if (0 == CloseHandle(hThread))
        {
            Trace("PALSUITE ERROR: CloseHandle(%p) call failed.\n"
                 "WaitForSingleObject(%p,%d) should have returned "
                 "WAIT_OBJECT_0 ('%d').\nBoth calls failed.  "
                 "Deleted CRITICAL_SECTION object which likely means\n"
                 "thread %p is now in an undefined state.  GetLastError "
                 "returned '%d'.\n", hThread, hThread, 10000, WAIT_OBJECT_0, 
                 hThread, GetLastError());
            DeleteCriticalSection(&CriticalSection);
            Fail("");
        }
        else 
        {
            Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
                 "returned WAIT_OBJECT_0 ('%d').\n  GetLastError returned "
                 "'%d'.\n", hThread, hThread, 10000, WAIT_OBJECT_0, 
                 hThread, GetLastError());
            DeleteCriticalSection(&CriticalSection);
            Fail("");
        }
    }    

    if (0 == CloseHandle(hThread))
    {
        Trace("PALSUITE ERROR: CloseHandle(%p) call failed.\n"
             "Deleted CRITICAL_SECTION object which likely means\n"
             "thread %p is now in an undefined state.  GetLastError "
             "returned '%d'.\n", hThread, hThread, GetLastError());
        DeleteCriticalSection(&CriticalSection);
        Fail("");

    }
    DeleteCriticalSection(&CriticalSection);
    /* 
     * Ensure both thread 0 experienced a wait timeout and thread 1 
     * accessed the critical section or fail the test, otherwise pass it.
     */
    if ((t0_tflag == FAIL) || (t1_cflag == FAIL))
    {
        Trace("PALSUITE ERROR: Thread 0 returned %d when %d was expected.\n"
              "Thread 1 returned %d when %d was expected.\n", t0_tflag, 
              PASS, t1_cflag, PASS); 
        bTestResult=FAIL;
    }
    else 
    {
        bTestResult=PASS;
    }
    
    PAL_TerminateEx(bTestResult);
    return (bTestResult);
}
