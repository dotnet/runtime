// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:      test4.c (DuplicateHandle)
**
** Purpose:     Tests the PAL implementation of the DuplicateHandle function.
**              This test duplication of a Mutex handle. The test will comprise
**              of creating a Mutex and its duplicate and create a thread that 
**              will get ownership. Another thread will be create that will 
**              attempt to get ownership of the duplicate Mutex, this will 
**              fail, since the Mutex is owned by another thread. The Mutex 
**              will be released and then the thread will attempt to get 
**              ownership of the duplicate Mutex, this will succeed.
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

                               
volatile int t1_result_DuplicateHandle_test4=WR_WAITING;
volatile int t2_result_DuplicateHandle_test4=WR_WAITING;


DWORD PALAPI ThreadTest1_DuplicateHandle_test4(LPVOID lpParam)
{
    DWORD dwWait;

    dwWait = WaitForSingleObject((HANDLE)lpParam, 0);
    if (dwWait == WAIT_OBJECT_0)
    {
        /* tell the main thread we got the mutex */
        t1_result_DuplicateHandle_test4=WR_GOT_MUTEX;

        /* wait for main thread to tell us to release the mutex */
        while(WR_GOT_MUTEX == t1_result_DuplicateHandle_test4)
            Sleep(1);
        ReleaseMutex((HANDLE)lpParam);

        /* tell the main thread we released the mutex */
        t1_result_DuplicateHandle_test4 = WR_RELEASED;
    }
    else
    {
        t1_result_DuplicateHandle_test4 = WR_TIMED_OUT;
    }
    return 0;
}

DWORD PALAPI ThreadTest2_DuplicateHandle_test4(LPVOID lpParam)
{
    DWORD dwWait;

    dwWait = WaitForSingleObject((HANDLE)lpParam, 0 );
    if (dwWait == WAIT_OBJECT_0)
    {
        ReleaseMutex((HANDLE)lpParam);
        t2_result_DuplicateHandle_test4 = WR_GOT_MUTEX;
    }
    else
    {
        t2_result_DuplicateHandle_test4 = WR_TIMED_OUT;
    }

    return 0;
}


PALTEST(threading_DuplicateHandle_test4_paltest_duplicatehandle_test4, "threading/DuplicateHandle/test4/paltest_duplicatehandle_test4")
{
    
    HANDLE hDupMutex;
    HANDLE hMutex;
    HANDLE hThread;
    HANDLE hThread2;
    BOOL   bDupHandle=FALSE;
    DWORD  dwThreadId = 0;

    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return(FAIL);
    }

    /*Create Mutex without ownership*/ 
    hMutex = CreateMutexW(NULL,       // no security attributes
                          FALSE,      // initially not owned
                          NULL);      // name of mutex
    if (hMutex == NULL) 
    {
        Fail("ERROR:%u: Unable to create mutex\n", 
             GetLastError());
    }

    /*Create Duplicate of the Mutex above*/
    bDupHandle = DuplicateHandle(GetCurrentProcess(),       
                                 hMutex,                    
                                 GetCurrentProcess(),       
                                 &hDupMutex,                
                                 GENERIC_READ|GENERIC_WRITE,
                                 FALSE,
                                 DUPLICATE_SAME_ACCESS);
    if (!bDupHandle)
    {
        Trace("ERROR:%u: Created the duplicate handle to "
             "closed event handle hMutex=0x%lx\n",
             GetLastError(),
             hMutex);
        CloseHandle(hMutex);
        Fail("");
    }

    /*Create a thread to test the Mutex*/       
    hThread = CreateThread(NULL,
                           0,
                           &ThreadTest1_DuplicateHandle_test4,
                           hMutex,
                           0,
                           &dwThreadId);
    if (hThread == NULL)
    {
        Trace("ERROR:%u: unable to create thread\n",
             GetLastError());
        CloseHandle(hMutex);
        CloseHandle(hDupMutex);
        Fail("");
    }

    /* wait until thread has taken the mutex */
    while (WR_WAITING == t1_result_DuplicateHandle_test4)
        Sleep(1);

    if(WR_TIMED_OUT == t1_result_DuplicateHandle_test4)
    {
        Trace("ERROR: %u: thread 1 couldn't acquire the mutex\n");
        CloseHandle(hMutex);
        CloseHandle(hDupMutex);
        CloseHandle(hThread);
        Fail("");
    }

    /*Create a second thread to use the duplicate Mutex*/
    /*This should fail since the Mutex is owned hThread*/
    hThread2 = CreateThread(NULL,        
                            0,           
                            &ThreadTest2_DuplicateHandle_test4,  
                            hDupMutex,   
                            0,           
                            &dwThreadId);

    if (hThread2 == NULL)
    {
        Trace("ERROR:%u: unable to create thread\n",
             GetLastError());
        CloseHandle(hMutex);
        CloseHandle(hDupMutex);
        CloseHandle(hThread);
        Fail("");
    }
    
    /* wait until thread has tried to take the mutex */
    while (WR_WAITING == t2_result_DuplicateHandle_test4)
        Sleep(1);
    
    if (WR_TIMED_OUT != t2_result_DuplicateHandle_test4 )
    {
        Trace("ERROR:%u: Able to take mutex %#x while its duplicate %#x is "
              "held\n", hDupMutex, hMutex);
        CloseHandle(hMutex);
        CloseHandle(hDupMutex);
        CloseHandle(hThread);
        CloseHandle(hThread2);
        Fail("");
    }

    /* reset second thread status */
    t2_result_DuplicateHandle_test4 = WR_WAITING;

    /* tell thread 1 to release the mutex */
    t1_result_DuplicateHandle_test4 = WR_WAITING;

    /* wait for thread 1 to release the mutex */
    while (WR_WAITING == t1_result_DuplicateHandle_test4)
        Sleep(1);
    
    CloseHandle(hThread2);

    /*Re-Create the second thread to reuse the duplicated Mutex*/
    /*This test should pass, the Mutex has since been released*/
    hThread2 = CreateThread(NULL,         
                             0,            
                             &ThreadTest2_DuplicateHandle_test4,   
                             hDupMutex,    
                             0,            
                             &dwThreadId);

    if (hThread2 == NULL)
    {
        Trace("ERROR:%u: unable to create thread\n",
             GetLastError());
        CloseHandle(hMutex);
        CloseHandle(hDupMutex);
        CloseHandle(hThread);
        Fail("");
    }
    
    /* wait until thread has taken the mutex */
    while (WR_WAITING == t2_result_DuplicateHandle_test4)
        Sleep(1);
    
    if (WR_GOT_MUTEX != t2_result_DuplicateHandle_test4 )
    {
        Trace("ERROR:%u: Unable to take mutex %#x after its duplicate %#x was "
              "released\n", hDupMutex, hMutex);
        CloseHandle(hMutex);
        CloseHandle(hDupMutex);
        CloseHandle(hThread);
        CloseHandle(hThread2);
        Fail("");
    }

    /*Cleanup.*/
    CloseHandle(hMutex);
    CloseHandle(hDupMutex);
    CloseHandle(hThread);
    CloseHandle(hThread2);

    PAL_Terminate();
    return (PASS);
}
