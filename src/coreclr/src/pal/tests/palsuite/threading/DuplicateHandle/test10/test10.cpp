// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:      test10.c (DuplicateHandle)
**
** Purpose:     Tests the PAL implementation of the DuplicateHandle function.
**              This tests the operation of a duplicated Semaphore handle
**
**
**===================================================================*/

#include <palsuite.h>

enum wait_results
{
    WR_WAITING,
    WR_GOT_MUTEX,
    WR_TIMED_OUT,
    WR_RELEASED
};


volatile int t1_result_DuplicateHandle_test10=WR_WAITING;
volatile int t2_result_DuplicateHandle_test10=WR_WAITING;


DWORD PALAPI ThreadTest1_DuplicateHandle_test10(LPVOID lpParam)
{
    DWORD dwWait;

    dwWait = WaitForSingleObject((HANDLE)lpParam, 0);
    if (dwWait == WAIT_OBJECT_0)
    {
        /* tell the main thread we got the mutex */
        t1_result_DuplicateHandle_test10=WR_GOT_MUTEX;

        /* wait for main thread to tell us to release the mutex */
        while(WR_GOT_MUTEX == t1_result_DuplicateHandle_test10)
            Sleep(1);
        ReleaseSemaphore((HANDLE)lpParam, 1, NULL);

        /* tell the main thread we released the mutex */
        t1_result_DuplicateHandle_test10 = WR_RELEASED;
    }
    else
    {
        t1_result_DuplicateHandle_test10 = WR_TIMED_OUT;
    }
    return 0;
}

DWORD PALAPI ThreadTest2_DuplicateHandle_test10(LPVOID lpParam)
{
    DWORD dwWait;

    dwWait = WaitForSingleObject((HANDLE)lpParam, 0 );
    if (dwWait == WAIT_OBJECT_0)
    {
        ReleaseSemaphore((HANDLE)lpParam, 1, NULL);
        t2_result_DuplicateHandle_test10 = WR_GOT_MUTEX;
    }
    else
    {
        t2_result_DuplicateHandle_test10 = WR_TIMED_OUT;
    }

    return 0;
}


PALTEST(threading_DuplicateHandle_test10_paltest_duplicatehandle_test10, "threading/DuplicateHandle/test10/paltest_duplicatehandle_test10")
{

    HANDLE hDupSemaphore;
    HANDLE hSemaphore;
    HANDLE hThread;
    HANDLE hThread2;
    BOOL   bDupHandle=FALSE;
    DWORD  dwThreadId = 0;

    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return(FAIL);
    }

    hSemaphore = CreateSemaphoreExW( NULL,
                                     1,
                                     1,
                                     NULL,
                                     0,
                                     0);
    if (hSemaphore == NULL)
    {
        Fail("PALSUITE ERROR:%u: Unable to create mutex\n",
             GetLastError());
    }

    /*Create Duplicate of the Semaphore above*/
    bDupHandle = DuplicateHandle(GetCurrentProcess(),
                                 hSemaphore,
                                 GetCurrentProcess(),
                                 &hDupSemaphore,
                                 GENERIC_READ|GENERIC_WRITE,
                                 FALSE,
                                 DUPLICATE_SAME_ACCESS);
    if (!bDupHandle)
    {
        Trace("PALSUITE ERROR:%u: Created the duplicate handle to "
              "closed event handle hSemaphore=0x%lx\n",
              GetLastError(),
              hSemaphore);
        CloseHandle(hSemaphore);
        Fail("");
    }

    /*Create a thread to test the Semaphore*/
    hThread = CreateThread(NULL,
                           0,
                           &ThreadTest1_DuplicateHandle_test10,
                           hSemaphore,
                           0,
                           &dwThreadId);
    if (hThread == NULL)
    {
        Trace("PALSUITE ERROR:%u: unable to create thread\n",
              GetLastError());
        CloseHandle(hSemaphore);
        CloseHandle(hDupSemaphore);
        Fail("");
    }

    /* wait until thread has taken the mutex */
    while (WR_WAITING == t1_result_DuplicateHandle_test10)
        Sleep(1);

    if(WR_TIMED_OUT == t1_result_DuplicateHandle_test10)
    {
        Trace("PALSUITE ERROR: %u: thread couldn't acquire the semaphore\n",
              GetLastError());
        CloseHandle(hSemaphore);
        CloseHandle(hDupSemaphore);
        CloseHandle(hThread);
        Fail("");
    }

    /*Create a second thread to use the Semaphore's duplicate handle*/
    /*This thread should block since the Semaphore is owned by another
      thread*/
    hThread2 = CreateThread(NULL,
                            0,
                            &ThreadTest2_DuplicateHandle_test10,
                            hDupSemaphore,
                            0,
                            &dwThreadId);

    if (hThread2 == NULL)
    {
        Trace("PALSUITE ERROR:%u: unable to create thread\n",
              GetLastError());
        CloseHandle(hSemaphore);
        CloseHandle(hDupSemaphore);
        CloseHandle(hThread);
        Fail("");
    }

    /* wait until thread has tried to take the mutex */
    while (WR_WAITING == t2_result_DuplicateHandle_test10)
        Sleep(1);
    
    if (WR_TIMED_OUT != t2_result_DuplicateHandle_test10 )
    {
        Trace("PALSUITE ERROR:%u: Able to take mutex %#x while its "
              "duplicate %#x is held\n", GetLastError(), hDupSemaphore,
              hSemaphore);
        CloseHandle(hSemaphore);
        CloseHandle(hDupSemaphore);
        CloseHandle(hThread);
        CloseHandle(hThread2);
        Fail("");
    }

    /* reset second thread status */
    t2_result_DuplicateHandle_test10 = WR_WAITING;

    /* tell thread 1 to release the mutex */
    t1_result_DuplicateHandle_test10 = WR_WAITING;

    /* wait for thread 1 to release the mutex */
    while (WR_WAITING == t1_result_DuplicateHandle_test10)
        Sleep(1);

    CloseHandle(hThread2);

    /*Re-Create the second thread to reuse the duplicated Semaphore*/
    /*Since the Semaphore has since been released, the thread should
      put WR_GOT_MUTEX into t2_result */
    hThread2 = CreateThread(NULL,
                            0,
                            &ThreadTest2_DuplicateHandle_test10,
                            hDupSemaphore,
                            0,
                            &dwThreadId);

    if (hThread2 == NULL)
    {
        Trace("PALSUITE ERROR:%u: unable to create thread\n",
              GetLastError());
        CloseHandle(hSemaphore);
        CloseHandle(hDupSemaphore);
        CloseHandle(hThread);
        Fail("");
    }

    /* wait until thread has taken the semaphore */
    while (WR_WAITING == t2_result_DuplicateHandle_test10)
        Sleep(1);
    
    if (WR_GOT_MUTEX != t2_result_DuplicateHandle_test10 )
    {
        Trace("PALSUITE ERROR:%u: Unable to take semaphore %#x after its"
              " duplicate %#x was released\n", GetLastError(), hDupSemaphore,
              hSemaphore);
        CloseHandle(hSemaphore);
        CloseHandle(hDupSemaphore);
        CloseHandle(hThread);
        CloseHandle(hThread2);
        Fail("");
    }

    /*Cleanup.*/
    CloseHandle(hSemaphore);
    CloseHandle(hDupSemaphore);
    CloseHandle(hThread);
    CloseHandle(hThread2);

    PAL_Terminate();
    return (PASS);
}
